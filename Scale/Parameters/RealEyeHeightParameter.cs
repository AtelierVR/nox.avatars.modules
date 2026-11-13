using Nox.Avatars.Parameters;
using Nox.CCK.Network;
using Nox.CCK.Avatars.Parameters;

namespace Nox.CCK.Avatars.Scale {
	/// <summary>
	/// Live height of the eyes above the feet, in metres (see <see cref="ScaleAvatarModule.RealEyeHeight"/>).
	/// <para>
	/// The <b>real</b> measurement of the current pose: it follows the animation, the rig and the trackers. The
	/// model's eye height is <c>EyeHeight</c> / <c>InitialEyeHeight</c>.
	/// </para>
	/// </summary>
	public class RealEyeHeightParameter : IParameter {
		/// <summary>Published by its module: this parameter does not announce a write.</summary>
		public UnityEngine.Events.UnityEvent<IParameter> OnValueChanged => null;
		private readonly ScaleAvatarModule _module;

		public RealEyeHeightParameter(ScaleAvatarModule module)
			=> _module = module;

		public string Name
			=> "Height/Eye/Real";

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
			get => _module.RealEyeHeight;
			set { }
		}
	}
}
