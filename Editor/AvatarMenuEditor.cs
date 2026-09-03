using Nox.CCK.Avatars.Menus;
using Nox.CCK.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nox.CCK.Avatars.Modules.Editor
{
	[CustomEditor(typeof(AvatarMenu))]
	public class AvatarMenuEditor : InspectorEditor<AvatarMenu>
	{
		private VisualElement _base;
		private PropertyField _entriesProperty;

		public override VisualElement CreateInspectorGUI() {
			var root = base.CreateInspectorGUI();

			var asset = Resources.Load<VisualTreeAsset>("AvatarMenuEditor");
			if (asset != null) {
				_base = asset.CloneTree();
			} else {
				_base = new VisualElement();
				var prop = new PropertyField(serializedObject.FindProperty("entries"), "Entries");
				_base.Add(prop);
			}

			_entriesProperty = _base.Q<PropertyField>("entries-property");
			_entriesProperty?.BindProperty(serializedObject.FindProperty("entries"));

			Content.Add(_base);
			return root;
		}
	}
}
