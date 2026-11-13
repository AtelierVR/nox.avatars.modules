using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using Nox.Avatars.Rigging;
using Nox.CCK.Events;
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
	/// system via <see cref="IRigProvider"/>. It also synchronises the <c>Rig/Type</c> parameter
	/// (see <see cref="RiggingTypeParameter"/>) so a remote viewer can detect the owner's backend
	/// and hot-swap its own rig to match — without reloading the avatar.
	/// </summary>
	[DisallowMultipleComponent]
	[DefaultExecutionOrder(1000)]
	public class RigManagerModule : MonoBehaviour, IAvatarModule, IRigProvider {
		private IRuntimeAvatar  _runtime;
		private IRiggingBackend _backend;
		private IRigging        _rig;
		private IParameter      _typeParameter;

		/// <summary>
		/// Raised when the active rig is replaced (backend hot-swap) or disposed.
		/// <para>
		/// The rig holds the IK targets every tracked part is written on: a consumer must know when
		/// those targets changed, instead of comparing the rig instance every frame.
		/// </para>
		/// </summary>
		public NoxEvent<IRigging> OnRigChanged { get; } = new();

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

			// Enregistré AVANT la résolution du backend : c'est ce paramètre qui reçoit l'annonce
			// du propriétaire et déclenche le hot-swap. S'il n'existe pas côté viewer, la valeur
			// annoncée ne peut jamais être appliquée (elle reste dans un UnassignedProperty) et
			// le rig ne matche jamais. Sans backend on annonce CRC32("") = 0, information
			// légitime : aucun backend en face ne répondra à cette clé.
			RegisterTypeParameter();

			if (_rig != null) {
				Logger.LogDebug($"Rig '{_rig.Id}' is already set up for this avatar, keeping it.", context: this, tag: nameof(RigManagerModule));
				return true;
			}

			var backend = RiggingBackendRegistry.Resolve(runtime);
			if (backend == null) {
				Logger.LogWarning("No rigging backend available — rig will not be set up.");
				return true;
			}

			if (!CreateRig(backend)) {
				Logger.LogError($"Rigging backend '{backend.Id}' failed to create a rig.");
				return false;
			}

			await UniTask.NextFrame(cancellationToken: token);
			return true;
		}

		/// <inheritdoc/>
		public IRigging Rig
			=> _rig;

		/// <summary>
		/// Forwards the late update to the active rig: it runs after the Animator (and the animation graph)
		/// evaluated, which is the only point where a backend can still write a bone the animation drives.
		/// </summary>
		private void LateUpdate()
			=> (_rig as BaseRigging)?.LateUpdate();

		/// <summary>Creates the rig for the given backend and stores it as the active rig.</summary>
		private bool CreateRig(IRiggingBackend backend) {
			var rig = backend.Create(_runtime);
			if (rig == null)
				return false;

			_backend = backend;
			_rig     = rig;

			Logger.LogDebug(
				$"Rig '{rig.Id}' created on '{_runtime?.Descriptor?.Anchor?.name}' with {rig.GetParts().Length} part(s) and {rig.GetParameters().Length} parameter(s).",
				context: this,
				tag: nameof(RigManagerModule)
			);

			OnRigChanged.Invoke(rig);
			return true;
		}

		/// <summary>
		/// Registers the synchronised <c>Rig/Type</c> parameter on the avatar's parameter module.
		/// The parameter reads the active backend id and, when set remotely, triggers a hot-swap.
		/// </summary>
		private void RegisterTypeParameter() {
			if (_typeParameter != null)
				return;

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
		/// Called when the <c>Rig/Type</c> parameter is set (typically on a remote viewer whose owner
		/// uses a different backend). Resolves the requested backend from the raw CRC32 and swaps
		/// the current rig, falling back to the current backend when it is unavailable.
		/// </summary>
		private void OnBackendRequested(int crc) {
			var requested = ResolveByCrc(crc);
			Logger.LogDebug($"Receive backend rigging {crc} === {requested.Id}");
			if (requested == null) {
				Logger.LogWarning(
					$"Requested rigging backend (crc={crc}) is not registered; keeping '{_backend?.Id ?? "none"}'.",
					context: this,
					tag: nameof(RigManagerModule)
				);
				return;
			}

			if (_backend != null && _backend.Id == requested.Id)
				return;

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

		/// <summary>Disposes the active rig (unregisters its parameters and destroys its IK targets), if any.</summary>
		private void DisposeRig() {
			if (_rig == null)
				return;

			_rig.Dispose();
			_rig = null;

			OnRigChanged.Invoke(null);
		}

		/// <summary>Disposes the current rig and creates a new one for the given backend.</summary>
		private void SwapRig(IRiggingBackend next) {
			var oldRig     = _rig;
			var oldBackend = _backend;
			var oldId      = oldRig?.Id ?? oldBackend?.Id ?? "none";

			DisposeRig();

			if (CreateRig(next)) {
				Logger.LogDebug(
					$"Hot-swapped rigging backend from '{oldId}' to '{next.Id}'.",
					context: this,
					tag: nameof(RigManagerModule)
				);
				return;
			}

			Logger.LogError(
				$"Failed to hot-swap rig to '{next.Id}'; attempting to restore '{oldId}'.",
				context: this,
				tag: nameof(RigManagerModule)
			);

			if (oldBackend == null || oldBackend.Id == next.Id)
				return;

			if (CreateRig(oldBackend))
				Logger.LogDebug(
					$"Restored rigging backend '{oldBackend.Id}' after the failed swap.",
					context: this,
					tag: nameof(RigManagerModule)
				);
			else
				Logger.LogError(
					$"Failed to restore rigging backend '{oldBackend.Id}'; no rig is active.",
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
			DisposeRig();
		}
	}
}
