using Nox.Avatars.Parameters;
using Nox.CCK.Network;
using Nox.CCK.Avatars.Parameters;

namespace Nox.CCK.Avatars.Scale {
	public class ScaleEditedParameter : IParameter {
		/// <summary>Published by its module: this parameter does not announce a write.</summary>
		public UnityEngine.Events.UnityEvent<IParameter> OnValueChanged => null;
		private readonly ScaleAvatarModule _module;

		public ScaleEditedParameter(ScaleAvatarModule module)
			=> _module = module;

		public string Name
			=> "Scale/Edited";

		/// <summary>Category shown by the parameter editors.</summary>
		public string Category
			=> ParameterCategory.Scale;

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