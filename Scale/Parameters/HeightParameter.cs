using Nox.Avatars.Parameters;
using Nox.CCK.Network;

namespace Nox.CCK.Avatars.Scale {
	/// <summary>
	/// Full avatar height (feet to the top of the skull), computed live from the head bone and camera
	/// points (see <see cref="ScaleAvatarModule.MeasuredHeight"/>).
	/// Setting it rescales the avatar so the measured height matches the requested value.
	/// </summary>
	public class HeightParameter : IParameter {
		private readonly ScaleAvatarModule _module;

		public HeightParameter(ScaleAvatarModule module)
			=> _module = module;

		public string Name
			=> "Height";

		public int Key
			=> Name.Hash();

		public ParameterType ValueType
			=> ParameterType.Float;

		public ParameterFlags Flags
			=> ParameterFlags.OwnerEditable
				| ParameterFlags.OwnerSyncsToViewers;

		public object Value {
			get => _module.MeasuredHeight;
			set => _module.Height = value.ToFloat();
		}
	}
}
