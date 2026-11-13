using Nox.Assets.Editor;
using Nox.CCK.Avatars;
using Nox.CCK.Avatars.Camera;
using UnityEditor;
using UnityEngine;

namespace Nox.CCK.Avatars.Modules.Editor {
	/// <summary>
	/// State of the <see cref="CameraAvatarModule"/> of the selected avatar: its <c>Check</c> adds it when
	/// there is none and fails with several, which blocks the build.
	/// <para>
	/// The offset is reported too: left at zero, the module computes the midpoint of the eyes when the
	/// avatar loads and the runtime warns about it, so the inspector can say it earlier.
	/// </para>
	/// </summary>
	public static class CameraAvatarModuleNotification {
		private const string ModuleUid = "avatars.modules.camera.module";
		private const string OffsetUid = "avatars.modules.camera.offset_unset";
		private const string Module    = "Camera";

		[InitializeOnLoadMethod]
		private static void Initialize()
			=> AvatarModuleNotification.Watch(Update, watchProperties: true);

		private static void Update(AvatarDescriptor avatar) {
			var modules = AvatarModuleNotification.Required<CameraAvatarModule>(
				avatar,
				ModuleUid,
				Module,
				"avatars.modules.camera.missing",
				() => Update(avatar)
			);

			if (modules.Length != 1 || modules[0].cameraOffset != Vector3.zero) {
				AvatarModuleNotification.Hide(OffsetUid);
				return;
			}

			AvatarModuleNotification.Show(
				OffsetUid,
				AssetNotificationType.Warning,
				"avatars.modules.camera.offset_unset",
				null,
				AvatarModuleNotification.Select(modules[0])
			);
		}
	}
}
