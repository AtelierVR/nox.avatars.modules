using System.Collections.Generic;
using System.Linq;
using Nox.Avatars;
using Nox.Avatars.Rigging;

namespace Nox.CCK.Avatars.Rigging {
	/// <summary>
	/// Central registry for rigging backends.
	/// Backends self-register at startup via <c>[RuntimeInitializeOnLoadMethod]</c>.
	/// At avatar setup time <see cref="Resolve"/> picks the best backend based on:
	/// <list type="number">
	///   <item>A global override (if set)</item>
	///   <item>Explicit <see cref="RiggingPreference"/> entries (most-specific controller type first, then by priority)</item>
	///   <item>Fallback to the backend with the highest <see cref="IRiggingBackend.CanHandle"/> score</item>
	/// </list>
	/// </summary>
	public static class RiggingBackendRegistry {
		private static readonly List<IRiggingBackend> _backends = new();

		// ── Registration ────────────────────────────────────────────────────────

		/// <summary>
		/// Registers a backend.  No-op if a backend with the same <see cref="IRiggingBackend.Id"/>
		/// is already registered.
		/// </summary>
		public static void Register(IRiggingBackend backend) {
			if (backend == null)
				return;
			if (_backends.Any(b => b.Id == backend.Id))
				return;
			_backends.Add(backend);
		}

		/// <summary>Unregisters a backend and disposes it.</summary>
		public static void Unregister(IRiggingBackend backend) {
			if (backend == null)
				return;
			_backends.RemoveAll(b => b.Id == backend.Id);
			backend.Dispose();
		}

		// ── Resolution ───────────────────────────────────────────────────────────

		/// <summary>
		/// Returns the best available backend for the given runtime arguments, or <c>null</c> if none
		/// are registered or none can handle the arguments.
		/// </summary>
		public static IRiggingBackend Resolve(IRuntimeAvatar runtime)
			=> _backends
				.Select(b => (backend: b, score: b.CanHandle(runtime)))
				.Where(t => t.score >= 0)
				.OrderByDescending(t => t.score)
				.Select(t => t.backend)
				.FirstOrDefault();
		
		/// <summary>Returns a snapshot of all registered backends.</summary>
		public static IRiggingBackend[] GetBackends()
			=> _backends.ToArray();

		public static void Clear() {
			foreach (var backend in _backends.ToArray())
				backend.Dispose();
			_backends.Clear();
		}
	}
}