using Nox.Avatars.Parameters;
using Nox.CCK.Network;

namespace Nox.CCK.Avatars.Scale {
	public class ScaleParameter : IParameter {
		private readonly ScaleAvatarModule _module;

		public ScaleParameter(ScaleAvatarModule module)
			=> _module = module;

		public string GetName()
			=> "Scale";

		public int GetKey()
			=> GetName().Hash();

		public ParameterType GetValueType()
			=> ParameterType.Float;

		public ParameterFlags GetFlags()
			=> ParameterFlags.LocalEditable
				| ParameterFlags.RemoteEditableByLocal;

		public object Get()
			=> _module.Scale;

		public void Set(object value)
			=> _module.Scale = value.ToFloat();
	}
}