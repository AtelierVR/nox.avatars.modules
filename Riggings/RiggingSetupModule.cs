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
using Object = UnityEngine.Object;

namespace Nox.CCK.Avatars.Rigging {
	/// <summary>
	/// Injected automatically by nox.avatars.modules into every avatar during preparation
	/// when no explicit <see cref="BaseRiggingModule"/> component is found.
	/// Resolves the best available backend from <see cref="RiggingBackendRegistry"/> and
	/// drives the full IK rig setup pipeline.
	///
	/// Also exposes a synchronised <c>ik/type</c> parameter (see
	/// <see cref="Parameters.RiggingTypeParameter"/>) so a remote viewer can detect the owner's
	/// backend and hot-swap its own rig to match — without reloading the avatar.
	/// </summary>
	[DisallowMultipleComponent]
	public class RiggingSetupModule : MonoBehaviour, IAvatarModule {
		private IRuntimeAvatar   _runtime;
		private IRiggingBackend  _backend;
		private IRiggingModule   _module;
		private IParameter       _typeParameter;

		public int Priority
			=> 70;

		public static bool Check(IAvatarDescriptor descriptor)
			=> descriptor.GetModules<IRiggingModule>().Length switch {
				1 => true,
				0 => descriptor.Anchor.AddComponent<RiggingSetupModule>(),
				_ => false
			};

		public async UniTask<bool> Setup(IRuntimeAvatar runtime, AvatarModulePhase phase, CancellationToken token = default) {
			if (phase != AvatarModulePhase.Init) return true;
			_runtime = runtime;

			var backend = RiggingBackendRegistry.Resolve(runtime);
			if (backend == null) {
				Logger.LogWarning("No rigging backend available — rig will not be set up.");
				return true;
			}

			if (!CreateModule(backend)) {
				Logger.LogError($"Rigging backend '{backend.Id}' failed to create a module.");
				return false;
			}

			RegisterTypeParameter();

			await UniTask.NextFrame(cancellationToken: token);
			return true;
		}

		/// <summary>
		/// Instantiates the module for the given backend and stores it as the active module.
		/// </summary>
		private bool CreateModule(IRiggingBackend backend) {
			var module = backend.Instantiate(_runtime);
			if (module == null)
				return false;

			_backend = backend;
			_module  = module;
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
		/// uses a different backend). Swaps the current rig to the requested backend if possible,
		/// falling back to the current backend when the requested one is unavailable.
		/// </summary>
		private void OnBackendRequested(string backendId) {
			if (string.IsNullOrEmpty(backendId))
				return;
			if (_backend != null && _backend.Id == backendId)
				return; // already using the requested backend

			var requested = RiggingBackendRegistry.Resolve(backendId);
			if (requested == null) {
				// Fallback: keep the current backend (e.g. FinalIK not compiled on a desktop viewer).
				Logger.LogWarning(
					$"Requested rigging backend '{backendId}' is not registered; keeping '{_backend?.Id ?? "none"}'.",
					context: this,
					tag: nameof(RiggingSetupModule)
				);
				return;
			}

			SwapBackend(requested);
		}

		/// <summary>
		/// Destroys the current rig (module + generated targets) and instantiates the new backend.
		/// </summary>
		private void SwapBackend(IRiggingBackend next) {
			var oldModule = _module as BaseRiggingModule;

			// 1. Dispose the old module and its generated IK targets.
			if (oldModule != null)
				oldModule.enabled = false;

			// 2. Instantiate the new module (its generator creates fresh targets).
			if (!CreateModule(next)) {
				// Rollback: recreate the previous backend if the new one fails.
				if (_backend != null && _module == null)
					CreateModule(_backend);
				Logger.LogError(
					$"Failed to hot-swap rig to '{next.Id}'; kept '{_backend?.Id ?? "none"}'.",
					context: this,
					tag: nameof(RiggingSetupModule)
				);
				return;
			}

			// 3. Remove the old module's component and targets after the new rig is in place.
			if (oldModule != null) {
				oldModule.Cleanup();
				Object.Destroy(oldModule);
			}

			Logger.LogDebug(
				$"Hot-swapped rigging backend from '{oldModule?.BackendId ?? "none"}' to '{next.Id}'.",
				context: this,
				tag: nameof(RiggingSetupModule)
			);
		}

		private void OnDestroy() {
			if (_typeParameter == null || _runtime == null)
				return;
			var paramModule = _runtime.Descriptor
				?.GetModules<IParameterModule>()
				.FirstOrDefault();
			paramModule?.UnregisterParameter(_typeParameter);
		}
	}
}