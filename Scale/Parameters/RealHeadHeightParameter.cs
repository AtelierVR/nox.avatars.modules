using Nox.Avatars.Parameters;
using Nox.CCK.Network;
using Nox.CCK.Avatars.Parameters;

namespace Nox.CCK.Avatars.Scale {
	/// <summary>
	/// Live height of the head bone above the feet, in metres (see <see cref="ScaleAvatarModule.RealHeadHeight"/>).
	/// <para>
	/// The <b>real</b> measurement of the current pose: it follows the animation, the rig and the trackers, and
	/// drops when the avatar crouches. The model's head height is <c>HeadHeight</c> / <c>InitialHeadHeight</c>.
	/// </para>
	/// </summary>
	public class RealHeadHeightParameter : IParameter {
		/// <summary>Published by its module: this parameter does not announce a write.</summary>
		public UnityEngine.Events.UnityEvent<IParameter> OnValueChanged => null;
		private readonly ScaleAvatarModule _module;

		public RealHeadHeightParameter(ScaleAvatarModule module)
			=> _module = module;

		public string Name
			=> "Height/Head/Real";

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
			get => _module.RealHeadHeight;
			set { }
		}
	}
}
