using Nox.Avatars.Parameters;
using UnityEngine;
using Nox.CCK.Network;
using Nox.CCK.Avatars.Parameters;

namespace Nox.CCK.Avatars.Rigging.Parameters {
	public class RiggingPositionParameter : IParameter {
		/// <summary>Published by its module: this parameter does not announce a write.</summary>
		public UnityEngine.Events.UnityEvent<IParameter> OnValueChanged => null;
		private readonly HumanBodyBones      _bone;
		private readonly BaseRigging _module;
		private readonly string              _parameterName;

		public RiggingPositionParameter(HumanBodyBones bone, BaseRigging module) {
			_bone          = bone;
			_module        = module;
			_parameterName = $"Tracking/{bone}/Position";
		}

		public string Name
			=> _parameterName;

		public bool IsValid()
			=> _module != null && _module.GetPart(_bone) != null;

		/// <summary>Category shown by the parameter editors.</summary>
		public string Category
			=> ParameterCategory.Rigging;

		public int Key
			=> Name.Hash();

		public ParameterType ValueType
			=> ParameterType.Vector3;

		/// <summary>
		/// Local only: the tracked targets travel as <b>parts</b> (the controller exposes them and the relay
		/// sends the parts with its threshold/interpolation), so syncing this parameter too would add a second
		/// writer on the viewer, driven by the avatar parameter cadence instead.
		/// </summary>
		/// <summary>
		/// Live IK state, not a preference: nothing to edit, save or synchronize.
		/// </summary>
		public ParameterFlags Flags
			=> ParameterFlags.None;

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
