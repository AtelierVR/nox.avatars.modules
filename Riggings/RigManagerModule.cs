using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using Nox.Avatars.Rigging;
using Nox.CCK.Utils;
using Nox.CCK.Avatars.Rigging.Parameters;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.Rigging {
	/// <summary>
	/// Injected automatically by nox.avatars.modules into every avatar during preparation.
	///
	/// Owns the avatar's live <see cref="IRigging"/> instance: it resolves the best
	/// <see cref="IRiggingBackend"/>, creates the initial rig, and exposes it to the rest of the
	/// system via <see cref="IRigProvider"/>. It also synchronises the <c>ik/type</c> parameter
	/// (see <see cref="RiggingTypeParameter"/>) so a remote viewer can detect the owner's backend
	/// and hot-swap its own rig to match — without reloading the avatar.
	/// </summary>
	[DisallowMultipleComponent]
	public class RigManagerModule : MonoBehaviour, IAvatarModule, IRigProvider {
		private IRuntimeAvatar  _runtime;
		private IRiggingBackend _backend;
		private IRigging        _rig;
		private IParameter      _typeParameter;

		public int Priority
			=> 70;

		public static bool Check(IAvatarDescriptor descriptor) {
			var providers = descriptor.Anchor.GetComponentsInChildren<IRigProvider>(true);
			if (providers.Length > 0)
				return true;
			descriptor.Anchor.AddComponent<RigManagerModule>();
			return true;
		}

		public async UniTask<bool> Setup(IRuntimeAvatar runtime, AvatarModulePhase phase, CancellationToken token = default) {
			if (phase != AvatarModulePhase.Init) return true;
			_runtime = runtime;

			var backend = RiggingBackendRegistry.Resolve(runtime);
			if (backend == null) {
				Logger.LogWarning("No rigging backend available — rig will not be set up.");
				return true;
			}

			if (!CreateRig(backend)) {
				Logger.LogError($"Rigging backend '{backend.Id}' failed to create a rig.");
				return false;
			}

			RegisterTypeParameter();

			await UniTask.NextFrame(cancellationToken: token);
			return true;
		}

		/// <inheritdoc/>
		public IRigging GetRig()
			=> _rig;

		/// <summary>Creates the rig for the given backend and stores it as the active rig.</summary>
		private bool CreateRig(IRiggingBackend backend) {
			var rig = backend.Create(_runtime);
			if (rig == null)
				return false;

			_backend = backend;
			_rig     = rig;
			return true;
		}

		/// <summary>
		/// Registers the synchronised <c>ik/type</c> parameter on the avatar's parameter module.
		/// The parameter reads the active backend id and, when set remotely, triggers a hot-swap.
		/// </summary>
		private void RegisterTypeParameter() {
			if (_typeParameter != null)
				return; // already registered

			var paramModule = _runtime?.Descriptor
				?.GetModules<IParameterModule>()
				.FirstOrDefault();
			if (paramModule == null)
				return;

			_typeParameter = new RiggingTypeParameter(
				getBackendId: () => _backend?.Id ?? string.Empty,
				onChanged:    OnBackendRequested
			);
			paramModule.RegisterParameter(_typeParameter);
		}

		/// <summary>
		/// Called when the <c>ik/type</c> parameter is set (typically on a remote viewer whose owner
		/// uses a different backend). Resolves the requested backend from the raw CRC32 and swaps
		/// the current rig, falling back to the current backend when it is unavailable.
		/// </summary>
		private void OnBackendRequested(int crc) {
			var requested = ResolveByCrc(crc);
			if (requested == null) {
				Logger.LogWarning(
					$"Requested rigging backend (crc={crc}) is not registered; keeping '{_backend?.Id ?? "none"}'.",
					context: this,
					tag: nameof(RigManagerModule)
				);
				return;
			}

			if (_backend != null && _backend.Id == requested.Id)
				return; // already using the requested backend

			SwapRig(requested);
		}

		/// <summary>Resolves a backend whose id hashes to the given CRC32, or <c>null</c>.</summary>
		private static IRiggingBackend ResolveByCrc(int crc) {
			foreach (var backend in RiggingBackendRegistry.GetBackends()) {
				if (Hash.CRC32(backend.Id) == crc)
					return backend;
			}
			return null;
		}

		/// <summary>Disposes the current rig and creates a new one for the given backend.</summary>
		private void SwapRig(IRiggingBackend next) {
			var oldRig = _rig;
			var oldId  = oldRig?.Id ?? "none";

			// 1. Dispose the old rig (destroys its generated IK targets + unregisters params).
			oldRig?.Dispose();

			// 2. Create the new rig.
			if (!CreateRig(next)) {
				Logger.LogError(
					$"Failed to hot-swap rig to '{next.Id}'; attempting to restore '{oldId}'.",
					context: this,
					tag: nameof(RigManagerModule)
				);
				// Rollback: recreate the previous backend if it was replaced.
				if (_backend != null && _rig == null && _backend.Id == next.Id)
					CreateRig(_backend);
				return;
			}

			Logger.LogDebug(
				$"Hot-swapped rigging backend from '{oldId}' to '{next.Id}'.",
				context: this,
				tag: nameof(RigManagerModule)
			);
		}

		private void OnDestroy() {
			if (_typeParameter != null && _runtime != null) {
				var paramModule = _runtime.Descriptor
					?.GetModules<IParameterModule>()
					.FirstOrDefault();
				paramModule?.UnregisterParameter(_typeParameter);
			}
			_rig?.Dispose();
			_rig = null;
		}
	}
}
