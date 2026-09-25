using Nox.Avatars.Parameters;
using Nox.CCK.Network;

namespace Nox.CCK.Avatars.Scale {
	/// <summary>
	/// Live distance from the anchor (feet) to the head bone (see <see cref="ScaleAvatarModule.HeadHeight"/>).
	/// Derived from the rig, so it is read-only and identical on every client.
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
