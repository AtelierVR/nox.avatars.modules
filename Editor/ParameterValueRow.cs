using System;
using System.Globalization;
using System.Text;
using Nox.Avatars.Parameters;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;

namespace Nox.CCK.Avatars.Modules.Editor {
	/// <summary>
	/// Renders the value of a parameter the way the avatar parameter inspector does: the name on the left,
	/// the current value in the middle, the badges (state, then type) on the right — plus the technical
	/// details (the synchronization key and the serialized buffer) on hover.
	/// <para>
	/// Both texts are elided when the panel is too narrow; the full name, the type, the key and the buffer
	/// are shown in a popup when the row is clicked (see <see cref="AddDetailsPopup"/>).
	/// </para>
	/// <para>
	/// Shared by the inspectors that display live parameter values (the avatar parameter module,
	/// the rig manager), so they all show the same thing in the same way.
	/// </para>
	/// </summary>
	public static class ParameterValueRow {
		/// <summary>Vertical gap between a row and its popup, in points.</summary>
		private const float PopupGap = 2f;

		/// <summary>Narrowest the popup gets, for a very thin inspector.</summary>
		private const float PopupMinWidth = 120f;

		/// <summary>How long a row keeps the « just changed » class before the background fades out.</summary>
		private const int FlashMs = 60;

		/// <summary>Language entry of the badge of a parameter the local player cannot write.</summary>
		private const string ReadOnlyKey = "avatars.modules.parameters.readonly";

		/// <summary>The details popup, shared by every row.</summary>
		private static VisualElement _popup;

		/// <summary>Line of technical details inside the popup (name, type, key, buffer).</summary>
		private static Label _popupInfo;

		/// <summary>Slot of the popup that holds the editor of the parameter, when the row provides one.</summary>
		private static VisualElement _popupField;

		/// <summary>Parameter the popup currently shows, so a tick can refresh its text.</summary>
		private static IParameter _popupParameter;

		/// <summary>Row whose popup is open, marked as selected while it is.</summary>
		private static Row _selectedRow;

		/// <summary>The root of the window the « click elsewhere closes the popup » handler sits on.</summary>
		private static VisualElement _hookedPanel;

		/// <summary>Places the popup again while it is open, so it follows its row when the inspector scrolls.</summary>
		private static IVisualElementScheduledItem _movement;

		/// <summary>Element <see cref="_movement"/> is scheduled on, rebuilt with the popup.</summary>
		private static VisualElement _movementHost;

		/// <summary>
		/// Row of a parameter: its name on the left, its value in the middle, the state and type badges on
		/// the right.
		/// </summary>
		/// <param name="parameter">The parameter to read.</param>
		/// <param name="showType">Whether the type is shown as a badge at the right end of the row.</param>
		/// <param name="state">
		/// Optional state shown as a badge next to the value (the tracking state of a bone, the
		/// fact that a backend is the active one...). <c>null</c> for no badge.
		/// </param>
		/// <param name="stateKey">
		/// Language entry of the state, so it follows the language changes. <c>null</c> when the
		/// state is already a plain text.
		/// </param>
		/// <param name="readOnly">Adds the « read-only » badge: the local player cannot write this parameter.</param>
		/// <param name="editor">
		/// Builds the editor shown in the popup of the row, so the value can be written from there instead of
		/// from a field in the list. <c>null</c> for a row that only displays the value.
		/// </param>
		public static VisualElement Create(
			IParameter parameter,
			bool showType = true,
			string state = null,
			string stateKey = null,
			bool readOnly = false,
			Func<IParameter, VisualElement> editor = null)
			=> Build(parameter, showType, state, stateKey, readOnly, editor).Container;

