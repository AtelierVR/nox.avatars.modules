using Nox.Avatars.Parameters;
using Nox.CCK.Network;

namespace Nox.CCK.Avatars.Scale {
	/// <summary>
	/// Scale the avatar was loaded with (see <see cref="ScaleAvatarModule.InitialScale"/>): the reference
	/// <c>ScaleModified</c> compares the current <c>Scale</c> against.
	/// <para>
	/// It is <b>not</b> a size — the current size is <c>Height</c> and the scale the avatar was authored with is
	/// <c>Scale</c>. Read-only and identical on every client.
	/// </para>
	/// </summary>
	public class InitialScaleParameter : IParameter {
		private readonly ScaleAvatarModule _module;

		public InitialScaleParameter(ScaleAvatarModule module)
			=> _module = module;

		public string Name
			=> "InitialScale";

		public int Key
			=> Name.Hash();

		public ParameterType ValueType
			=> ParameterType.Float;

		public ParameterFlags Flags
			=> ParameterFlags.None;

		public object Value {
			get => _module.InitialScale;
			set { }
		}
	}
}
