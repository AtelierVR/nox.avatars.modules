using System.Collections.Generic;
using System.Linq;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using Nox.Avatars.Rigging;
using Nox.CCK.Players;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using Object = UnityEngine.Object;
using Transform = UnityEngine.Transform;

namespace Nox.CCK.Avatars.Rigging {
	/// <summary>
	/// Base (non-MonoBehaviour) implementation of <see cref="IRigging"/>.
	/// Backends derive from this and provide <see cref="SetupParameters"/> plus the rig
	/// generator that creates the IK targets and populates <see cref="Parts"/>.
	///
	/// Instances are created, swapped and disposed by the rig manager owning the avatar.
	/// They are never attached as components.
	/// </summary>
	public abstract class BaseRigging : IRigging {
		public IAvatarDescriptor Descriptor;

		public readonly List<IParameter> Parameters = new();
		public readonly List<RiggingPart> Parts = new();

		/// <summary>The id of the backend that created this rig (e.g. "rigbuilder", "finalik").</summary>
		public string Id { get; set; }

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

			foreach (var p in Parameters)
				paramModule?.RegisterParameter(p);

			return true;
		}

		public abstract bool SetupParameters(BaseRigging module);

		public abstract bool IsActive(HumanBodyBones bone);

		public abstract void SetActive(HumanBodyBones bone, bool active);

		public bool TryGetPart(ushort id, out IRigPart part) {
			for (var i = 0; i < Parts.Count; i++) {
				if (Parts[i].GetId() != id)
					continue;
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
				if (Parts[i].GetId() != index)
					continue;
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

		/// <summary>
		/// Destroys any generated IK target GameObjects owned by this rig.
		/// Called before disposing/swapping so stale VRIK_*/IKRig_* targets do not linger.
		/// </summary>
		public virtual void Dispose() {
			UnregisterParameters();
			Cleanup();
		}

		protected void Cleanup() {
			for (var i = Parts.Count - 1; i >= 0; i--) {
				var part = Parts[i];
				Parts.RemoveAt(i);
				var t = part.GetTransform();
				if (t && t.gameObject)
					Object.Destroy(t.gameObject);
			}
		}

		protected void UnregisterParameters() {
			if (Descriptor == null)
				return;
			var paramModule = Descriptor.GetModules<IParameterModule>().FirstOrDefault();
			foreach (var p in Parameters)
				paramModule?.UnregisterParameter(p);
		}
	}
}