		/// <summary>
		/// Builds the row and returns it instead of its element only, so an inspector that follows a live
		/// value keeps it and refreshes it in place (see <see cref="Row.Update"/>) instead of rebuilding
		/// the whole list on every tick.
		/// </summary>
		/// <inheritdoc cref="Create"/>
		public static Row Build(
			IParameter parameter,
			bool showType = true,
			string state = null,
			string stateKey = null,
			bool readOnly = false,
			Func<IParameter, VisualElement> editor = null) {
			var entry     = new Row { Parameter = parameter, Editor = editor };
			var container = new VisualElement { name = "parameter-field" };
			container.AddToClassList("parameter-field");
			entry.Container = container;

			if (parameter == null) {
				container.Add(Unavailable());
				return entry;
			}

			var row = new VisualElement { name = "value-row" };
			row.AddToClassList("value-row");

			var name = new Label(parameter.Name) { name = "value-row-name" };
			name.AddToClassList("value-row-name");
			row.Add(name);

			var value = new Label { name = "value-row-value" };
			value.AddToClassList("value-row-value");
			value.text = ValueOf(parameter);
			row.Add(value);

			entry.Name  = name;
			entry.Value = value;
			entry.Text  = value.text;

			// Badges on the right of the row, the type last: the state tells what the entry is doing, the
			// type only what it is, so the type is the one pushed to the far right.
			if (stateKey != null) {
				var badge = new LocalizedLabel { name = "value-row-state" };
				badge.AddToClassList("value-row-state");
				badge.LangKey = stateKey;
				row.Add(badge);
				entry.State = badge;
			} else if (!string.IsNullOrEmpty(state)) {
				var badge = new Label(state) { name = "value-row-state" };
				badge.AddToClassList("value-row-state");
				row.Add(badge);
				entry.State = badge;
			}

			if (readOnly) {
				var badge = new LocalizedLabel { name = "value-row-readonly" };
				badge.AddToClassList("value-row-readonly");
				badge.LangKey = ReadOnlyKey;
				row.Add(badge);
			}

			if (showType) {
				var type = new Label(parameter.ValueType.ToString()) { name = "value-row-type" };
				type.AddToClassList("value-row-type");

				// The badge carries the colour of the type (see Palette): a fixed hue per type, so a row is
				// identified at a glance. The USS class keeps its size, its border and its radius.
				var typeColor = Palette.ColorOf(parameter.ValueType);
				type.style.backgroundColor = typeColor;
				type.style.color = Palette.TextColorFor(typeColor);

				row.Add(type);
				entry.Type = type;
			}

			container.Add(row);

			AddDetailsPopup(container, entry);

			AddCopyMenu(container, entry);

			return entry;
		}

		/// <summary>
		/// Row of a parameter kept by the inspector that displays it: refreshing it only touches the labels
		/// that actually changed, and flashes the row of a value that just moved.
		/// </summary>
		public sealed class Row {
			public VisualElement Container;
			public Label         Name;
			public Label         Value;
			public Label         Type;
			public Label         State;
			public IParameter    Parameter;

			/// <summary>Builds the editor shown in the popup, or <c>null</c> for a display-only row.</summary>
			public Func<IParameter, VisualElement> Editor;

			/// <summary>Text of the value last rendered, to tell a real change from a repaint.</summary>
			public string Text;

			/// <summary>
			/// Reads the parameter again and updates the row when its value moved. Returns <c>true</c> when it
			/// did, so the caller can flash the row.
			/// </summary>
			public bool Update() {
				if (Value == null)
					return false;

				var text = ValueOf(Parameter);
				if (string.Equals(text, Text, StringComparison.Ordinal))
					return false;

				Text       = text;
				Value.text = text;

				// The name and the key never move but the serialized buffer does: keep the popup in sync when
				// it is the one this row displays.
				if (_popupParameter == Parameter && _popupInfo != null)
					_popupInfo.text = BuildTooltip(Parameter);

				return true;
			}

			/// <summary>
			/// Flashes the row: the class is added for a few milliseconds then removed, and the background fades
			/// out through the transition declared in <c>value.uss</c> — no colour is written from the code.
			/// </summary>
			public void Flash() {
				Container.AddToClassList("value-row-updated");
				Container
					.schedule
					.Execute(() => Container.RemoveFromClassList("value-row-updated"))
					.StartingIn(FlashMs);
			}
		}

		/// <summary>
		/// Current value of a parameter, in a form that fits in a line. Falls back to a dash when
		/// the value cannot be read (the module that carried it was destroyed).
		/// </summary>
		public static string ValueOf(IParameter parameter) {
			if (parameter == null)
				return "-";

			try {
				var value = parameter.Value;
				return value switch {
					null           => "-",
					Vector3 v      => Format(v.x) + " / " + Format(v.y) + " / " + Format(v.z),
					Quaternion q   => Format(q.x) + " / " + Format(q.y) + " / " + Format(q.z) + " / " + Format(q.w),
					byte[] bytes   => Convert.ToBase64String(bytes),
					_ => Convert.ToString(value, CultureInfo.InvariantCulture)
				};
			} catch (Exception) {
				return "—";
			}
		}

