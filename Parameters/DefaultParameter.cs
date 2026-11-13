using System;
using Nox.Avatars.Parameters;
using Nox.CCK.Events;
using Nox.CCK.Network;
using UnityEngine.Events;

namespace Nox.CCK.Avatars.Parameters {
	/// <summary>
	/// Paramètre "de secours" utilisé quand l'Animator de l'avatar ne déclare pas un paramètre
	/// standard et qu'il ne peut pas être composé (Velocity, Grounded, Pose, Team/Color...). La valeur
	/// est conservée en mémoire pour que
	/// <see cref="IParameterModule.GetParameter(string)"/> ne renvoie jamais <c>null</c> et que les
	/// autres modules puissent le lire/écrire.
	/// </summary>
	public class DefaultParameter : IParameter {
		private readonly string _name;
		private object          _value;

		/// <summary>Raised after <see cref="Value"/> was written (see <see cref="IParameter.OnValueChanged"/>).</summary>
		public UnityEvent<IParameter> OnValueChanged { get; } = new NoxEvent<IParameter>();

		public DefaultParameter(
			string name,
			ParameterType type,
			object defaultValue = null,
			ParameterFlags flags = ParameterFlags.AllEditable,
			string category = null
		) {
			_name     = name;
			ValueType = type;
			Flags     = flags;
			Category  = category;
			_value    = Coerce(defaultValue);
		}

		public string Name
			=> _name;

		/// <summary>Clé de synchronisation : CRC32 du nom, identique à <see cref="Serializer.Hash(string)"/>.</summary>
		public int Key
			=> _name.Hash();

		/// <summary>Category of the parameter, or <c>null</c> when it declares none (see <see cref="IParameter.Category"/>).</summary>
		public string Category { get; }

		public ParameterFlags Flags { get; }

		public ParameterType ValueType { get; }

		public object Value {
			get => _value;
			set {
				var next = Coerce(value);
				if (Equals(_value, next))
					return;

				_value = next;
				OnValueChanged.Invoke(this);
			}
		}

		/// <summary>
		/// Normalise la valeur vers le type fort du paramètre, avec les mêmes conversions
		/// big-endian que <c>BaseParameter.Value</c> (via <see cref="Converter"/>).
		/// </summary>
		private object Coerce(object value)
			=> ValueType switch {
				ParameterType.Bool       => value.ToBool(),
				ParameterType.Byte       => value.ToByte(),
				ParameterType.Short      => value.ToShort(),
				ParameterType.UShort     => value.ToUShort(),
				ParameterType.Int        => value.ToInt(),
				ParameterType.UInt       => value.ToUInt(),
				ParameterType.Long       => value.ToLong(),
				ParameterType.ULong      => value.ToULong(),
				ParameterType.Float      => value.ToFloat(),
				ParameterType.Double     => value.ToDouble(),
				ParameterType.String     => Converter.ToString(value),
				ParameterType.ByteArray  => value as byte[] ?? Array.Empty<byte>(),
				ParameterType.Vector3    => value.ToVector3(),
				ParameterType.Quaternion => value.ToQuaternion(),
				ParameterType.Color      => value.ToColor(),
				_                        => value
			};
	}
}
