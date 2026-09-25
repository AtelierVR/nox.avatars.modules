using Nox.Avatars.Parameters;
using Nox.CCK.Network;

namespace Nox.CCK.Avatars.Scale {
	/// <summary>
	/// Live eye height: the distance from the anchor (feet) to the camera point
	/// (see <see cref="ScaleAvatarModule.EyeHeight"/>).
	/// Derived from the rig, so it is read-only and identical on every client.
	/// </summary>
	public class EyeHeightParameter : IParameter {
		private readonly ScaleAvatarModule _module;

		public EyeHeightParameter(ScaleAvatarModule module) 
			=> _module = module;

		public string Name
			=> "EyeHeight";

		public int Key
			=> Name.Hash();

		public ParameterType ValueType
			=> ParameterType.Float;

		public ParameterFlags Flags
			=> ParameterFlags.None;

		public object Value {
			get => _module.EyeHeight;
			set { }
		}
	}
}