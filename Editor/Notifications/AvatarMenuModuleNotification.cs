using Nox.Assets.Editor;
using Nox.CCK.Avatars;
using Nox.CCK.Avatars.Menus;
using UnityEditor;

namespace Nox.CCK.Avatars.Modules.Editor {
	/// <summary>
	/// State of the <see cref="AvatarMenuModule"/> of the selected avatar: its <c>Check</c> adds it when
	/// there is none and fails with several, which blocks the build. Without a menu asset the module serves
	/// nothing, which is only worth knowing.
	/// </summary>
	public static class AvatarMenuModuleNotification {
		private const string ModuleUid = "avatars.modules.menus.module";
		private const string NoMenuUid = "avatars.modules.menus.no_menu";
		private const string Module    = "Menus";

		[InitializeOnLoadMethod]
		private static void Initialize()
			=> AvatarModuleNotification.Watch(Update, watchProperties: true);

		private static void Update(AvatarDescriptor avatar) {
			var modules = AvatarModuleNotification.Required<AvatarMenuModule>(
				avatar,
				ModuleUid,
				Module,
				"avatars.modules.menus.missing",
				() => Update(avatar)
			);

			if (modules.Length != 1 || modules[0].menu) {
				AvatarModuleNotification.Hide(NoMenuUid);
				return;
			}

			AvatarModuleNotification.Show(
				NoMenuUid,
				AssetNotificationType.Info,
				"avatars.modules.menus.no_menu",
				null,
				AvatarModuleNotification.Select(modules[0])
			);
		}
	}
}
