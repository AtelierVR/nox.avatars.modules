using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using Nox.Controllers;

namespace Nox.CCK.Avatars.Camera {
	/// <summary>
	/// Avatar module that hides the local avatar's head from the player's own camera
	/// (first-person "head chop") by scaling the head and neck bones to zero before
	/// each render and restoring them after.
	/// Works for any avatar topology, including single-mesh skinned avatars.
	/// The watched camera is set externally by the module system
	/// (see <c>nox.avatars.modules/Runtime/Main.cs</c>).
	/// </summary>
	[DisallowMultipleComponent]
	public class CameraChopModule : CameraChop, IAvatarModule {
		public int GetPriority() 
			=> 0;

		public async UniTask<bool> Setup(IRuntimeAvatar runtime) {
			// Pre-validation
			if (Bones == null || Bones.Length == 0) {
				Logger.LogWarning("No head/neck bones found, cannot perform camera chop.", tag: nameof(CameraChopModule));
				enabled = false;
				return true; // Return true to avoid retrying setup; the module will just be disabled
			}

			await UniTask.Yield();

			// Case when is not the local avatar, or no controller/camera is available. 
			// Just disable the module and do nothing.
			if (!runtime.Arguments.TryGetValue("local", out var c) || c is not bool isLocal || !isLocal) {	
				enabled = false;
				return true;
			}

			var animator = runtime.Descriptor.Animator;

			// Enable per-frame bone matrix recalculation so scale changes take effect immediately
			foreach (var smr in animator.GetComponentsInChildren<SkinnedMeshRenderer>(true)) {
				smr.forceMatrixRecalculationPerRender = true;
				smr.updateWhenOffscreen = true;
			}

			return true;
		}

		// ── Static helpers ──────────────────────────────────────────────────────

		public static bool Check(IAvatarDescriptor descriptor)
            => descriptor.GetModules<CameraChopModule>().Length switch {
				> 1 => false, // Multiple CameraChopModules are not allowed
				_   => true, // Allow zero or one CameraChopModule; you need to add one manually if you want it
			};
	}
}
