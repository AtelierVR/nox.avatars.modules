using Nox.Avatars.Parameters;
using UnityEngine.Animations.Rigging;
using System.Linq;
using Nox.CCK.Network;
using UnityEngine;

namespace Nox.CCK.Avatars.Rigging.Parameters {
	public class RigBuilderLayerActiveParameter : IParameter {
		private readonly RigBuilder _rigBuilder;
		private readonly string     _layerName;
		private readonly string     _parameterName;

		public RigBuilderLayerActiveParameter(string parameterName, string layerName, RigBuilder rigBuilder) {
			_parameterName = parameterName;
			_layerName     = layerName;
			_rigBuilder    = rigBuilder;
		}

		public string Name
			=> _parameterName;

		public int Key
			=> Name.Hash();

		public ParameterType ValueType
			=> ParameterType.Bool;

		public ParameterFlags Flags
			=> ParameterFlags.OwnerEditable
				| ParameterFlags.OwnerSyncsToViewers;

		public object Value {
			get {
				if (!_rigBuilder || !_rigBuilder.enabled) 
					return false;
				var layer = _rigBuilder.layers
					.FirstOrDefault(l => l.rig && l.rig.name == _layerName);
				return layer is { active: true };
			}
			set {
			if (!_rigBuilder || !_rigBuilder.enabled) 
				return;
			
			foreach (var layer in _rigBuilder.layers.Where(layer => layer.rig && layer.rig.name == _layerName)) {
				layer.active = value.ToBool();
				break;
			}
			
			if (Application.isPlaying && _rigBuilder.isActiveAndEnabled)
				try {
					_rigBuilder.Build();
				} catch (System.InvalidOperationException ex) when (ex.Message.Contains("TransformStreamHandle")) {
					Debug.LogWarning($"RigBuilder build skipped due to timing: {ex.Message}");
				}
			}
		}
	}
}