using System;
using Nox.Avatars.Parameters;
using UnityEngine;

namespace Nox.CCK.Avatars.Parameters {
	/// <summary>
	/// A parameter value as it is stored in the avatar data (persistent values and saved profiles): the
	/// type of the parameter and its value encoded in base64, exactly like a <see cref="ParameterEntry"/>
	/// of the avatar parameters asset.
	/// <para>
	/// Storing the type next to the bytes keeps the value readable without the parameter it came from, so
	/// the format survives a change of the Animator controller behind a name.
	/// </para>
	/// </summary>
	[Serializable]
	public sealed class ParameterValue : IEquatable<ParameterValue> {
		/// <summary>Type of the value (see <see cref="ParameterType"/>).</summary>
		public ParameterType type;

		/// <summary>Value, base64 of its big-endian bytes (see <c>Nox.CCK.Converter</c>).</summary>
		public string value;

		public ParameterValue() { }

		public ParameterValue(ParameterType type, string value) {
			this.type  = type;
			this.value = value;
		}

		/// <summary>
		/// Encodes <paramref name="value"/> as <paramref name="type"/>, or <c>null</c> when the value cannot
		/// be read as that type.
		/// </summary>
		public static ParameterValue From(object value, ParameterType type) {
			try {
				return new ParameterValue(type, Convert.ToBase64String(Bytes(value, type)));
			} catch (Exception) {
				return null;
			}
		}

		/// <summary>
		/// Encodes <paramref name="value"/> with the type its runtime type maps to (see <see cref="TypeOf"/>),
		/// or <c>null</c> when it has no <see cref="ParameterType"/> counterpart.
		/// </summary>
		public static ParameterValue From(object value)
			=> TypeOf(value) is { } type ? From(value, type) : null;

		/// <summary><see cref="ParameterType"/> a value of the runtime type of <paramref name="value"/> encodes to.</summary>
		public static ParameterType? TypeOf(object value)
			=> value switch {
				bool       => ParameterType.Bool,
				byte       => ParameterType.Byte,
				short      => ParameterType.Short,
				ushort     => ParameterType.UShort,
				int        => ParameterType.Int,
				uint       => ParameterType.UInt,
				long       => ParameterType.Long,
				ulong      => ParameterType.ULong,
				float      => ParameterType.Float,
				double     => ParameterType.Double,
				string     => ParameterType.String,
				byte[]     => ParameterType.ByteArray,
				Vector3    => ParameterType.Vector3,
				Quaternion => ParameterType.Quaternion,
				Color      => ParameterType.Color,
				_          => null
			};

		/// <summary>Decoded value, or <c>null</c> when the stored bytes are unusable.</summary>
		public object ToObject() {
			var bytes = Converter.FromBase64(value);
			if (bytes == null)
				return null;

			try {
				return type switch {
					ParameterType.Bool       => bytes.To<bool>(),
					ParameterType.Byte       => bytes.To<byte>(),
					ParameterType.Short      => bytes.To<short>(),
					ParameterType.UShort     => bytes.To<ushort>(),
					ParameterType.Int        => bytes.To<int>(),
					ParameterType.UInt       => bytes.To<uint>(),
					ParameterType.Long       => bytes.To<long>(),
					ParameterType.ULong      => bytes.To<ulong>(),
					ParameterType.Float      => bytes.To<float>(),
					ParameterType.Double     => bytes.To<double>(),
					ParameterType.String     => bytes.To<string>(),
					ParameterType.ByteArray  => bytes,
					ParameterType.Vector3    => bytes.To<UnityEngine.Vector3>(),
					ParameterType.Quaternion => bytes.To<UnityEngine.Quaternion>(),
					ParameterType.Color      => bytes.To<UnityEngine.Color>(),
					_                        => null
				};
			} catch (Exception) {
				return null;
			}
		}

		/// <summary>
		/// Big-endian bytes of <paramref name="value"/>, coerced to <paramref name="type"/> first. The single
		/// encoding every stored or serialized parameter value goes through (see <see cref="ParameterEntry"/>).
		/// </summary>
		public static byte[] Bytes(object value, ParameterType type)
			=> type switch {
				ParameterType.Bool       => value.ToBool().ToBytes(),
				ParameterType.Byte       => value.ToByte().ToBytes(),
				ParameterType.Short      => value.ToShort().ToBytes(),
				ParameterType.UShort     => value.ToUShort().ToBytes(),
				ParameterType.Int        => value.ToInt().ToBytes(),
				ParameterType.UInt       => value.ToUInt().ToBytes(),
				ParameterType.Long       => value.ToLong().ToBytes(),
				ParameterType.ULong      => value.ToULong().ToBytes(),
				ParameterType.Float      => value.ToFloat().ToBytes(),
				ParameterType.Double     => value.ToDouble().ToBytes(),
				ParameterType.String     => Converter.ToString(value).ToBytes(),
				ParameterType.ByteArray  => value as byte[] ?? Array.Empty<byte>(),
				ParameterType.Vector3    => value.ToVector3().ToBytes(),
				ParameterType.Quaternion => value.ToQuaternion().ToBytes(),
				ParameterType.Color      => value.ToColor().ToBytes(),
				_                        => Array.Empty<byte>()
			};

		public bool Equals(ParameterValue other)
			=> other != null && other.type == type && other.value == value;

		public override bool Equals(object obj)
			=> Equals(obj as ParameterValue);

		public override int GetHashCode()
			=> ((int)type * 397) ^ (value?.GetHashCode() ?? 0);

		public override string ToString()
			=> $"{type}:{value}";
	}
}
