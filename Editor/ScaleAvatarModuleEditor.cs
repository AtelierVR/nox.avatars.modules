using Nox.CCK.Avatars.Scale;
using Nox.CCK.Editor;
using Nox.Search;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nox.CCK.Avatars.Modules.Editor {
	/// <summary>
	/// Inspector of the scale module. It serializes nothing — the scale follows the height of the player while
	/// the avatar runs — so the panel is the header of the module and a line saying there is nothing to set.
	/// </summary>
	public class ScaleAvatarModuleEditor : LocalizedInspectorEditor<ScaleAvatarModule> {
		/// <summary>Prefix of the language entries of this inspector (see the mod lang.asset).</summary>
		private const string LangPrefix = "avatars.modules.scale.inspector";

		/// <summary>Icon of the component: the « straighten » glyph rendered in the assets of the <c>ui</c> mod.</summary>
		protected override ResourceTextureSource Icon
			=> "ui:icons/scale_avatar.png";

		protected override string TitleKey
			=> LangPrefix + ".title";

		public override VisualElement CreateInspectorGUI() {
			var root = base.CreateInspectorGUI();
			if (Content == null)
				return root;

			// The GUI is rebuilt on every hierarchy change while the base keeps its root: never add twice.
			Content.Clear();

			var uxml = Resources.Load<VisualTreeAsset>(nameof(ScaleAvatarModuleEditor));
			if (uxml == null) {
				Content.Add(new Label($"The {nameof(ScaleAvatarModuleEditor)} uxml template is not found in Resources."));
				return root;
			}

			Content.Add(uxml.CloneTree());
			return root;
		}
	}
}
