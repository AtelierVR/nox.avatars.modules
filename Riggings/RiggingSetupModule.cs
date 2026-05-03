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

			if (module is BaseRiggingModule baseModule)
				baseModule.Descriptor = runtime.Descriptor;

			backend.SetupRig(module);

			if (module is BaseRiggingModule bm) {
				if (!IKRigParameters.SetupParameters(bm)) {
					Logger.LogError("Failed to setup rigging parameters.");
					return false;
				}

				var paramModule = runtime.Descriptor
					.GetModules<IParameterModule>()
					.FirstOrDefault();

				foreach (var p in bm.Parameters)
					paramModule?.RegisterParameter(p);
			}

            if(GetType() == typeof(RiggingSetupModule))
                this.Destroy();

			await UniTask.NextFrame();
			return true;
		}
	}
}
