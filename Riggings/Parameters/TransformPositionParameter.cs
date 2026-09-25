using Nox.Avatars.Parameters;
using UnityEngine;
using Nox.CCK.Network;

namespace Nox.CCK.Avatars.Rigging.Parameters {
	public class TransformPositionParameter : IParameter {
		private readonly Transform _transform;
		private readonly string    _name;
		private readonly bool      _isReadOnly;

		public TransformPositionParameter(string name, Transform transform, bool isReadOnly = false) {
			_name       = name;
			_transform  = transform;
			_isReadOnly = isReadOnly;
		}

		public string Name
			=> _name;

		public bool IsValid()
			=> _transform;

		public int Key
			=> Name.Hash();

		public ParameterType ValueType
			=> ParameterType.Vector3;

		public ParameterFlags Flags
			=> ParameterFlags.Persistent;

		public object Value {
			get => _transform
				? _transform.position
				: Vector3.zero;
			set {
				if (_isReadOnly || !_transform) return;
				_transform.position = value.ToVector3();
			}
		}
	}
}