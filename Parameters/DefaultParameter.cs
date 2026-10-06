using System;
using Nox.Avatars.Parameters;
using Nox.CCK.Network;

namespace Nox.CCK.Avatars.Parameters {
	/// <summary>
	/// Paramètre "de secours" utilisé quand l'Animator de l'avatar ne déclare pas un paramètre
	/// standard (VelocityX/Y/Z, Grounded, Pose...). La valeur est conservée en mémoire pour que
	/// <see cref="IParameterModule.GetParameter(string)"/> ne renvoie jamais <c>null</c> et que les
	/// autres modules puissent le lire/écrire.
	/// </summary>
	public class DefaultParameter : IParameter {
		private readonly string _name;
		private object          _value;

		public DefaultParameter(string name, ParameterType type, object defaultValue = null) {
			_name     = name;
			ValueType = type;
			_value    = Coerce(defaultValue);
		}

		public string Name
			=> _name;

		/// <summary>Clé de synchronisation : CRC32 du nom, identique à <see cref="Serializer.Hash(string)"/>.</summary>
		public int Key
			=> _name.Hash();

		public ParameterFlags Flags
			=> ParameterFlags.AllEditable;

		public ParameterType ValueType { get; }

		public object Value {
			get => _value;
			set => _value = Coerce(value);
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
				_                        => value
			};
	}
}
