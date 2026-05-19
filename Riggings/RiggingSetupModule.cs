using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using Nox.Avatars.Rigging;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.Rigging {
	/// <summary>
	/// Injected automatically by nox.avatars.modules into every avatar during preparation
	/// when no explicit <see cref="BaseRiggingModule"/> component is found.
	/// Resolves the best available backend from <see cref="RiggingBackendRegistry"/> and
	/// drives the full IK rig setup pipeline.
	/// </summary>
	[DisallowMultipleComponent]
	public class RiggingSetupModule : MonoBehaviour, IAvatarModule {

		public static bool Check(IAvatarDescriptor descriptor)
			=> descriptor.GetModules<IRiggingModule>().Length switch {
				1 => true,
                0 => descriptor.Anchor.AddComponent<RiggingSetupModule>(),
				_ => false
			};

        public async UniTask<bool> Setup(IRuntimeAvatar runtime) {
			var backend = RiggingBackendRegistry.Resolve(runtime);
			if (backend == null) {
				Logger.LogWarning("No rigging backend available — rig will not be set up.");
				return true;
			}

			var module = backend.Instantiate(runtime);
			if (module == null) {
				Logger.LogError($"Rigging backend '{backend.Id}' failed to create a module.");
				return false;
			}

			await UniTask.NextFrame();
			return true;
		}
	}
}