		/// <summary>
		/// Tooltip of a parameter, shown when the row is hovered: the full name (the row elides it), the type,
		/// then the details the row does not show — the synchronization key (as it travels on the network) and
		/// the serialized buffer, exactly as in <c>PropertiesRequest</c> (big-endian, see
		/// <c>Serializer.ToBytes</c>).
		/// </summary>
		public static string BuildTooltip(IParameter parameter) {
			if (parameter == null)
				return string.Empty;

			var sb = new StringBuilder();

			try {
				sb.Append("Name: ").Append(parameter.Name);
				sb.Append("\nType: ").Append(parameter.ValueType);
			} catch (Exception) {
				return $"{nameof(IParameter)} unavailable (module destroyed)";
			}

			if (!TryRead(parameter, out var key, out var buffer))
				return sb.Append("\nKey: —\nBuffer: —").ToString();

			sb.Append("\nKey: ").Append(key).Append(" (0x").Append(unchecked((uint)key).ToString("X8")).Append(')');
			sb.Append("\nBuffer: ").Append(HexOf(buffer));

			return sb.ToString();
		}

		/// <summary>Buffer of a parameter as one hexadecimal word, e.g. <c>0x0000C03F</c>.</summary>
		public static string HexOf(byte[] buffer)
			=> buffer == null || buffer.Length == 0
				? "0x"
				: "0x" + BitConverter.ToString(buffer)
					.Replace("-", string.Empty);

		/// <summary>Serialized buffer of a parameter, or a dash when it cannot be read anymore.</summary>
		public static string BufferOf(IParameter parameter)
			=> TryRead(parameter, out _, out var buffer)
				? HexOf(buffer)
				: "—";

		/// <summary>
		/// Opens the details of the row in a popup when it is clicked, and closes it on the next click either on
		/// the row (a toggle) or anywhere else in the window. The row is marked as selected while the popup is
		/// open, through a border colour: the background carries the « just changed » highlight, so the two
		/// states stay visible at the same time.
		/// </summary>
		private static void AddDetailsPopup(VisualElement container, Row entry) {
			container.RegisterCallback<PointerUpEvent>(evt => {
				if (evt.button != 0)
					return;

				if (_selectedRow == entry)
					HidePopup();
				else
					Select(entry);
			});

			container.RegisterCallback<DetachFromPanelEvent>(_ => {
				if (_selectedRow == entry)
					HidePopup();
			});
		}

		/// <summary>Marks the row of <paramref name="entry"/> as selected and opens its popup.</summary>
		private static void Select(Row entry) {
			var panel = entry?.Container?.panel;
			if (panel == null)
				return;

			_selectedRow?.Container?.RemoveFromClassList("value-row-selected");

			_selectedRow = entry;
			entry.Container.AddToClassList("value-row-selected");

			HookPanel(panel);
			ShowPopup(entry);
		}

		/// <summary>
		/// Closes the popup when the pointer goes down somewhere else — including in another inspector, which is
		/// why the handler sits on the window and not on the row.
		/// </summary>
		private static void HookPanel(IPanel panel) {
			if (_hookedPanel == panel.visualTree)
				return;

			_hookedPanel = panel.visualTree;

			panel.visualTree.RegisterCallback<PointerDownEvent>(evt => {
				var row = _selectedRow?.Container;
				if (row == null)
					return;

				// A press on the row is the click that may toggle it, and a press in the popup is the one that
				// uses the editor it holds: neither closes anything.
				if (evt.target is VisualElement element && (IsInside(row, element) || IsInside(_popup, element)))
					return;

				HidePopup();
			});
		}

		/// <summary>Whether <paramref name="element"/> is <paramref name="root"/> or one of its descendants.</summary>
		private static bool IsInside(VisualElement root, VisualElement element)
			=> root != null && element != null && (element == root || root.Contains(element));

