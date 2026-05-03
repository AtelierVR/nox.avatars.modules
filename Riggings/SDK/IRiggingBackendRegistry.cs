namespace Nox.Avatars.Rigging {
	/// <summary>
	/// Public API exposed by the <c>nox.avatars.modules</c> mod for managing rigging backends.
	/// Retrieve it via: <c>api.ModAPI.GetMod("nox.avatars.modules").GetInstance&lt;IRiggingBackendRegistry&gt;()</c>
	/// </summary>
	public interface IRiggingBackendRegistry {
		/// <summary>Registers a backend. No-op if a backend with the same <see cref="IRiggingBackend.Id"/> is already registered.</summary>
		void Register(IRiggingBackend backend);

		/// <summary>Unregisters a backend and disposes it.</summary>
		void Unregister(IRiggingBackend backend);

		/// <summary>Returns the best backend for the given runtime arguments, or <c>null</c> if none available.</summary>
		IRiggingBackend Resolve(IRuntimeAvatar runtime);

		/// <summary>Returns a snapshot of all registered backends.</summary>
		IRiggingBackend[] GetBackends();
	}
}
