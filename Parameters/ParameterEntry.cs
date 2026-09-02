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
		public byte[]         defaultValue;
		public bool           synced;
		public bool           savable;
		public ParameterFlags flags;

		public int GetNameHash()
			=> Animator.StringToHash(name);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetDefaultValue<T>(T newValue) {
			if (newValue is null) {
				defaultValue = Array.Empty<byte>();
				return;
			}

			// Sérialisation déléguée à Nox.CCK.Converter (big-endian, cohérent avec le réseau)
			(defaultValue, type) = newValue switch {
				string str   => (str.ToBytes(), ParameterType.String),
				byte[] bytes => (bytes.ToBytes(), ParameterType.ByteArray),
				bool b       => (b.ToBytes(), ParameterType.Bool),
				byte b       => (b.ToBytes(), ParameterType.Byte),
				short s      => (s.ToBytes(), ParameterType.Short),
				ushort us    => (us.ToBytes(), ParameterType.UShort),
				int i        => (i.ToBytes(), ParameterType.Int),
				uint ui      => (ui.ToBytes(), ParameterType.UInt),
				long l       => (l.ToBytes(), ParameterType.Long),
				ulong ul     => (ul.ToBytes(), ParameterType.ULong),
				float f      => (f.ToBytes(), ParameterType.Float),
				double d     => (d.ToBytes(), ParameterType.Double),
				Vector3 v    => (v.ToBytes(), ParameterType.Vector3),
				Quaternion q => (q.ToBytes(), ParameterType.Quaternion),
				_            => throw new ArgumentException($"Unsupported type: {typeof(T)}")
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T GetDefaultValue<T>() {
			if (defaultValue == null || defaultValue.Length == 0)
				return default;

			return defaultValue.To<T>();
		}
	}
}