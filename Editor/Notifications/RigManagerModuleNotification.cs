using Nox.Avatars.Rigging;
using Nox.CCK.Avatars;
using Nox.CCK.Avatars.Rigging;
using UnityEditor;

namespace Nox.CCK.Avatars.Modules.Editor {
	/// <summary>
	/// State of the <see cref="RigManagerModule"/> of the selected avatar. Its <c>Check</c> only adds it
	/// when the avatar has no rig provider at all — a provider is what the manager drives — so the entry is
	/// a warning with an add action, and several managers are an error like everywhere else.
	/// </summary>
	public static class RigManagerModuleNotification {
		private const string Uid    = "avatars.modules.rigging.module";
		private const string Module = "Rig manager";

		[InitializeOnLoadMethod]
		private static void Initialize()
			=> AvatarModuleNotification.Watch(Update);

		private static void Update(AvatarDescriptor avatar) {
			if (!avatar) {
				AvatarModuleNotification.Hide(Uid);
				return;
			}

			// Nothing to report when a provider is there: the manager is then created by the check itself
			// and drives what it found.
			if (avatar.Anchor && avatar.Anchor.GetComponentsInChildren<IRigProvider>(true).Length > 0) {
				AvatarModuleNotification.Hide(Uid);
				return;
			}

			AvatarModuleNotification.Required<RigManagerModule>(
				avatar,
				Uid,
				Module,
				"avatars.modules.rigging.missing",
				() => Update(avatar)
			);
		}
	}
}
