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

		/// <summary>
		/// Owner to viewers: this position <b>is</b> the tracking target the viewers replay on their rig, so it
		/// must be emitted. <see cref="ParameterFlags.Persistent"/> alone is not a sync flag: the value then never
		/// leaves the owner's client, the remote rig keeps its own (stale) target while
		/// <c>tracking/&lt;bone&gt;/active</c> reports the bone as tracked, and the solver pulls the avatar into a
		/// wrong pose.
		/// </summary>
		public ParameterFlags Flags
			=> ParameterFlags.Persistent
				| ParameterFlags.OwnerEditable
				| ParameterFlags.OwnerSyncsToViewers;

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