using System.Linq;
using Nox.CCK.Avatars.Menus;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nox.CCK.Avatars.Modules.Editor
{
	[CustomPropertyDrawer(typeof(MenuEntry))]
	public class MenuEntryDrawer : PropertyDrawer
	{
		public override VisualElement CreatePropertyGUI(SerializedProperty property) {
			var root = new VisualElement();

			var uxmlAsset = Resources.Load<VisualTreeAsset>("MenuEntryDrawer");
			if (uxmlAsset != null) {
				uxmlAsset.CloneTree(root);
			} else {
				root.Add(CreateFallbackGUI(property));
				return root;
			}

			var typeProp         = property.FindPropertyRelative(nameof(MenuEntry.type));
			var typeViewContainer = root.Q<VisualElement>("type-view-container");
			var typeField        = root.Q<PropertyField>("type-field");
			var parameterTypeProp = property.FindPropertyRelative(nameof(MenuEntry.parameterType));

			void RebuildTypeView() {
				if (typeViewContainer == null) return;
				typeViewContainer.Clear();
				if (typeProp == null || parameterTypeProp == null) return;
				var type = (EntryType)typeProp.enumValueIndex;
				var view = MenuEntryViews.CreateTypeView(type, property);
				foreach (var child in view.Children().ToArray()) {
					child.RemoveFromHierarchy();
					typeViewContainer.Add(child);
				}
			}

			RebuildTypeView();
			typeField?.RegisterValueChangeCallback(_ => RebuildTypeView());
			root.TrackPropertyValue(typeProp, _ => RebuildTypeView());
			root.TrackPropertyValue(parameterTypeProp, _ => RebuildTypeView());

			return root;
		}

		private VisualElement CreateFallbackGUI(SerializedProperty property) {
			var container = new VisualElement();
			container.Add(new PropertyField(property.FindPropertyRelative("name")));
			container.Add(new PropertyField(property.FindPropertyRelative(nameof(MenuEntry.icon))));
			container.Add(new PropertyField(property.FindPropertyRelative(nameof(MenuEntry.type))));
			container.Add(new PropertyField(property.FindPropertyRelative(nameof(MenuEntry.parameter))));
			container.Add(new PropertyField(property.FindPropertyRelative(nameof(MenuEntry.menu))));
			container.Add(new PropertyField(property.FindPropertyRelative(nameof(MenuEntry.values))));
			return container;
		}
	}
}