		/// <summary>Opens the popup of <paramref name="entry"/> on top of the window.</summary>
		private static void ShowPopup(Row entry) {
			var parameter = entry?.Parameter;
			var panel     = entry?.Container?.panel;
			if (parameter == null || panel == null)
				return;

			var host = panel.visualTree;

			// The popup belongs to the window, not to the row: it must not be clipped by the scroll views of
			// the inspector, nor scroll away with its content. Rebuilt when the window changed (another
			// inspector, a domain reload).
			if (_popup == null || _popup.panel != panel) {
				// Another window still holds the previous popup: drop it, one popup is enough.
				_popup?.RemoveFromHierarchy();

				_popup = CreatePopup(entry.Container);
				host.Add(_popup);
			}

			_popupParameter = parameter;
			_popupInfo.text = BuildTooltip(parameter);
			_popup.BringToFront();
			_popup.EnableInClassList("hidden", false);

			// The editor of the parameter, when the row provides one: the field lives in the popup instead of in
			// the list, so every row stays a compact label, whatever it edits.
			_popupField.Clear();
			if (entry.Editor != null && entry.Editor(parameter) is { } editor)
				_popupField.Add(editor);

			_popupField.EnableInClassList("hidden", _popupField.childCount == 0);

			Reposition();

			// The popup follows its row: a scroll moves the row without raising any layout event, so the place
			// of the popup is read from the row again on a timer while it is open.
			if (_movement == null || _movementHost != _popup) {
				_movementHost = _popup;
				_movement     = _popup.schedule.Execute(Reposition).Every(16);
			} else {
				_movement.Resume();
			}
		}

		/// <summary>
		/// Places the popup on its row: the same width, right under it — or right over it when the row is too low
		/// in the window for the content — with the height its content needs.
		/// </summary>
		private static void Reposition() {
			var row = _selectedRow?.Container;
			if (_popup == null || row?.panel == null)
				return;

			// The row scrolled out of the view: the popup would end up pinned to an edge of the window, far
			// from what it describes, so it is dropped — the row has to be clicked again to get it back.
			if (!IsInView(row)) {
				HidePopup();
				return;
			}

			var frame  = row.panel.visualTree.worldBound;
			var bounds = row.worldBound;

			var width = Mathf.Max(PopupMinWidth, bounds.width);
			if (!Mathf.Approximately(_popup.resolvedStyle.width, width))
				_popup.style.width = width;

			// The height comes from the content: read once it is laid out, guessed on the very first frame.
			var height = _popup.resolvedStyle.height;
			if (float.IsNaN(height) || height <= 0f)
				height = 20f + 14f * _popupInfo.text.Split('\n').Length;

			var below = bounds.yMax - frame.y + PopupGap;
			var above = bounds.yMin - frame.y - height - PopupGap;

			// Below the row, like an accordion opened from it; above when there is no room left underneath.
			var top  = below + height <= frame.height ? below : above;
			var left = Mathf.Clamp(bounds.x - frame.x, 0f, Mathf.Max(0f, frame.width - width));
			top      = Mathf.Clamp(top, 0f, Mathf.Max(0f, frame.height - height));

			// Only written when it moves: an inline style written every frame would dirty the layout forever.
			if (float.IsNaN(_popup.resolvedStyle.left) || Mathf.Abs(_popup.resolvedStyle.left - left) > 0.5f)
				_popup.style.left = left;

			if (float.IsNaN(_popup.resolvedStyle.top) || Mathf.Abs(_popup.resolvedStyle.top - top) > 0.5f)
				_popup.style.top = top;
		}

		/// <summary>
		/// Whether the row is inside the area its window and its scroll views actually show. An inspector is a
		/// scroll view: a row can be laid out and still be out of the visible part, above or below it.
		/// </summary>
		private static bool IsInView(VisualElement element) {
			var area = element.worldBound;
			if (area.width <= 0f || area.height <= 0f)
				return false;

			for (var parent = element.parent; parent != null; parent = parent.parent) {
				if (parent.resolvedStyle.display == DisplayStyle.None || parent.resolvedStyle.visibility == Visibility.Hidden)
					return false;

				// The row has to overlap the visible part of the list it belongs to, not just the window.
				if (parent is ScrollView scrollView) {
					var viewport = scrollView.contentViewport.worldBound;
					if (area.yMax <= viewport.yMin || area.yMin >= viewport.yMax)
						return false;
				}
			}

			return true;
		}

		/// <summary>Hides the popup and clears the selected state of its row.</summary>
		private static void HidePopup() {
			_popupParameter = null;

			_movement?.Pause();

			_selectedRow?.Container?.RemoveFromClassList("value-row-selected");
			_selectedRow = null;

			if (_popup != null)
				_popup.EnableInClassList("hidden", true);
		}

