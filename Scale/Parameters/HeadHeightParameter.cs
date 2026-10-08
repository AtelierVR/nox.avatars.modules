using Nox.Avatars.Parameters;
using Nox.CCK.Network;

namespace Nox.CCK.Avatars.Scale {
	/// <summary>
	/// Distance from the anchor (feet) to the head bone, in metres, at the current scale
	/// (see <see cref="ScaleAvatarModule.HeadHeight"/>).
	/// <para>
	/// A proportion of the <b>model</b> measured in its rest pose: it is the same value on every client and it
	/// does not follow the animation or the trackers.
	/// </para>
	/// </summary>
	public class HeadHeightParameter : IParameter {
		private readonly ScaleAvatarModule _module;

		public HeadHeightParameter(ScaleAvatarModule module)
			=> _module = module;

		public string Name
			=> "HeadHeight";

		public int Key
			=> Name.Hash();

		public ParameterType ValueType
			=> ParameterType.Float;

		public ParameterFlags Flags
			=> ParameterFlags.None;

		public object Value {
			get => _module.HeadHeight;
			set { }
		}
	}
}
