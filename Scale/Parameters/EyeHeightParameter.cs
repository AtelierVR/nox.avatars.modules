using Nox.Avatars.Parameters;
using Nox.CCK.Network;

namespace Nox.CCK.Avatars.Scale {
	public class EyeHeightParameter : IParameter {
		private readonly ScaleAvatarModule _module;

		public EyeHeightParameter(ScaleAvatarModule module) 
			=> _module = module;

		public string GetName()
			=> "EyeHeight";

		public int GetKey()
			=> GetName().Hash();

		public ParameterType GetValueType()
			=> ParameterType.Float;

		public ParameterFlags GetFlags()
			=> ParameterFlags.LocalEditable
				| ParameterFlags.RemoteEditableByLocal;

		public object Get()
			=> _module.Height;

		public void Set(object value)
			=> _module.Height = value.ToFloat();
	}
}