		/// <summary>
		/// Creates the popup: the box of <c>value.uss</c> holding the technical details and, when the row provides
		/// one, the editor of the parameter.
		/// </summary>
		/// <param name="row">Row the popup describes: its stylesheets are copied onto the popup.</param>
		private static VisualElement CreatePopup(VisualElement row) {
			var popup = new VisualElement { name = "value-row-popup" };
			popup.AddToClassList("value-row-popup");
		popup.AddToClassList("hidden");

			AttachStyles(popup, row);

			_popupInfo = new Label { name = "value-row-details", pickingMode = PickingMode.Ignore };
			_popupInfo.AddToClassList("value-row-details");
			popup.Add(_popupInfo);

			_popupField = new VisualElement { name = "value-row-editor" };
			_popupField.AddToClassList("value-row-editor");
			popup.Add(_popupField);

			return popup;
		}

		/// <summary>
		/// Copies the stylesheets of the row onto the popup. The popup hangs from the window root, outside the
		/// subtree the sheets were attached to, so it would inherit none of them: no theme (hence no font and no
		/// rule for the Unity fields it may hold) and none of the CCK classes.
		/// </summary>
		private static void AttachStyles(VisualElement popup, VisualElement row) {
			for (var element = row; element != null; element = element.parent) {
				var sheets = element.styleSheets;
				for (var i = 0; i < sheets.count; i++) {
					var sheet = sheets[i];
					if (sheet != null && !popup.styleSheets.Contains(sheet))
						popup.styleSheets.Add(sheet);
				}
			}
		}

		/// <summary>
		/// Right-click menu of the row: copies the name, the key, the value or the buffer of the parameter. The
		/// parameter is read on click, so a copy always carries what is displayed at that moment.
		/// </summary>
		private static void AddCopyMenu(VisualElement container, Row entry)
			=> container.AddManipulator(new ContextualMenuManipulator(evt => {
				evt.menu.AppendAction("Copy Name", _ => Copy(entry, parameter => parameter.Name));
				evt.menu.AppendAction("Copy Key", _ => Copy(entry, parameter => parameter.Key.ToString()));
				evt.menu.AppendAction("Copy Value", _ => Copy(entry, parameter => ValueOf(parameter)));
				evt.menu.AppendAction("Copy Buffer", _ => Copy(entry, parameter => BufferOf(parameter)));
			}));

		/// <summary>Writes what <paramref name="read"/> returns into the system clipboard, when it can be read.</summary>
		private static void Copy(Row entry, Func<IParameter, string> read) {
			var parameter = entry?.Parameter;
			if (parameter == null)
				return;

			try {
				EditorGUIUtility.systemCopyBuffer = read(parameter);
			} catch (Exception) {
				// The module was destroyed under us (avatar swap): there is nothing to copy.
			}
		}

		/// <summary>
		/// Compact version on a single line, shown inline under the value during the hover.
		/// </summary>
		public static string BuildDetail(IParameter parameter) {
			if (parameter == null)
				return string.Empty;

			if (!TryRead(parameter, out var key, out var buffer))
				return "(valeur indisponible : module détruit)";

			var sb = new StringBuilder();
			sb.Append("Key ").Append(key).Append(" (0x").Append(unchecked((uint)key).ToString("X8")).Append(')');
			sb.Append("   ·   Buffer ").Append(buffer.Length).Append(" B : ");
			sb.Append(buffer.Length > 0 ? BitConverter.ToString(buffer).Replace('-', ' ') : "—");

			return sb.ToString();
		}

		/// <summary>
		/// Reads the key and the serialized buffer of a parameter. Returns <c>false</c> when the
		/// module that carried it was destroyed (avatar swap) — the caller then shows a
		/// placeholder instead of letting the exception reach the refresh.
		/// </summary>
		private static bool TryRead(IParameter parameter, out int key, out byte[] buffer) {
			key    = 0;
			buffer = Array.Empty<byte>();

			if (parameter == null)
				return false;

			try {
				key    = parameter.Key;
				buffer = parameter.Value.ToBytes() ?? Array.Empty<byte>();
				return true;
			} catch (Exception) {
				return false;
			}
		}

		/// <summary>Line stating the value cannot be read.</summary>
		private static Label Unavailable() {
			var label = new Label("(valeur indisponible : module détruit)") { name = "value-unavailable" };
			label.AddToClassList("value-unavailable");
			return label;
		}

		/// <summary>Formats a float without the trailing zeros of the invariant culture.</summary>
		private static string Format(float value)
			=> value.ToString("0.###", CultureInfo.InvariantCulture);
	}
}
