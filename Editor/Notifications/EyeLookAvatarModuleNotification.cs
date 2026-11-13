using Nox.CCK.Avatars;
using Nox.CCK.Avatars.EyeLooks;
using UnityEditor;

namespace Nox.CCK.Avatars.Modules.Editor {
	/// <summary>
	/// State of the <see cref="EyeLookAvatarModule"/> of the selected avatar: its <c>Check</c> accepts none
	/// or one — an eye look is opt-in — and fails with several, which blocks the build.
	/// </summary>
	public static class EyeLookAvatarModuleNotification {
		private const string Uid    = "avatars.modules.eye_look.module";
		private const string Module = "Eye look";

		[InitializeOnLoadMethod]
		private static void Initialize()
			=> AvatarModuleNotification.Watch(Update);

		private static void Update(AvatarDescriptor avatar)
			=> AvatarModuleNotification.AtMostOne<EyeLookAvatarModule>(avatar, Uid, Module);
	}
}
