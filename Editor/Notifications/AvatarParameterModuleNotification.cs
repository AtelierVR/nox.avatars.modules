using Nox.CCK.Avatars;
using Nox.CCK.Avatars.Parameters;
using UnityEditor;

namespace Nox.CCK.Avatars.Modules.Editor {
	/// <summary>
	/// State of the <see cref="AvatarParameterModule"/> of the selected avatar: its <c>Check</c> adds it
	/// when there is none and fails with several, which blocks the build.
	/// </summary>
	public static class AvatarParameterModuleNotification {
		private const string Uid    = "avatars.modules.parameters.module";
		private const string Module = "Parameters";

		[InitializeOnLoadMethod]
		private static void Initialize()
			=> AvatarModuleNotification.Watch(Update);

		private static void Update(AvatarDescriptor avatar)
			=> AvatarModuleNotification.Required<AvatarParameterModule>(
				avatar,
				Uid,
				Module,
				"avatars.modules.parameters.missing",
				() => Update(avatar)
			);
	}
}
