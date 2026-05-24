using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using Nox.Avatars.Rigging;
using Nox.CCK.Players;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;

namespace Nox.CCK.Avatars.Rigging {
	public abstract class BaseRiggingModule : MonoBehaviour, IRiggingModule, IParameterGroup {
		public IAvatarDescriptor Descriptor;

		public readonly List<IParameter> Parameters = new();
		public readonly List<RiggingPart> Parts = new();

		public bool Before(IRuntimeAvatar runtime) {
			Descriptor = runtime.Descriptor;
			return true;
		}

		public bool After(IRuntimeAvatar runtime) {
			if (!IKRigParameters.SetupParameters(this)) {
				Logger.LogError("Failed to setup rigging parameters.");
				return false;
			}

			var paramModule = runtime.Descriptor
				.GetModules<IParameterModule>()
				.FirstOrDefault();

			foreach (var p in this.Parameters)
				paramModule?.RegisterParameter(p);

			return true;
		}

		public abstract bool SetupParameters(BaseRiggingModule module);

		public abstract bool IsActive(HumanBodyBones bone);

		public abstract void SetActive(HumanBodyBones bone, bool active);


		public virtual UniTask<bool> Setup(IRuntimeAvatar runtime)
			=> UniTask.FromResult(true);

		bool IRiggingModule.TryGetPart(ushort id, out IRigPart part) {
			for (var i = 0; i < Parts.Count; i++) {
				if (Parts[i].GetId() != id) continue;
				part = Parts[i];
				return true;
			}
			part = null;
			return false;
		}

		public Transform GetPart(HumanBodyBones bone) {
			var index = bone.ToIndex();
			for (var i = 0; i < Parts.Count; i++) {
				if (Parts[i].GetId() == index)
					return Parts[i].GetTransform();
			}
			return null;
		}

		public IRigPart[] GetParts()
			=> Parts.Cast<IRigPart>().ToArray();

		public void SetPart(HumanBodyBones bone, Transform part) {
			var index = bone.ToIndex();
			for (var i = 0; i < Parts.Count; i++) {
				if (Parts[i].GetId() != index) continue;
				Parts[i].SetTransform(part);
				return;
			}

			var rigPart = new RiggingPart(index, part);
			Parts.Add(rigPart);
		}

		public Transform GetBone(HumanBodyBones bone)
			=> Descriptor.Animator.GetBoneTransform(bone);

		public IParameter[] GetParameters()
			=> Parameters.Cast<IParameter>().ToArray();

		public IParameter GetParameter(string n) {
			for (var i = 0; i < Parameters.Count; i++) {
				if (Parameters[i].GetName() == n)
					return Parameters[i];
			}
			return null;
		}

		public IParameter GetParameter(int hash) {
			for (var i = 0; i < Parameters.Count; i++) {
				if (Parameters[i].GetKey() == hash)
					return Parameters[i];
			}
			return null;
		}

		private void OnDestroy() {
			if (Descriptor == null)
				return;
			var paramModule = Descriptor.GetModules<IParameterModule>().FirstOrDefault();
			foreach (var p in Parameters)
				paramModule?.UnregisterParameter(p);
		}
	}
}