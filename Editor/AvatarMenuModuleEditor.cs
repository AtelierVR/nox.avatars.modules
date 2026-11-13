using System;
using Nox.CCK.Avatars.Menus;
using Nox.CCK.Avatars.Parameters;
using Nox.CCK.Editor;
using Nox.Search;
using UnityEditor;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nox.CCK.Avatars.Modules.Editor
{
	/// <summary>Inspector of the avatar menu module: the menu asset the player menu is built from.</summary>
	public class AvatarMenuModuleEditor : LocalizedInspectorEditor<AvatarMenuModule>
	{
		/// <summary>Icon of the component: the « menu » glyph rendered in the assets of the <c>ui</c> mod.</summary>
		protected override ResourceTextureSource Icon
			=> "ui:icons/avatar_menu.png";

		protected override string TitleKey
			=> "avatars.modules.menu.inspector.title";

		private VisualElement _base;
		private PropertyField _property;
		
		public override VisualElement CreateInspectorGUI() {
			var root = base.CreateInspectorGUI();
			if (Content == null)
				return root;

			// The GUI is rebuilt on every hierarchy change while the base keeps its root: never add twice.
			Content.Clear();

			var uxml = Resources.Load<VisualTreeAsset>(nameof(AvatarMenuModuleEditor));
			if (uxml == null) {
				Content.Add(new Label($"The {nameof(AvatarMenuModuleEditor)} uxml template is not found in Resources."));
				return root;
			}

			_base = uxml.CloneTree();

			_property = _base.Q<PropertyField>("menu-property");
			if (_property != null)
				_property.BindProperty(serializedObject.FindProperty(nameof(AvatarMenuModule.menu)));

			Content.Add(_base);
			return root;
		}
	}
}