using Nox.Avatars.Parameters;
using Nox.CCK.Network;

namespace Nox.CCK.Avatars.Scale {
	public class ScaleParameter : IParameter {
		private readonly ScaleAvatarModule _module;

		public ScaleParameter(ScaleAvatarModule module)
			=> _module = module;

		public string Name
			=> "Scale";

		public int Key
			=> Name.Hash();

		public ParameterType ValueType
			=> ParameterType.Float;

		public ParameterFlags Flags
			=> ParameterFlags.OwnerEditable; // local only: Height carries the synced value

		public object Value {
			get => _module.Scale;
			set => _module.Scale = value.ToFloat();
		}
	}
}