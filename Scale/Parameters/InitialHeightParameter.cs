using Nox.Avatars.Parameters;
using Nox.CCK.Network;

namespace Nox.CCK.Avatars.Scale {
	/// <summary>
	/// Height of the model without its scale, in metres (see <see cref="ScaleAvatarModule.InitialHeight"/>):
	/// what the avatar would measure with a scale of 1.
	/// <para>
	/// Measured once at load, in the model's rest pose, so it is a constant of the avatar: it is read-only and
	/// identical on every client. The scaled height is the <c>Height</c> parameter, the scale itself is
	/// <c>Scale</c>.
	/// </para>
	/// </summary>
	public class InitialHeightParameter : IParameter {
		private readonly ScaleAvatarModule _module;

		public InitialHeightParameter(ScaleAvatarModule module)
			=> _module = module;

		public string Name
			=> "InitialHeight";

		public int Key
			=> Name.Hash();

		public ParameterType ValueType
			=> ParameterType.Float;

		public ParameterFlags Flags
			=> ParameterFlags.None;

		public object Value {
			get => _module.InitialHeight;
			set { }
		}
	}
}
