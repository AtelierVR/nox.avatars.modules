using Nox.CCK.Avatars;
using Nox.CCK.Avatars.Camera;
using UnityEditor;

namespace Nox.CCK.Avatars.Modules.Editor {
	/// <summary>
	/// State of the <see cref="CameraChopModule"/> of the selected avatar: its <c>Check</c> accepts none or
	/// one — the module is opt-in, it hides the head of the local player in first person — and fails with
	/// several, which blocks the build.
	/// </summary>
	public static class CameraChopModuleNotification {
		private const string Uid    = "avatars.modules.camera_chop.module";
		private const string Module = "Camera chop";

		[InitializeOnLoadMethod]
		private static void Initialize()
			=> AvatarModuleNotification.Watch(Update);

		private static void Update(AvatarDescriptor avatar)
			=> AvatarModuleNotification.AtMostOne<CameraChopModule>(avatar, Uid, Module);
	}
}
