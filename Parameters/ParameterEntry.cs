using System;
using System.Runtime.CompilerServices;
using Nox.Avatars.Parameters;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;

namespace Nox.CCK.Avatars.Parameters {
	[Serializable]
	public class ParameterEntry {
		public string         name;
		public ParameterType  type;
		public string         defaultValue;
		public bool           synced;
		public bool           savable;
		public ParameterFlags flags;

		public int GetNameHash()
			=> Animator.StringToHash(name);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetDefaultValue<T>(T newValue) {
			if (newValue is null) {
				defaultValue = string.Empty;
				return;
			}

			// Sérialisation déléguée à Nox.CCK.Converter (big-endian, cohérent avec le réseau),
			// puis encodée en base64 pour rester sérialisable par Unity.
			(defaultValue, type) = newValue switch {
				string str   => (Converter.ToBase64(str.ToBytes()), ParameterType.String),
				byte[] bytes => (Converter.ToBase64(bytes.ToBytes()), ParameterType.ByteArray),
				bool b       => (Converter.ToBase64(b.ToBytes()), ParameterType.Bool),
				byte b       => (Converter.ToBase64(b.ToBytes()), ParameterType.Byte),
				short s      => (Converter.ToBase64(s.ToBytes()), ParameterType.Short),
				ushort us    => (Converter.ToBase64(us.ToBytes()), ParameterType.UShort),
				int i        => (Converter.ToBase64(i.ToBytes()), ParameterType.Int),
				uint ui      => (Converter.ToBase64(ui.ToBytes()), ParameterType.UInt),
				long l       => (Converter.ToBase64(l.ToBytes()), ParameterType.Long),
				ulong ul     => (Converter.ToBase64(ul.ToBytes()), ParameterType.ULong),
				float f      => (Converter.ToBase64(f.ToBytes()), ParameterType.Float),
				double d     => (Converter.ToBase64(d.ToBytes()), ParameterType.Double),
				Vector3 v    => (Converter.ToBase64(v.ToBytes()), ParameterType.Vector3),
				Quaternion q => (Converter.ToBase64(q.ToBytes()), ParameterType.Quaternion),
				_            => throw new ArgumentException($"Unsupported type: {typeof(T)}")
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T GetDefaultValue<T>() {
			var bytes = Converter.FromBase64(defaultValue);
			return bytes != null ? bytes.To<T>() : default;
		}
	}
}