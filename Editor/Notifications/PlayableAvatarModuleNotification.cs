using System.Linq;
using Nox.Assets.Editor;
using Nox.CCK.Avatars;
using Nox.CCK.Avatars.Playable;
using UnityEditor;

namespace Nox.CCK.Avatars.Modules.Editor {
	/// <summary>
	/// State of the <see cref="PlayableAvatarModule"/> of the selected avatar. Its <c>Check</c> never asks
	/// for one — the module is added along with the layers that need it — so only the empty case is worth
	/// reporting: a module without a layer controller plays nothing.
	/// </summary>
	public static class PlayableAvatarModuleNotification {
		private const string Uid = "avatars.modules.playable.none";

		[InitializeOnLoadMethod]
		private static void Initialize()
			=> AvatarModuleNotification.Watch(Update, watchProperties: true);

		private static void Update(AvatarDescriptor avatar) {
			var modules = AvatarModuleNotification.Modules<PlayableAvatarModule>(avatar);

			if (modules.Length == 0 || modules.Any(module => module && module.controllers is { Length: > 0 })) {
				AvatarModuleNotification.Hide(Uid);
				return;
			}

			AvatarModuleNotification.Show(
				Uid,
				AssetNotificationType.Info,
				"avatars.modules.playable.none",
				null,
				AvatarModuleNotification.Select(modules[0])
			);
		}
	}
}
