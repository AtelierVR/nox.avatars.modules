using System;
using Nox.CCK.Avatars.Menus;
using Nox.CCK.Avatars.Parameters;
using Nox.CCK.Editor;
using UnityEditor;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nox.CCK.Avatars.Modules.Editor
{
	[UnityEditor.CustomEditor(typeof(AvatarMenuModule))]
	public class AvatarMenuModuleEditor : InspectorEditor<AvatarMenuModule>
	{
		private VisualElement _base;
		private PropertyField _property;
		
		public override VisualElement CreateInspectorGUI() {
			var root = base.CreateInspectorGUI();

			_base = Resources
				.Load<VisualTreeAsset>("AvatarMenuModuleEditor")
				.CloneTree();

			_property   = _base.Q<PropertyField>("menu-property");
			_property.BindProperty(serializedObject.FindProperty("menu"));

			Content.Add(_base);
			return root;
		}
	}
}