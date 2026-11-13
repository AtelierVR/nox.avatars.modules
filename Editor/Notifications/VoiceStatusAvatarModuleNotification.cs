using Nox.Avatars.Modules.Runtime;
using Nox.CCK.Avatars;
using UnityEditor;

namespace Nox.CCK.Avatars.Modules.Editor {
	/// <summary>
	/// State of the <see cref="VoiceStatusAvatarModule"/> of the selected avatar: its <c>Check</c> adds it
	/// when there is none and fails with several, which blocks the build.
	/// </summary>
	public static class VoiceStatusAvatarModuleNotification {
		private const string Uid    = "avatars.modules.voice_status.module";
		private const string Module = "Voice status";

		[InitializeOnLoadMethod]
		private static void Initialize()
			=> AvatarModuleNotification.Watch(Update);

		private static void Update(AvatarDescriptor avatar)
			=> AvatarModuleNotification.Required<VoiceStatusAvatarModule>(
				avatar,
				Uid,
				Module,
				"avatars.modules.voice_status.missing",
				() => Update(avatar)
			);
	}
}
