using Nox.Avatars.Rigging;
using Nox.CCK.Avatars.Rigging.Parameters;
using UnityEngine;


namespace Nox.CCK.Avatars.Rigging {
	public static class IKRigParameters {
		public static bool SetupParameters(BaseRiggingModule module) {
			module.Parameters.Clear();

			// Paramètres communs pour tous les bones
			for (var i = 0; i < (int)HumanBodyBones.LastBone; i++) {
				var bone = (HumanBodyBones)i;
				if (!module.GetPart(bone)) continue;
				module.Parameters.Add(new RiggingActiveParameter(bone, module));
				module.Parameters.Add(new RiggingPositionParameter(bone, module));
				module.Parameters.Add(new RiggingRotationParameter(bone, module));
			}

			return module.SetupParameters(module);
		}
	}
}