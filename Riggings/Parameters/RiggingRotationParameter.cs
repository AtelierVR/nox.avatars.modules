using Nox.Avatars.Parameters;
using Nox.CCK.Utils;
using UnityEngine;
using Nox.CCK.Network;

namespace Nox.CCK.Avatars.Rigging.Parameters {
	public class RiggingRotationParameter : IParameter {
		private readonly HumanBodyBones      _bone;
		private readonly BaseRigging _module;
		private readonly string              _parameterName;

		public RiggingRotationParameter(HumanBodyBones bone, BaseRigging module) {
			_bone          = bone;
			_module        = module;
			_parameterName = $"tracking/{bone.ToString().ToSnakeCase()}/rotation";
		}

		public string Name
			=> _parameterName;

		public bool IsValid()
			=> _module != null && _module.GetPart(_bone) != null;

		public int Key
			=> Name.Hash();

		public ParameterType ValueType
			=> ParameterType.Quaternion;

		/// <summary>
		/// Local only: the tracked targets travel as <b>parts</b> (the controller exposes them and the relay
		/// sends the parts with its threshold/interpolation), so syncing this parameter too would add a second
		/// writer on the viewer, driven by the avatar parameter cadence instead.
		/// </summary>
		public ParameterFlags Flags
			=> ParameterFlags.Persistent;

		public object Value {
			get => _module != null 
				? _module.GetPart(_bone)?.rotation 
					?? Quaternion.identity 
				: Quaternion.identity;
			set {
				if (_module == null) 
					return;
				var part = _module.GetPart(_bone);
				if (part != null)
					part.rotation = value.ToQuaternion();
			}
		}
	}
}