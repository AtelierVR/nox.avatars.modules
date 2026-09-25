using Nox.Avatars.Parameters;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Linq;
using Nox.CCK.Network;

namespace Nox.CCK.Avatars.Rigging.Parameters {
	public class RigBuilderLayerWeightParameter : IParameter {
		private readonly RigBuilder _rigBuilder;
		private readonly string     _layerName;
		private readonly string     _parameterName;

		public RigBuilderLayerWeightParameter(string parameterName, string layerName, RigBuilder rigBuilder) {
			_parameterName = parameterName;
			_layerName     = layerName;
			_rigBuilder    = rigBuilder;
		}

		public string Name
			=> _parameterName;

		public bool IsValid()
			=> _rigBuilder && _rigBuilder.enabled && _rigBuilder.layers.Any(l => l.rig && l.rig.name == _layerName);

		public int Key
			=> Name.Hash();

		public ParameterType ValueType
			=> ParameterType.Float;

		public ParameterFlags Flags
			=> ParameterFlags.OwnerEditable
				| ParameterFlags.OwnerSyncsToViewers;

		public object Value {
			get {
				if (!_rigBuilder) 
					return 0f;
				var layer = _rigBuilder.layers
					.FirstOrDefault(l => l.rig && l.rig.name == _layerName);
				return layer?.rig?.weight ?? 0f;
			}
			set {
				if (!_rigBuilder || !_rigBuilder.enabled) 
					return;

				foreach (var layer in _rigBuilder.layers.Where(layer => layer.rig && layer.rig.name == _layerName)) {
					layer.rig.weight = Mathf.Clamp01(value.ToFloat());
					break;
				}

				// Only rebuild if safe to do so  
				if (Application.isPlaying && _rigBuilder.isActiveAndEnabled)
					try {
						_rigBuilder.Build();
					} catch (System.InvalidOperationException ex) when (ex.Message.Contains("TransformStreamHandle")) {
						// Silently handle timing issues - build will happen later
						Debug.LogWarning($"RigBuilder build skipped due to timing: {ex.Message}");
					}
			}
		}
	}
}