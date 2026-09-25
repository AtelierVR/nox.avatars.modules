using Nox.Avatars.Parameters;
using UnityEngine;
using Nox.CCK.Network;

namespace Nox.CCK.Avatars.Rigging.Parameters {
	public class TransformRotationParameter : IParameter {
		private readonly Transform _transform;
		private readonly string    _name;
		private readonly bool      _isReadOnly;

		public TransformRotationParameter(string name, Transform transform, bool isReadOnly = false) {
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
			=> ParameterType.Quaternion;

		public ParameterFlags Flags
			=> ParameterFlags.Persistent;

		public object Value {
			get => _transform
				? _transform.rotation
				: Quaternion.identity;
			set {
				if (_isReadOnly || !_transform) return;
				_transform.rotation = value.ToQuaternion();
			}
		}
	}
}