using System;
using Nox.Avatars.Parameters;
using Nox.CCK.Events;
using Nox.CCK.Network;
using UnityEngine;
using UnityEngine.Events;

namespace Nox.CCK.Avatars.Parameters {
	public abstract class BaseParameter : IParameter {
		internal AnimatorControllerParameter Parameter;
		internal ParameterEntry              Entry;

		/// <summary>Raised after <see cref="Value"/> was written (see <see cref="IParameter.OnValueChanged"/>).</summary>
		public UnityEvent<IParameter> OnValueChanged { get; } = new NoxEvent<IParameter>();

		public string Name
			=> Parameter.name;

		/// <summary>
		/// Hash Unity attendu par l'Animator / l'AnimatorControllerPlayable pour accéder au
		/// paramètre (<see cref="AnimatorControllerParameter.nameHash"/>).
		/// À ne pas confondre avec <see cref="GetKey"/>, qui est la clé de synchronisation.
		/// </summary>
		internal int AnimatorHash
			=> Parameter.nameHash;

		public int Key
			=> Name.Hash();

		/// <summary>
		/// Category declared by <see cref="AvatarParameterModule.DefaultParameters"/> for this name (the game
		/// owns the parameters it declares), or <c>null</c> when neither declares one.
		/// </summary>
		public string Category
			=> AvatarParameterModule.CategoryOf(Name);

		public ParameterFlags Flags
			=> Entry?.flags ?? ParameterFlags.AllEditable;

		public ParameterType ValueType
			=> Parameter.type switch {
				AnimatorControllerParameterType.Float => ParameterType.Float,
				AnimatorControllerParameterType.Int   => ParameterType.Int,
				AnimatorControllerParameterType.Bool  => ParameterType.Bool,
				_                                     => ParameterType.Float
			};

		public object Value {
			get => Parameter.type switch {
				AnimatorControllerParameterType.Float   => GetFloat(),
				AnimatorControllerParameterType.Int     => GetInteger(),
				AnimatorControllerParameterType.Bool    => GetBool(),
				AnimatorControllerParameterType.Trigger => throw new InvalidOperationException("Cannot get value of a Trigger parameter."),
				_                                       => throw new ArgumentOutOfRangeException()
			};
			set {
				switch (ValueType) {
					case ParameterType.Float:
					case ParameterType.Double:
						SetFloat(value.ToFloat());
						break;

					case ParameterType.Byte:
					case ParameterType.Short:
					case ParameterType.UShort:
					case ParameterType.UInt:
					case ParameterType.Long:
					case ParameterType.ULong:
					case ParameterType.Int:
						SetInteger(value.ToInt());
						break;

					case ParameterType.Bool:
						SetBool(value.ToBool());
						break;

					case ParameterType.ByteArray:
					case ParameterType.String:
					case ParameterType.Vector3:
					case ParameterType.Quaternion:
					default:
						throw new ArgumentOutOfRangeException($"Unsupported parameter type: {Parameter.type}");
				}

				OnValueChanged.Invoke(this);
			}
		}

		protected abstract void SetFloat(float value);

		protected abstract void SetInteger(int value);

		protected abstract void SetBool(bool value);

		protected abstract float GetFloat();

		protected abstract int GetInteger();

		protected abstract bool GetBool();
	}
}
