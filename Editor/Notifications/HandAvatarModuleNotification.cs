using Nox.Assets.Editor;
using Nox.CCK.Avatars;
using Nox.CCK.Avatars.Hand;
using UnityEditor;

namespace Nox.CCK.Avatars.Modules.Editor {
	/// <summary>
	/// State of the <see cref="HandAvatarModule"/> of the selected avatar: its <c>Check</c> adds it when
	/// there is none and fails with several, which blocks the build.
	/// </summary>
	public static class HandAvatarModuleNotification {
		private const string Uid    = "avatars.modules.hand.module";
		private const string Module = "Hands";

		[InitializeOnLoadMethod]
		private static void Initialize()
			=> AvatarModuleNotification.Watch(Update);

		private static void Update(AvatarDescriptor avatar)
			=> AvatarModuleNotification.Required<HandAvatarModule>(
				avatar,
				Uid,
				Module,
				"avatars.modules.hand.missing",
				() => Update(avatar)
			);
	}
}
