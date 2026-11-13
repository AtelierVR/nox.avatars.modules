using Nox.Avatars.Parameters;
using Nox.CCK.Network;
using Nox.CCK.Avatars.Parameters;

namespace Nox.CCK.Avatars.Scale {
	/// <summary>
	/// Height of the eyes above the feet at a scale of 1, in metres
	/// (see <see cref="ScaleAvatarModule.InitialEyeHeight"/>).
	/// <para>
	/// Read-only constant of the model: measured once at load in its rest pose, identical on every client. The
	/// value at the avatar's current scale is <c>EyeHeight</c>, the live one is <c>RealEyeHeight</c>.
	/// </para>
	/// </summary>
	public class InitialEyeHeightParameter : IParameter {
		/// <summary>Published by its module: this parameter does not announce a write.</summary>
		public UnityEngine.Events.UnityEvent<IParameter> OnValueChanged => null;
		private readonly ScaleAvatarModule _module;

		public InitialEyeHeightParameter(ScaleAvatarModule module)
			=> _module = module;

		public string Name
			=> "Height/Eye/Initial";

		/// <summary>Category shown by the parameter editors.</summary>
		public string Category
			=> ParameterCategory.Scale;

		public int Key
			=> Name.Hash();

		public ParameterType ValueType
			=> ParameterType.Float;

		public ParameterFlags Flags
			=> ParameterFlags.None;

		public object Value {
			get => _module.InitialEyeHeight;
			set { }
		}
	}
}
