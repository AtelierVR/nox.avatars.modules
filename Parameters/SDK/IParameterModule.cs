using UnityEngine.Events;

namespace Nox.Avatars.Parameters {
	public interface IParameterModule : IAvatarModule, IParameterGroup {
		public void RegisterParameter(IParameter parameter);
		public void UnregisterParameter(IParameter parameter);

		public UnityEvent<IParameter> OnRegistred { get; }
		public UnityEvent<IParameter> OnUnRegistred { get; }

		/// <summary>
		/// Raised after the value of a parameter was written, with the parameter that changed (see
		/// <see cref="IParameter.OnValueChanged"/>). The single place to follow every parameter of an avatar from
		/// outside its packages (relay, controllers, editors…) instead of polling <see cref="IParameter.Value"/>.
		/// <para>
		/// Only the parameters that can announce a write are reported: a value a module publishes from its
		/// own state (rig weights, scale, jiggles) is not.
		/// </para>
		/// </summary>
		public UnityEvent<IParameter> OnValueChanged { get; }
	}
}