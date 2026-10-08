using Nox.Avatars.Parameters;
using Nox.CCK.Network;

namespace Nox.CCK.Avatars.Scale {
	/// <summary>
	/// Live height of the avatar, in metres: distance from the anchor (feet) to the head bone, plus twice the
	/// head bone to camera offset (mirrored past the eyes to reach the top of the skull) — see
	/// <see cref="ScaleAvatarModule.RealHeight"/>.
	/// <para>
	/// This is the <b>real</b> measurement of the current pose: it follows the animation, the rig and the
	/// trackers. Read-only, and identical on every client because the same rig is replayed everywhere. The
	/// model's height is <c>Height</c> (current scale) / <c>InitialHeight</c> (scale of 1).
	/// </para>
	/// </summary>
	public class RealHeightParameter : IParameter {
		private readonly ScaleAvatarModule _module;

		public RealHeightParameter(ScaleAvatarModule module)
			=> _module = module;

		public string Name
			=> "RealHeight";

		public int Key
			=> Name.Hash();

		public ParameterType ValueType
			=> ParameterType.Float;

		public ParameterFlags Flags
			=> ParameterFlags.None;

		public object Value {
			get => _module.RealHeight;
			set { }
		}
	}
}
