using Nox.Assets.Editor;
using Nox.CCK.Avatars;
using Nox.CCK.Avatars.Camera;
using Nox.CCK.Avatars.Scale;
using UnityEditor;

namespace Nox.CCK.Avatars.Modules.Editor {
	/// <summary>
	/// State of the <see cref="ScaleAvatarModule"/> of the selected avatar: its <c>Check</c> adds it when
	/// there is none and fails with several, which blocks the build. It also needs a camera module — the
	/// scale is measured against the eyes of the player — so the missing dependency is an error of its own.
	/// </summary>
	public static class ScaleAvatarModuleNotification {
		private const string ModuleUid   = "avatars.modules.scale.module";
		private const string NoCameraUid = "avatars.modules.scale.no_camera";
		private const string Module      = "Scale";

		[InitializeOnLoadMethod]
		private static void Initialize()
			=> AvatarModuleNotification.Watch(Update);

		private static void Update(AvatarDescriptor avatar) {
			var modules = AvatarModuleNotification.Required<ScaleAvatarModule>(
				avatar,
				ModuleUid,
				Module,
				"avatars.modules.scale.missing",
				() => Update(avatar)
			);

			var cameras = AvatarModuleNotification.Modules<CameraAvatarModule>(avatar);

			// Only report the dependency once the scale module is there: the entry above already says the
			// avatar is missing it, and adding it brings the camera module along through its own check.
			if (modules.Length == 0 || cameras.Length > 0) {
				AvatarModuleNotification.Hide(NoCameraUid);
				return;
			}

			AvatarModuleNotification.Show(
				NoCameraUid,
				AssetNotificationType.Error,
				"avatars.modules.scale.no_camera",
				null,
				AvatarModuleNotification.Select(modules[0])
			);
		}
	}
}
