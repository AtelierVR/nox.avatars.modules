using Nox.Avatars.Parameters;
using Nox.CCK.Network;

namespace Nox.CCK.Avatars.Scale {
	public class ScaleEditedParameter : IParameter {
		private readonly ScaleAvatarModule _module;

		public ScaleEditedParameter(ScaleAvatarModule module)
			=> _module = module;

		public string GetName()
			=> "ScaleEdited";

		public int GetKey()
			=> GetName().Hash();

		public ParameterType GetValueType()
			=> ParameterType.Bool;

		public ParameterFlags GetFlags()
			=> ParameterFlags.LocalEditable
				| ParameterFlags.RemoteEditableByLocal;

		public object Get()
			=> _module.ScaleModified;

		public void Set(object value)
			=> _module.ScaleModified = value.ToBool();
	}
}