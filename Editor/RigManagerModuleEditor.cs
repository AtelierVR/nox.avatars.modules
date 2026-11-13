using System;
using System.Collections.Generic;
using System.Linq;
using Nox.Avatars.Parameters;
using Nox.Avatars.Rigging;
using Nox.CCK.Avatars.Rigging;
using Nox.CCK.Editor;
using Nox.CCK.Entities;
using Nox.Search;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.Modules.Editor {
	/// <summary>
	/// Inspector of the avatar rig manager, the <see cref="IRigProvider"/> of the avatar.
	/// <para>
	/// The module owns the live <see cref="IRigging"/> of the avatar: it resolves the rigging backend,
	/// creates the rig and swaps it when the owner announces another backend. None of it is serialized,
	/// so the inspector is a read-only summary of the provider: the backend in use, the IK targets the
	/// rig exposes, the parameters it publishes and the backends it could use.
	/// </para>
	/// <para>
	/// The panel follows the rig: it refreshes when the module raises
	/// <see cref="RigManagerModule.OnRigChanged"/> (backend swap, rig disposal) and when the avatar
	/// hierarchy changes, but only while the inspector is actually displayed. The values of the parameters
	/// are the exception: they move without any event, so they are re-read on a timer while the inspector is
	/// displayed (see <see cref="Tick"/>), and the row of a value that just moved flashes like the property
	/// rows of the session player panel.
	/// </para>
	/// </summary>
	public class RigManagerModuleEditor : LocalizedInspectorEditor<RigManagerModule> {
		/// <summary>Period of the refresh of the live parameter values, in milliseconds.</summary>
		private const int TickMs = 50;

		/// <summary>Icon of the component: the « accessibility_new » glyph rendered in the assets of the <c>ui</c> mod.</summary>
		protected override ResourceTextureSource Icon
			=> "ui:icons/rigging.png";

		protected override string TitleKey => "rigging.inspector.title";

		private LocalizedLabel   _backend;
		private LocalizedLabel   _targets;
		private VisualElement    _warning;
		private LocalizedLabel   _warningLabel;
		private LocalizedFoldout _partsFoldout;
		private VisualElement    _parts;
		private LocalizedFoldout _parametersFoldout;
		private VisualElement    _parameters;
		private LocalizedFoldout _backendsFoldout;
		private VisualElement    _backends;
		private VisualTreeAsset  _row;

		/// <summary>Rows of the parameters, kept so a tick can update the values in place.</summary>
		private readonly List<ParameterValueRow.Row> _parameterRows = new();

		/// <summary>Scheduled refresh of the live values, running only while the inspector is displayed.</summary>
		private IVisualElementScheduledItem _tick;

		/// <summary>Element the scheduled item belongs to: it has to be rescheduled after a rebuild.</summary>
		private VisualElement _tickHost;

		public override VisualElement CreateInspectorGUI() {
			var root = base.CreateInspectorGUI();
			if (Content == null)
				return root;

			// The GUI is rebuilt on every hierarchy change while the base keeps its root: drop the previous
			// panel so it is never added twice.
			Content.Clear();

			// nox.style.uss and nox.cck.style.uss are imported by the template (see its <Style> tags).
			var uxml = Resources.Load<VisualTreeAsset>("RigManagerModuleEditor");
			_row      = Resources.Load<VisualTreeAsset>("RigManagerModuleEditor.Row");
			if (uxml == null || _row == null) {
				Content.Add(new Label("The RigManagerModuleEditor uxml templates are not found in Resources."));
				return root;
			}

			var ui = uxml.CloneTree();
			Content.Add(ui);

			_backend  = ui.Q<LocalizedLabel>("label-backend");
			_targets  = ui.Q<LocalizedLabel>("label-targets");
			_warning  = ui.Q<VisualElement>("warning-no-rig");
			_warningLabel = ui.Q<LocalizedLabel>("label-warning");
			_partsFoldout  = ui.Q<LocalizedFoldout>("foldout-parts");
			_parts         = ui.Q<VisualElement>("list-parts");
			_parametersFoldout = ui.Q<LocalizedFoldout>("foldout-parameters");
			_parameters         = ui.Q<VisualElement>("list-parameters");
			_backendsFoldout    = ui.Q<LocalizedFoldout>("foldout-backends");
			_backends           = ui.Q<VisualElement>("list-backends");

			// The panels get their icons from the mod loader stylesheet, this project only ships them as
			// resources (see Packages/nox.cck/Resources/icons).
			SetIcon(ui.Q<VisualElement>("icon"), "icons/warning", new Color32(200, 151, 10, 255));

			Refresh();
			EnsureTick();
			return root;
		}

		/// <summary>Sets a nox.cck icon (see <c>Resources/icons</c>) as the background of <paramref name="element"/>.</summary>
		private static void SetIcon(VisualElement element, string resource, Color tint) {
			if (element == null)
				return;

			var texture = Resources.Load<Texture2D>(resource);
			if (texture != null)
				element.style.backgroundImage = new StyleBackground(texture);

			element.style.unityBackgroundImageTintColor = new StyleColor(tint);
		}

		// Nothing is serialized: the module reads the avatar and the rigging registry, so the panel follows
		// them instead of the inspector being a set of editable fields.
		protected override void OnAttachToPanel(AttachToPanelEvent evt) {
			// Localizes the markup and keeps it in sync with the language changes.
			base.OnAttachToPanel(evt);

			EditorApplication.hierarchyChanged += OnHierarchyChanged;
			SubscribeRigChanged();
		}

		protected override void OnDetachFromPanel(DetachFromPanelEvent evt) {
			base.OnDetachFromPanel(evt);

			EditorApplication.hierarchyChanged -= OnHierarchyChanged;
			UnsubscribeRigChanged();
		}

		// Back on screen: catch up with what was left alone while the inspector was hidden, and restart the
		// refresh of the live values.
		protected override void OnVisible() {
			Refresh();
			EnsureTick();
		}

		// Nothing is displayed: the values can wait until the inspector is shown again (see OnVisible).
		protected override void OnInvisible()
			=> _tick?.Pause();

		/// <summary>
		/// Starts (or restarts) the refresh of the live values. The item is scheduled on a panel element, so it
		/// stops by itself when the inspector is detached, and it has to be rebuilt after the panel is rebuilt.
		/// </summary>
		private void EnsureTick() {
			if (_parameters == null)
				return;

			if (_tick != null && _tickHost == _parameters) {
				_tick.Resume();
				return;
			}

			_tickHost = _parameters;
			_tick     = _parameters.schedule.Execute(Tick).Every(TickMs);
		}

		/// <summary>
		/// Re-reads the value of every parameter displayed and updates only the rows that moved, flashing the
		/// ones that did (see <see cref="ParameterValueRow.Row.Flash"/>). The values have no change event to
		/// subscribe to, so they are polled — at 20 Hz, and only while the inspector is displayed.
		/// </summary>
		private void Tick() {
			if (!IsVisible || _parameterRows.Count == 0)
				return;

			// The rig was swapped (or the avatar reloaded) without the panel hearing about it: the rows no
			// longer describe the parameters the rig publishes.
			if (CountOf((Target ? Target.Rig : null)?.GetParameters()) != _parameterRows.Count) {
				Refresh();
				return;
			}

			foreach (var row in _parameterRows)
				if (row.Update())
					row.Flash();
		}

		/// <summary>Number of parameters a rig actually publishes (the array may carry destroyed entries).</summary>
		private static int CountOf(IParameter[] parameters) {
			if (parameters == null)
				return 0;

			var count = 0;
			foreach (var parameter in parameters)
				if (parameter != null)
					count++;

			return count;
		}

		// The avatar hierarchy changes constantly while an avatar is loaded: the panel is only rebuilt when it
		// is actually displayed.
		private void OnHierarchyChanged() {
			if (IsVisible)
				Refresh();
		}

		/// <summary>The rig changed (backend swap, rig disposal): the summary is out of date.</summary>
		private void OnRigChanged(IRigging rig) {
			if (IsVisible)
				Refresh();
		}

		/// <summary>Follows the rig of the module, so a backend swap refreshes the panel without a poll.</summary>
		private void SubscribeRigChanged() {
			var module = Target;
			if (!module)
				return;

			module.OnRigChanged.AddListener(OnRigChanged);
		}

		private void UnsubscribeRigChanged() {
			var module = Target;
			if (!module)
				return;

			module.OnRigChanged.RemoveListener(OnRigChanged);
		}

		private void Refresh() {
			if (_backend == null || !Target)
				return;

			var rig = Target.Rig;

			// The rig only exists once the avatar is loaded and a backend handled it.
			_warning.style.display = rig == null ? DisplayStyle.Flex : DisplayStyle.None;

			_backend.LangKey  = rig == null ? "rigging.inspector.backend_none" : "rigging.inspector.backend";
			_backend.LangArgs = rig == null ? null : new object[] { rig.Id };

			var parts = rig?.GetParts() ?? Array.Empty<IRigPart>();
			_targets.LangArgs = new object[] { parts.Length };

			_partsFoldout.LangArgs = new object[] { parts.Length };
			_parts.Clear();

			// Alphabetical, case-insensitive: the rig exposes the parts in its own order.
			var displayed = 0;
			foreach (var part in parts.Where(e => e != null).OrderBy(PartName, StringComparer.OrdinalIgnoreCase)) {
				_parts.Add(MakePartRow(part, rig));
				displayed++;
			}

			if (displayed == 0)
				_parts.Add(Empty("rigging.inspector.no_parts"));

			var parameters = rig?.GetParameters() ?? Array.Empty<IParameter>();
			_parametersFoldout.LangArgs = new object[] { parameters.Length };
			_parameters.Clear();
			_parameterRows.Clear();

			displayed = 0;
			foreach (var parameter in parameters.Where(e => e != null).OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)) {
				// The row is kept so the timer can update its value without rebuilding the list.
				var row = ParameterValueRow.Build(parameter);
				_parameterRows.Add(row);
				_parameters.Add(row.Container);
				displayed++;
			}

			if (displayed == 0)
				_parameters.Add(Empty("rigging.inspector.no_parameters"));

			var backends = RiggingBackendRegistry.GetBackends();
			_backendsFoldout.LangArgs = new object[] { backends.Length };
			_backends.Clear();

			displayed = 0;
			foreach (var backend in backends.Where(e => e != null).OrderBy(e => e.Id, StringComparer.OrdinalIgnoreCase)) {
				_backends.Add(MakeBackendRow(backend.Id, rig != null && backend.Id == rig.Id));
				displayed++;
			}

			if (displayed == 0)
				_backends.Add(Empty("rigging.inspector.no_backends"));

			// The texts count what the rig just gave: re-format them with the arguments above.
			LocalizeTexts();
		}

		/// <summary>Text that names a rig part: its slot, or its raw index when the rig exposes no slot for it.</summary>
		private static string PartName(IRigPart part) {
			var slot = part.GetId().ToEntityRig();

			return slot == EntityRig.None ? part.GetId().ToString() : slot.ToString();
		}

		/// <summary>Row of <c>RigManagerModuleEditor.Row.uxml</c>: an IK target and the state of its bone.</summary>
		private VisualElement MakePartRow(IRigPart part, IRigging rig) {
			var row = _row.CloneTree();

			var slot = part.GetId().ToEntityRig();

			var name = row.Q<LocalizedLabel>("label-row");
			name.LangKey  = "rigging.inspector.part_row";
			name.LangArgs = new object[] { PartName(part) };

			var state = row.Q<LocalizedLabel>("label-state");
			state.LangKey  = StateKeyOf(rig, slot);
			state.LangArgs = null;

			return row;
		}

		/// <summary>Row of <c>RigManagerModuleEditor.Row.uxml</c>: a registered backend, active or not.</summary>
		private VisualElement MakeBackendRow(string id, bool active) {
			var row = _row.CloneTree();

			var name = row.Q<LocalizedLabel>("label-row");
			name.LangKey  = "rigging.inspector.backend_row";
			name.LangArgs = new object[] { id };

			var state = row.Q<LocalizedLabel>("label-state");
			state.LangKey  = active ? "rigging.inspector.backend_row_active" : "rigging.inspector.backend_row_idle";
			state.LangArgs = null;

			return row;
		}

		/// <summary>
		/// Language entry of the state of the bone: the avatar's own override first (tracking / animation),
		/// then the controller's choice, which is what the rig applies when the avatar does not override it.
		/// </summary>
		private static string StateKeyOf(IRigging rig, EntityRig slot) {
			if (slot == EntityRig.None)
				return "rigging.inspector.state_unknown";

			try {
				var bone = slot.ToHumanBodyBones();

				// The rig slots that have no humanoid bone (the base slot) map to the sentinel bone.
				if (bone == HumanBodyBones.LastBone)
					return "rigging.inspector.state_unknown";

				return rig.GetTracking(bone) switch {
					RiggingTrackingMode.Tracking  => "rigging.inspector.state_tracked",
					RiggingTrackingMode.Animation => "rigging.inspector.state_animated",
					_ => rig.IsActive(bone)
						? "rigging.inspector.state_driven"
						: "rigging.inspector.state_idle"
				};
			} catch (Exception e) {
				Logger.LogException(e, tag: nameof(RigManagerModuleEditor));
				return "rigging.inspector.state_unknown";
			}
		}

		/// <summary>Label of a language entry, for the texts the markup cannot carry (the empty lists).</summary>
		private static LocalizedLabel Empty(string key)
			=> new() { LangKey = key };
	}
}
