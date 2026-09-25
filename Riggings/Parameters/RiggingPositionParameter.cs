using Nox.Avatars.Parameters;
using Nox.CCK.Utils;
using UnityEngine;
using Nox.CCK.Network;

namespace Nox.CCK.Avatars.Rigging.Parameters {
	public class RiggingPositionParameter : IParameter {
		private readonly HumanBodyBones      _bone;
		private readonly BaseRigging _module;
		private readonly string              _parameterName;

		public RiggingPositionParameter(HumanBodyBones bone, BaseRigging module) {
			_bone          = bone;
			_module        = module;
			_parameterName = $"tracking/{bone.ToString().ToSnakeCase()}/position";
		}

		public string Name
			=> _parameterName;

		public bool IsValid()
			=> _module != null && _module.GetPart(_bone) != null;

		public int Key
			=> Name.Hash();

		public ParameterType ValueType
			=> ParameterType.Vector3;

		public ParameterFlags Flags
			=> ParameterFlags.Persistent;

		public object Value {
			get => _module != null 
				? _module.GetPart(_bone)?.position 
					?? Vector3.zero 
				: Vector3.zero;
			set {
				if (_module == null) 
					return;
				var part = _module.GetPart(_bone);
				if (part != null)
					part.position = value.ToVector3();
			}
		}
	}
}