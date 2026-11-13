using System.Linq;
using Nox.Assets.Editor;
using Nox.CCK.Avatars;
using Nox.CCK.Avatars.Proxies;
using UnityEditor;

namespace Nox.CCK.Avatars.Modules.Editor {
	/// <summary>
	/// State of the <see cref="ProxiesAvatarModule"/> of the selected avatar. It has no <c>Check</c> — it is
	/// purely optional — so only the empty case is worth reporting: a module without a replacement clip
	/// changes nothing in the animation graph.
	/// </summary>
	public static class ProxiesAvatarModuleNotification {
		private const string Uid = "avatars.modules.proxies.none";

		[InitializeOnLoadMethod]
		private static void Initialize()
			=> AvatarModuleNotification.Watch(Update, watchProperties: true);

		private static void Update(AvatarDescriptor avatar) {
			var modules = AvatarModuleNotification.Modules<ProxiesAvatarModule>(avatar);

			if (modules.Length == 0 || modules.Any(module => module && module.clips is { Length: > 0 })) {
				AvatarModuleNotification.Hide(Uid);
				return;
			}

			AvatarModuleNotification.Show(
				Uid,
				AssetNotificationType.Info,
				"avatars.modules.proxies.none",
				null,
				AvatarModuleNotification.Select(modules[0])
			);
		}
	}
}
