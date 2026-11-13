using UnityEngine.Events;

namespace Nox.Avatars.Parameters {
	/// <summary>
	/// Follows the writes of a parameter or of a whole module without polling, from any package that
	/// references <c>Nox.Avatars.Parameters</c> (relay, desktop, xr, offline…).
	/// <para>
	/// A parameter only announces its writes when it can (see <see cref="IParameter.OnValueChanged"/>):
	/// the helpers below return <c>false</c> otherwise, so the caller knows it has to read the value itself.
	/// </para>
	/// </summary>
	public static class ParameterEvents {
		/// <summary>
		/// Subscribes <paramref name="listener"/> to the writes of <paramref name="parameter"/>. Returns
		/// <c>false</c> when the parameter does not announce them.
		/// </summary>
		public static bool Subscribe(this IParameter parameter, UnityAction<IParameter> listener) {
			var events = parameter?.OnValueChanged;
			if (events == null || listener == null)
				return false;

			events.AddListener(listener);
			return true;
		}

		/// <summary>Removes <paramref name="listener"/> from the writes of <paramref name="parameter"/>.</summary>
		public static bool Unsubscribe(this IParameter parameter, UnityAction<IParameter> listener) {
			var events = parameter?.OnValueChanged;
			if (events == null || listener == null)
				return false;

			events.RemoveListener(listener);
			return true;
		}

		/// <summary>Whether <paramref name="parameter"/> announces its writes.</summary>
		public static bool IsObservable(this IParameter parameter)
			=> parameter?.OnValueChanged != null;

		/// <summary>Subscribes <paramref name="listener"/> to every parameter write of <paramref name="module"/>.</summary>
		public static bool Subscribe(this IParameterModule module, UnityAction<IParameter> listener) {
			var events = module?.OnValueChanged;
			if (events == null || listener == null)
				return false;

			events.AddListener(listener);
			return true;
		}

		/// <summary>Removes <paramref name="listener"/> from the parameter writes of <paramref name="module"/>.</summary>
		public static bool Unsubscribe(this IParameterModule module, UnityAction<IParameter> listener) {
			var events = module?.OnValueChanged;
			if (events == null || listener == null)
				return false;

			events.RemoveListener(listener);
			return true;
		}
	}
}
