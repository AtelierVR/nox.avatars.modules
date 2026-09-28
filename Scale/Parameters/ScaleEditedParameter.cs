using Nox.Avatars.Parameters;
using Nox.CCK.Network;

namespace Nox.CCK.Avatars.Scale {
	public class ScaleEditedParameter : IParameter {
		private readonly ScaleAvatarModule _module;

		public ScaleEditedParameter(ScaleAvatarModule module)
			=> _module = module;

		public string Name
			=> "ScaleEdited";

		public int Key
			=> Name.Hash();

		public ParameterType ValueType
			=> ParameterType.Bool;

		public ParameterFlags Flags
			=> ParameterFlags.OwnerEditable; // local only: Scale carries the synced value

		public object Value {
			get => _module.ScaleModified;
			set => _module.ScaleModified = value.ToBool();
		}
	}
}