using Nox.CCK.Avatars;
using Nox.CCK.Avatars.Voice;
using UnityEditor;

namespace Nox.CCK.Avatars.Modules.Editor {
	/// <summary>
	/// State of the <see cref="VoiceAvatarModule"/> of the selected avatar: its <c>Check</c> adds it when
	/// there is none and fails with several, which blocks the build.
	/// </summary>
	public static class VoiceAvatarModuleNotification {
		private const string Uid    = "avatars.modules.voice.module";
		private const string Module = "Voice";

		[InitializeOnLoadMethod]
		private static void Initialize()
			=> AvatarModuleNotification.Watch(Update);

		private static void Update(AvatarDescriptor avatar)
			=> AvatarModuleNotification.Required<VoiceAvatarModule>(
				avatar,
				Uid,
				Module,
				"avatars.modules.voice.missing",
				() => Update(avatar)
			);
	}
}
