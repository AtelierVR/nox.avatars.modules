using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using Nox.CCK.Avatars.Parameters;
using Nox.CCK.Editor;
using Nox.Search;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nox.CCK.Avatars.Modules.Editor {
	/// <summary>
	/// Inspector of the parameter module: the parameters the animator of the avatar exposes, as a list of live
	/// values.
	/// <para>
	/// The list itself is read-only â€” a row carries the name, the value and the badges of the parameter â€” and
	/// the field that writes it lives in the popup of the row (see <see cref="ParameterValueRow"/>). A value
	/// that the local player does not own gets the Â« read-only Â» badge, and its field is disabled.
	/// </para>
	/// <para>
	/// The values have no change event: they are polled while the inspector is displayed, and a row flashes
	/// when its value moves, exactly like the rows of the rig manager.
	/// </para>
	/// </summary>
	public class AvatarParameterModuleEditor : LocalizedInspectorEditor<AvatarParameterModule> {
		/// <summary>Period of the refresh of the live values, in milliseconds.</summary>
		private const int TickMs = 50;

		/// <summary>Icon of the component: the Â« tune Â» glyph rendered in the assets of the <c>ui</c> mod.</summary>
		protected override ResourceTextureSource Icon
			=> "ui:icons/avatar_parameters.png";

		protected override string TitleKey
			=> "avatars.modules.parameters.inspector.title";

		/// <summary>Box shown instead of the list when there is nothing to display (no play mode, no avatar).</summary>
		private LocalizedNotificationElement _info;

		/// <summary>Section of the live values, whose title carries their count.</summary>
		private LocalizedFoldout _values;

		/// <summary>Rows of the parameters, kept so a tick can update the values in place.</summary>
		private VisualElement _list;

		private readonly List<ParameterValueRow.Row> _rows = new();

		/// <summary>
		/// Headers of the category sections, kept so the refresh on a language change also updates their
		/// tooltip (the description of a category is not a <c>lang-key</c> of its own).
		/// </summary>
		private readonly List<(LocalizedLabel Header, string Category)> _headers = new();

		/// <summary>Scheduled refresh of the live values, running only while the inspector is displayed.</summary>
		private IVisualElementScheduledItem _tick;

		/// <summary>Element the scheduled item belongs to: it has to be rescheduled after a rebuild.</summary>
		private VisualElement _tickHost;

		/// <summary>Set while a rebuild is scheduled, so a burst of registrations only rebuilds once.</summary>
		private bool _rebuild;

		/// <summary>Section of the saved parameter profiles (see <see cref="AvatarParameterProfiles"/>).</summary>
		private LocalizedFoldout _saves;

		/// <summary>Rows of the save section: the capture line, the profiles and the templates.</summary>
		private VisualElement _savesList;

		/// <summary>Name typed for the next capture.</summary>
		private TextField _savesName;

		/// <summary>Outcome of the last save/load, shown under the section.</summary>
		private LocalizedLabel _savesStatus;

		/// <summary>Avatar whose data was already read, so a rebuild does not read it again.</summary>
		private IRuntimeAvatar _avatar;

		public override VisualElement CreateInspectorGUI() {
			var root = base.CreateInspectorGUI();
			if (Content == null)
				return root;

			// The GUI is rebuilt on every hierarchy change while the base keeps its root: never add twice.
			Content.Clear();

			// The asset the module reads, in a section of its own: a plain PropertyField, so it keeps exactly the
			// look of a Unity object field (the row style of the list does not apply to it).
			var asset = new LocalizedFoldout {
				name = "foldout-asset",
				LangKey = "avatars.modules.parameters.asset"
			};
			asset.AddToClassList("foldout-flush");
			asset.AddToClassList("m-8");
			Content.Add(asset);

			var property = new PropertyField { name = "parameters-property" };
			property.BindProperty(serializedObject.FindProperty(nameof(AvatarParameterModule.parameters)));
			asset.Add(property);

			// The live values in a second section, titled with their count.
			_values = new LocalizedFoldout {
				name = "foldout-values",
				LangKey = "avatars.modules.parameters.values"
			};
			_values.AddToClassList("foldout-flush");
			_values.AddToClassList("m-8");
			Content.Add(_values);

			// The box that replaces the list when there is nothing to show: a notification of the CCK, whose icon
			// and accent come from the stylesheet (see NotificationElement).
			_info = new LocalizedNotificationElement { name = "parameters-info", Kind = NotificationKind.Info };
			_values.Add(_info);

			_list = new VisualElement { name = "parameters-list" };
			_list.AddToClassList("flex-col");
			_values.Add(_list);

			// The saved profiles in a third section: the savable parameters captured at a given time, applied
			// back later, and the templates the avatar ships with (see AvatarParameterProfiles).
			_saves = new LocalizedFoldout {
				name = "foldout-saves",
				LangKey = "avatars.modules.parameters.saves"
			};
			_saves.AddToClassList("foldout-flush");
			_saves.AddToClassList("m-8");
			Content.Add(_saves);

			_savesList = new VisualElement { name = "saves-list" };
			_savesList.AddToClassList("flex-col");
			_saves.Add(_savesList);

			_savesStatus = new LocalizedLabel { name = "saves-status" };
			_savesStatus.AddToClassList("value-row-detail");
			_saves.Add(_savesStatus);

			Refresh();
			EnsureTick();

			return root;
		}

		protected override void OnAttachToPanel(AttachToPanelEvent evt) {
			base.OnAttachToPanel(evt);
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			Target?.OnRegistred.AddListener(OnParameterChanged);
			Target?.OnUnRegistred.AddListener(OnParameterChanged);
		}

		protected override void OnDetachFromPanel(DetachFromPanelEvent evt) {
			base.OnDetachFromPanel(evt);
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			Target?.OnRegistred.RemoveListener(OnParameterChanged);
			Target?.OnUnRegistred.RemoveListener(OnParameterChanged);
		}

		private void OnPlayModeStateChanged(PlayModeStateChange state)
			=> Refresh();

		/// <summary>
		/// Applies the <c>lang-key</c> of the panel, then refreshes the tooltips of the category headers: their
		/// description is a second entry of the pack, so it follows the language through this pass.
		/// </summary>
		protected override void LocalizeTexts() {
			base.LocalizeTexts();

			foreach (var (header, category) in _headers)
				header.tooltip = ParameterCategoryLanguage.GetDescription(category);
		}

		/// <summary>
		/// A parameter was registered or unregistered (the module populates them once the avatar is loaded,
		/// which happens after this inspector was built): rebuild the list, once per batch.
		/// </summary>
		private void OnParameterChanged(IParameter _) {
			if (_list == null || !IsVisible || _rebuild)
				return;

			_rebuild = true;
			_list.schedule.Execute(() => {
				_rebuild = false;
				Refresh();
			});
		}

		// Back on screen: catch up with what was left alone while the inspector was hidden, and restart the
		// refresh of the live values.
		protected override void OnVisible() {
			Refresh();
			EnsureTick();
		}

		protected override void OnInvisible()
			=> _tick?.Pause();

		/// <summary>
		/// Starts (or restarts) the refresh of the live values. The item is scheduled on a panel element, so it
		/// stops by itself when the inspector is detached, and it has to be rebuilt after a rebuild of the panel.
		/// </summary>
		private void EnsureTick() {
			if (_list == null)
				return;

			if (_tick != null && _tickHost == _list) {
				_tick.Resume();
				return;
			}

			_tickHost = _list;
			_tick = _list.schedule.Execute(Tick).Every(TickMs);
		}

		/// <summary>Re-reads the live values while the inspector is displayed, and flashes the rows that moved.</summary>
		private void Tick() {
			if (!IsVisible || _rows.Count == 0)
				return;

			foreach (var row in _rows)
				if (row.Update())
					row.Flash();
		}

		/// <summary>Rebuilds the list from the parameters the module exposes right now.</summary>
		private void Refresh() {
			if (_list == null || !Target)
				return;

			_rows.Clear();
			_headers.Clear();
			_list.Clear();

			RefreshSaves();

			// The module reads the parameters of the animator: outside play mode, and before the avatar has
			// loaded, there is nothing to show.
			if (!Application.isPlaying) {
				ShowInfo("avatars.modules.parameters.play_mode");
				return;
			}

			var parameters = Target.GetParameters() ?? Array.Empty<IParameter>();
			if (parameters.Length == 0) {
				ShowInfo("avatars.modules.parameters.none");
				return;
			}

			if (Target.Runtime == null || !Target.Runtime.Descriptor?.Animator) {
				ShowInfo("avatars.modules.parameters.no_animator");
				return;
			}

			_values.LangArgs = new object[] { parameters.Length };
			_info.EnableInClassList("hidden", true);

			// The flags tell who may write a parameter: the owner (the player itself) or the viewers of it.
			var isLocal = parameters
					.FirstOrDefault(e => e.Name.Contains("IsLocal"))
					?.Value.ToBool()
				?? true;

			// Alphabetical, case-insensitive: the module registers the parameters in load order. The list is
			// split into one section per category, the Â« general Â» one last for the parameters that declare
			// no category, and each section is sorted by name.
			foreach (var group in GroupByCategory(parameters)) {
				_list.Add(CreateCategoryHeader(group.Key));

				foreach (var parameter in group.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)) {
					var editable = parameter.Flags.HasFlag(isLocal ? ParameterFlags.OwnerEditable : ParameterFlags.ViewerEditable);

					var row = ParameterValueRow.Build(
						parameter,
						readOnly: !editable,
						editor: p => MakeEditor(p, editable));

					_rows.Add(row);
					_list.Add(row.Container);
				}
			}
		}

		/// <summary>
		/// Splits the parameters into one group per category (snake_case), the parameters without a category
		/// first in the Â« general Â» group: the sections are ordered by category, not by their label (which
		/// depends on the language), then the rows by name.
		/// </summary>
		private static IEnumerable<IGrouping<string, IParameter>> GroupByCategory(IParameter[] parameters)
			=> parameters
				.Where(e => e != null)
				.GroupBy(e => ParameterCategoryLanguage.Normalize(e.Category))
				.OrderBy(g => g.Key == null ? 0 : 1)
				.ThenBy(g => g.Key ?? string.Empty, StringComparer.Ordinal);

		/// <summary>
		/// Header of a category section: the label entry of the category (<c>avatar.parameters.categories.&lt;category&gt;.label</c>),
		/// with the description entry as tooltip.
		/// </summary>
		private LocalizedLabel CreateCategoryHeader(string category) {
			var header = new LocalizedLabel { LangKey = ParameterCategoryLanguage.LabelKey(category) };
			header.AddToClassList("group-header");
			header.tooltip = ParameterCategoryLanguage.GetDescription(category);

			_headers.Add((header, category));

			return header;
		}

		/// <summary>Shows a language entry instead of the list, and drops the rows it described.</summary>
		private void ShowInfo(string key) {
			_rows.Clear();
			_headers.Clear();
			_list.Clear();

			_values.LangArgs = new object[] { 0 };

			_info.LangKey = key;
			_info.EnableInClassList("hidden", false);
		}

		/// <summary>
		/// Field that writes the parameter, shown in the popup of its row: the list itself stays a set of
		/// labels, so every row keeps the same size whatever the type it holds.
		/// </summary>
		/// <param name="parameter">The parameter the field writes.</param>
		/// <param name="editable">Whether the local player may write it (see the flags of the parameter).</param>
		private static VisualElement MakeEditor(IParameter parameter, bool editable) {
			var type = parameter.ValueType;
			var value = parameter.Value;

			// The row already carries the name: the field only shows the input.
			if (type == ParameterType.ByteArray)
				return ParameterFieldFactory.CreateField(null, type, editable, value, bytes => parameter.Value = bytes);

			var field = ParameterFieldFactory.CreateFieldByType(type, null, value, editable);
			ParameterFieldFactory.RegisterCallback(field, type, bytes => parameter.Value = bytes);

			return field;
		}

		//  Saved profiles

		/// <summary>
		/// Rebuilds the save section: the capture line (a name and a Save button), the profiles of the owner
		/// for this avatar, then the templates of the avatar.
		/// </summary>
		private void RefreshSaves() {
			if (_savesList == null)
				return;

			var previous = _savesName?.value;
			_savesList.Clear();

			var avatar = Target ? Target.Runtime : null;
			if (avatar == null) {
				_savesList.Add(Note("avatars.modules.parameters.saves.no_avatar"));
				return;
			}

			// The data of the avatar (profiles of the local user, templates of the avatar) is only read once.
			if (_avatar != avatar) {
				_avatar = avatar;
				LoadData(avatar).Forget();
			}

			// Capture line: the name of the profile to write, then its button. A remote avatar has no
			// writable data, so it only lists what its data already holds.
			if (avatar.IsLocal) {
				var capture = Row();
				_savesName = new TextField {
					name = "saves-name",
					value = string.IsNullOrEmpty(previous) ? "default" : previous,
					isDelayed = true
				};
				_savesName.AddToClassList("value-row-fill");
				capture.Add(_savesName);
				capture.Add(Action("avatars.modules.parameters.saves.save", () => SaveProfile(_savesName.value)));
				_savesList.Add(capture);
			} else
				_savesList.Add(Note("avatars.modules.parameters.saves.remote"));

			// Profiles of the local user for this avatar.
			var profiles = AvatarParameterProfiles.GetProfiles(avatar);
			_savesList.Add(Header("avatars.modules.parameters.saves.profiles", profiles.Length));

			if (profiles.Length == 0)
				_savesList.Add(Note("avatars.modules.parameters.saves.none"));
			else foreach (var profile in profiles) {
				var name = profile;
				_savesList.Add(Saved(
					name,
					() => LoadProfile(name),
					avatar.IsLocal ? () => DeleteProfile(name) : null
				));
			}

			// Templates of the avatar: shared by every user, so they cannot be deleted from here.
			var templates = AvatarParameterProfiles.GetTemplates(avatar);
			if (templates.Length == 0)
				return;

			_savesList.Add(Header("avatars.modules.parameters.saves.templates", templates.Length));

			foreach (var template in templates) {
				var name = template;
				_savesList.Add(Saved(name, () => LoadProfile(name), null));
			}
		}

		/// <summary>
		/// Reads the data of the avatar once (see <see cref="ISavableContainer.Load"/>: local file, or the
		/// table of the account when connected), then rebuilds the section.
		/// </summary>
		private async UniTaskVoid LoadData(IRuntimeAvatar avatar) {
			await avatar.Data.Load();
			_savesList?.schedule.Execute(RefreshSaves);
		}

		/// <summary>Captures the savable parameters as a profile of the local user for this avatar.</summary>
		private void SaveProfile(string name) {
			name = name?.Trim();
			if (string.IsNullOrEmpty(name))
				return;

			if (!Target.SaveProfile(name)) {
				Status("avatars.modules.parameters.saves.failed", name);
				return;
			}

			var count = AvatarParameterProfiles.Load(Target.Runtime, name)?.Count ?? 0;
			Status("avatars.modules.parameters.saves.saved", name, count);
			RefreshSaves();
		}

		/// <summary>Writes the values of a profile (or of a template) back into the parameters.</summary>
		private void LoadProfile(string name) {
			var count = Target.ApplyProfile(name);
			Status("avatars.modules.parameters.saves.loaded", name, count);
			Refresh();
		}

		/// <summary>Deletes a profile of the local user.</summary>
		private void DeleteProfile(string name) {
			AvatarParameterProfiles.Delete(Target.Runtime, name);
			RefreshSaves();
		}

		/// <summary>Shows the outcome of the last action under the section.</summary>
		private void Status(string key, string profile, int count = 0) {
			if (_savesStatus == null)
				return;

			_savesStatus.LangKey = key;
			_savesStatus.LangArgs = new object[] { profile, count };
		}

		/// <summary>Horizontal row of the save section: the same layout as a value row.</summary>
		private static VisualElement Row() {
			var row = new VisualElement();
			row.AddToClassList("value-row");
			return row;
		}

		/// <summary>Header of a subsection, with the number of entries it holds.</summary>
		private static LocalizedLabel Header(string key, int count) {
			var header = new LocalizedLabel { LangKey = key, LangArgs = new object[] { count } };
			header.AddToClassList("group-header");
			return header;
		}

		/// <summary>Small explanatory line of the save section.</summary>
		private static LocalizedLabel Note(string key) {
			var note = new LocalizedLabel { name = "saves-note", LangKey = key };
			note.AddToClassList("value-row-detail");
			return note;
		}

		/// <summary>Localized button of the save section.</summary>
		private static LocalizedButton Action(string key, Action clicked) {
			var button = new LocalizedButton { LangKey = key };
			button.clicked += clicked;
			return button;
		}

		/// <summary>Row of a saved profile (or of a template), with its own buttons.</summary>
		private static VisualElement Saved(string name, Action apply, Action delete) {
			var row = Row();

			var label = new Label(name) { name = "saves-entry" };
			label.AddToClassList("value-row-name");
			label.AddToClassList("value-row-fill");
			row.Add(label);

			row.Add(Action("avatars.modules.parameters.saves.load", apply));

			if (delete != null)
				row.Add(Action("avatars.modules.parameters.saves.delete", delete));

			return row;
		}

	}
}
