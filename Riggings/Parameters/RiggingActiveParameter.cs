using Nox.Avatars.Parameters;
using UnityEngine;
using Nox.CCK.Network;
using Nox.CCK.Avatars.Parameters;

namespace Nox.CCK.Avatars.Rigging.Parameters {
	public class RiggingActiveParameter : IParameter {
		/// <summary>Published by its module: this parameter does not announce a write.</summary>
		public UnityEngine.Events.UnityEvent<IParameter> OnValueChanged => null;
		private readonly HumanBodyBones _bone;
		private readonly BaseRigging _module;
		private readonly string _name;

		public RiggingActiveParameter(HumanBodyBones bone, BaseRigging module) {
			_bone   = bone;
			_module = module;
			_name   = $"Tracking/{bone}/Active";
		}

		public string Name
			=> _name;

		public bool IsValid()
			=> _module != null && _module.GetPart(_bone) != null;

		/// <summary>Category shown by the parameter editors.</summary>
		public string Category
			=> ParameterCategory.Rigging;

		public int Key
			=> Name.Hash();

		public ParameterType ValueType
			=> ParameterType.Bool;

		public ParameterFlags Flags
			=> ParameterFlags.OwnerEditable
				| ParameterFlags.OwnerSyncsToViewers;

		public object Value {
			get => _module != null && _module.IsActive(_bone);
			set {
				if (_module == null)
					return;
				_module.SetActive(_bone, value.ToBool());
			}
		}
	}
}
