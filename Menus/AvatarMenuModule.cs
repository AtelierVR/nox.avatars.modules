using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Menus;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.Menus {
	/// <summary>
	/// Module du menu radial d'avatar : expose le menu racine de l'avatar via
	/// <see cref="IMenuModule.Menu"/> (un <see cref="IMenuEntry"/> construit depuis
	/// l'asset <see cref="AvatarMenu"/>). Les pages radiales sont générées côté
	/// client par AvatarRadialPage (converter vers nox.ui).
	/// </summary>
	public class AvatarMenuModule : MonoBehaviour, IMenuModule {
		public AvatarMenu menu;

		public int Priority
			=> int.MaxValue;

		public async UniTask<bool> Setup(IRuntimeAvatar runtime, AvatarModulePhase phase, CancellationToken token = default) {
			await UniTask.Yield(cancellationToken: token);
			if (phase != AvatarModulePhase.Init)
				return true;
			if (!menu)
				menu = ScriptableObject.CreateInstance<AvatarMenu>();
			return true;
		}

		IMenuEntry IMenuModule.Menu
			=> menu != null ? new MenuAdapter(menu) : null;

		public static bool Check(IAvatarDescriptor descriptor) {
			var modules = descriptor.GetModules<AvatarMenuModule>();

			var module = modules.Length switch {
				1 => modules.FirstOrDefault(),
				0 => descriptor.Anchor.AddComponent<AvatarMenuModule>(),
				_ => null
			};

			if (!module) {
				Logger.LogError("Verify that the Avatar prefab has a valid AvatarMenuModule component.");
				return false;
			}

			return true;
		}

		/// <summary>Adapte un asset AvatarMenu (CCK) en IMenuEntry (SDK Avatars.Menus).</summary>
		private sealed class MenuAdapter : IMenuEntry {
			private readonly AvatarMenu _menu;

			public MenuAdapter(AvatarMenu menu) {
				_menu = menu;
			}

			public int Id
				=> _menu ? _menu.name.GetHashCode() : 0;

			public string Label
				=> _menu ? _menu.name : string.Empty;

			public Sprite Icon
				=> null;

			public IEntry[] Entries {
				get {
					if (_menu == null || _menu.entries == null)
						return Array.Empty<IEntry>();
					return _menu.entries
						.Where(entry => entry != null)
						.Select(Convert)
						.ToArray();
				}
			}

			private static IEntry Convert(MenuEntry entry) {
				// Sous-menu → sous-menu AvatarMenu (IMenuEntry).
				if (entry.submenu != null)
					return new MenuAdapter(entry.submenu);

				// Paramètre → toggle (IToggleEntry).
				if (!string.IsNullOrEmpty(entry.parameter))
					return new ToggleAdapter(entry);

				// Entrée simple.
				return new LeafAdapter(entry);
			}
		}

		/// <summary>Adapte une entrée paramètre en IToggleEntry (On/Off par défaut 1/0).</summary>
		private sealed class ToggleAdapter : IToggleEntry {
			private readonly MenuEntry _entry;

			public ToggleAdapter(MenuEntry entry) {
				_entry = entry;
			}

			public int Id
				=> _entry != null ? _entry.label.GetHashCode() : 0;

			public string Label
				=> _entry?.label ?? string.Empty;

			public Sprite Icon
				=> _entry?.icon;

			public string Parameter
				=> _entry?.parameter ?? string.Empty;

			public byte[] On
				=> new byte[] { 1 };

			public byte[] Off
				=> new byte[] { 0 };
		}

		/// <summary>Adapte une entrée simple en IEntry.</summary>
		private sealed class LeafAdapter : IEntry {
			private readonly MenuEntry _entry;

			public LeafAdapter(MenuEntry entry) {
				_entry = entry;
			}

			public int Id
				=> _entry != null ? _entry.label.GetHashCode() : 0;

			public string Label
				=> _entry?.label ?? string.Empty;

			public Sprite Icon
				=> _entry?.icon;
		}
	}
}
