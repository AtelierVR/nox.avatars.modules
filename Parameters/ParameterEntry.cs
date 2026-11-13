using System;
using System.Runtime.CompilerServices;
using Nox.Avatars.Parameters;
using UnityEngine;

namespace Nox.CCK.Avatars.Parameters {
	[Serializable]
	public class ParameterEntry {
		public string        name;
		public ParameterType type;

		/// <summary>Default value of the parameter, base64 of its bytes (see <see cref="ParameterValue"/>).</summary>
		public string value;

		/// <summary>
		/// Behaviour of the parameter: who may edit it (<see cref="ParameterFlags.OwnerEditable"/>,
		/// <see cref="ParameterFlags.ViewerEditable"/>), how it is synchronized
		/// (<see cref="ParameterFlags.OwnerSyncsToViewers"/>, <see cref="ParameterFlags.ViewerSyncsToOwner"/>) and
		/// whether it survives a session (<see cref="ParameterFlags.Persistent"/>).
		/// </summary>
		public ParameterFlags flags;

		public int GetNameHash()
			=> Animator.StringToHash(name);

		/// <summary>The default value of the entry as a typed value, so it goes through the same codec as the saved ones.</summary>
		public ParameterValue ToParameterValue()
			=> new(type, value);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetDefaultValue<T>(T newValue) {
			if (newValue is null) {
				value = string.Empty;
				return;
			}

			var encoded = ParameterValue.From(newValue)
				?? throw new ArgumentException($"Unsupported type: {typeof(T)}");

			type  = encoded.type;
			value = encoded.value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T GetDefaultValue<T>()
			=> ToParameterValue().ToObject() is T typed ? typed : default;
	}
}
