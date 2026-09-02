using System;
using Nox.UI;
using UnityEngine;

namespace Nox.CCK.Avatars.Menus {
	/// <summary>
	/// Entrée d'un menu d'avatar : correspond à un élément du menu radial.
	/// <list type="bullet">
	/// <item><see cref="RadialElementType.Menu"/> → sous-menu (via <see cref="submenu"/>) ;</item>
	/// <item><see cref="RadialElementType.Choice"/> ou Slider → contrôle d'un paramètre (via <see cref="parameter"/>) ;</item>
	/// <item>autre type (Button…) → action simple.</item>
	/// </list>
	/// </summary>
	[Serializable]
	public class MenuEntry {
		/// <summary>Libellé de l'élément.</summary>
		public string label;

		/// <summary>Icône de l'élément (peut être null).</summary>
		public Sprite icon;

		/// <summary>Type radial de l'élément.</summary>
		public RadialElementType type = RadialElementType.Button;

		/// <summary>Nom du paramètre de l'avatar lié au contrôle (Choice/Slider).</summary>
		public string parameter;

		/// <summary>Sous-menu cible (type Menu) : un autre asset AvatarMenu.</summary>
		public AvatarMenu submenu;
	}
}