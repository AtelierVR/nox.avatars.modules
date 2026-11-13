using Nox.Avatars.Parameters;
using Nox.CCK.Network;
using Nox.CCK.Avatars.Parameters;

namespace Nox.CCK.Avatars.Scale {
	/// <summary>
	/// Height of the head bone above the feet at a scale of 1, in metres
	/// (see <see cref="ScaleAvatarModule.InitialHeadHeight"/>).
	/// <para>
	/// Read-only constant of the model: measured once at load in its rest pose, identical on every client. The
	/// value at the avatar's current scale is <c>HeadHeight</c>, the live one is <c>RealHeadHeight</c>.
	/// </para>
	/// </summary>
	public class InitialHeadHeightParameter : IParameter {
		/// <summary>Published by its module: this parameter does not announce a write.</summary>
		public UnityEngine.Events.UnityEvent<IParameter> OnValueChanged => null;
		private readonly ScaleAvatarModule _module;

		public InitialHeadHeightParameter(ScaleAvatarModule module)
			=> _module = module;

		public string Name
			=> "Height/Head/Initial";

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
			get => _module.InitialHeadHeight;
			set { }
		}
	}
}
