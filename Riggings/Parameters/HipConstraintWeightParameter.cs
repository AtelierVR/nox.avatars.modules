using Nox.Avatars.Parameters;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Linq;
using Nox.CCK.Network;

namespace Nox.CCK.Avatars.Rigging.Parameters {
	public class HipConstraintWeightParameter : IParameter {
		private readonly RigBuilder     _rigBuilder;
		private readonly string         _parameterName;
		private readonly ConstraintType _constraintType;

		public enum ConstraintType {
			Position,
			Rotation
		}

		public HipConstraintWeightParameter(string parameterName, ConstraintType constraintType, RigBuilder rigBuilder) {
			_parameterName  = parameterName;
			_constraintType = constraintType;
			_rigBuilder     = rigBuilder;
		}

		public string Name
			=> _parameterName;

		public int Key
			=> Name.Hash();

		public ParameterType ValueType
			=> ParameterType.Float;

		public ParameterFlags Flags
			=> ParameterFlags.OwnerEditable
				| ParameterFlags.OwnerSyncsToViewers;
		
		// ReSharper disable Unity.PerformanceAnalysis
		public object Value {
			get {
				if (!_rigBuilder) 
					return 0f;

				foreach (var layer in _rigBuilder.layers.Where(layer => layer.rig && layer.rig.name.Contains("Hip"))) 
					if (_constraintType == ConstraintType.Position) {
						var pc = layer.rig.GetComponentInChildren<MultiPositionConstraint>();
						if (pc) return pc.weight;
					} else {
						var rc = layer.rig.GetComponentInChildren<MultiRotationConstraint>();
						if (rc) return rc.weight;
					}

				return 0f;
			}
			set {
				if (!_rigBuilder) 
					return;

				var weight = value.ToFloat();

				foreach (var layer in _rigBuilder.layers.Where(layer => layer.rig && layer.rig.name.Contains("Hip")))
					if (_constraintType == ConstraintType.Position) {
						var pc = layer.rig.GetComponentInChildren<MultiPositionConstraint>();
						if (pc) pc.weight = Mathf.Clamp01(weight);
					} else {
						var rc = layer.rig.GetComponentInChildren<MultiRotationConstraint>();
						if (rc) rc.weight = Mathf.Clamp01(weight);
					}
			}
		}
	}
}