using Nox.CCK.Avatars.Playable;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Nox.CCK.Avatars.Modules.Editor {
	/// <summary>
	/// Draws a <see cref="PlayableLayer"/>: the controller, the key naming mode and,
	/// when the mode is Custom, the custom name field.
	/// </summary>
	[CustomPropertyDrawer(typeof(PlayableLayer))]
	public class PlayableLayerDrawer : PropertyDrawer {
		public override VisualElement CreatePropertyGUI(SerializedProperty property) {
			var root = new VisualElement();

			var controllerProp = property.FindPropertyRelative(nameof(PlayableLayer.controller));
			var namingProp     = property.FindPropertyRelative(nameof(PlayableLayer.naming));
			var customProp     = property.FindPropertyRelative(nameof(PlayableLayer.custom));

			root.Add(new PropertyField(controllerProp, "Controller"));
			root.Add(new PropertyField(namingProp, "Key"));

			var customField = new PropertyField(customProp, "Custom Name");
			root.Add(customField);

			void Refresh() {
				var mode = (PlayableLayerNaming)namingProp.enumValueIndex;

				customField.style.display = mode == PlayableLayerNaming.Custom
					? DisplayStyle.Flex
					: DisplayStyle.None;
			}

			Refresh();
			root.TrackPropertyValue(namingProp, _ => Refresh());

			return root;
		}
	}
}
