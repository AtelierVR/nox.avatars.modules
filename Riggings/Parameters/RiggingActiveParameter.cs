using Nox.Avatars.Parameters;
using Nox.Avatars.Rigging;
using Nox.CCK.Utils;
using UnityEngine;
using Nox.CCK.Network;

namespace Nox.CCK.Avatars.Rigging.Parameters {
	public class RiggingActiveParameter : IParameter {
		private readonly HumanBodyBones _bone;
		private readonly BaseRigging _module;
		private readonly string _name;

		public RiggingActiveParameter(HumanBodyBones bone, BaseRigging module) {
			_bone   = bone;
			_module = module;
			_name   = $"tracking/{bone.ToString().ToSnakeCase()}/active";
		}

		public string GetName()
			=> _name;

		public bool IsValid()
			=> _module != null && _module.GetPart(_bone) != null;

		public int GetKey()
			=> _name.GetHashCode();

		public ParameterType GetValueType()
			=> ParameterType.Bool;

		public ParameterFlags GetFlags()
			=> ParameterFlags.OwnerEditable
				| ParameterFlags.OwnerSyncsToViewers;

		public object Get()
			=> _module != null && _module.IsActive(_bone);

		public void Set(object value) {
			if (_module == null)
				return;
			_module.SetActive(_bone, value.ToBool());
		}
	}
}