using Nox.Avatars.Parameters;
using UnityEngine;
using Nox.CCK.Network;
using Nox.CCK.Avatars.Parameters;

namespace Nox.CCK.Avatars.Rigging.Parameters {
	public class TransformRotationParameter : IParameter {
		/// <summary>Published by its module: this parameter does not announce a write.</summary>
		public UnityEngine.Events.UnityEvent<IParameter> OnValueChanged => null;
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

		/// <summary>Category shown by the parameter editors.</summary>
		public string Category
			=> ParameterCategory.Rigging;

		public int Key
			=> Name.Hash();

		public ParameterType ValueType
			=> ParameterType.Quaternion;

		/// <summary>
		/// Live IK state, not a preference: nothing to edit, save or synchronize.
		/// </summary>
		public ParameterFlags Flags
			=> ParameterFlags.None;

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
