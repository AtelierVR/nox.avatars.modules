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

		// Two independent channels feeding the same bones:
		//  - controller channel (_controllerActive): what the player controller (Desktop/XR/Remote) wants;
		//  - avatar channel (_trackingOverride): what a state behavior (TrackingControl) forces, or nothing.
		// The avatar override wins while it is set; otherwise the controller's value applies, and when neither
		// was set we fall back to the backend's real state (e.g. before a controller wrote anything).
		private readonly Dictionary<HumanBodyBones, bool> _active = new();
		private readonly Dictionary<HumanBodyBones, bool> _tracking = new();

		/// <summary>
		/// Effective state of the bone: the avatar's tracking override when one is set, otherwise the
		/// controller's choice, otherwise the backend's real state.
		/// </summary>
		public bool IsActive(HumanBodyBones bone)
			=> _tracking.TryGetValue(bone, out var tracking) ? tracking
				: _active.TryGetValue(bone, out var active) ? active
					: GetActive(bone);

		/// <summary>Controller channel: the player controller declares whether it drives the bone.</summary>
		public void SetActive(HumanBodyBones bone, bool active) {
			_active[bone] = active;
			ApplyActive(bone, IsActive(bone));
		}

		/// <summary>
		/// Avatar channel: overrides tracking for the bone (<see cref="RiggingTrackingMode.Tracking"/> = follow IK,
		/// <see cref="RiggingTrackingMode.Animation"/> = animation), or clears the override with
		/// <see cref="RiggingTrackingMode.Normal"/> to let the controller decide again.
		/// </summary>
		public void SetTracking(HumanBodyBones bone, RiggingTrackingMode mode) {
			if (mode == RiggingTrackingMode.Normal)
				_tracking.Remove(bone);
			else
				_tracking[bone] = mode == RiggingTrackingMode.Tracking;

			ApplyActive(bone, IsActive(bone));
		}

		/// <summary>
		/// Avatar channel read: whether the avatar currently requests tracking for the bone (its override is
		/// <see cref="RiggingTrackingMode.Tracking"/>). <c>false</c> when there is no override or the override
		/// is <see cref="RiggingTrackingMode.Animation"/>.
		/// </summary>
		public bool IsTracking(HumanBodyBones bone)
			=> _tracking.TryGetValue(bone, out var tracking) && tracking;

		/// <summary>
		/// Avatar channel mode for the bone: <see cref="RiggingTrackingMode.Animation"/>/<see cref="RiggingTrackingMode.Tracking"/>
		/// when the avatar set an override, <see cref="RiggingTrackingMode.Normal"/> when it has none.
		/// </summary>
		public RiggingTrackingMode GetTracking(HumanBodyBones bone)
			=> _tracking.TryGetValue(bone, out var tracking)
				? (tracking ? RiggingTrackingMode.Tracking : RiggingTrackingMode.Animation)
				: RiggingTrackingMode.Normal;

		/// <summary>Backend read of the bone's real active state (used before any channel wrote).</summary>
		protected abstract bool GetActive(HumanBodyBones bone);

		/// <summary>
		/// Le contrôleur prend la main sur la racine (voir <see cref="IRigging.SetExternalRootControl"/>).
		/// Les backends qui pilotent la racine eux-mêmes (FinalIK : locomotion + VRIKRootController) surchargent.
		/// </summary>
		public virtual void SetExternalRootControl(bool external) { }

		/// <summary>Backend write of the bone's active state on the actual rig (RigLayer.active, VRIK weights...).</summary>
		protected abstract void ApplyActive(HumanBodyBones bone, bool active);

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
			for (var i = 0; i < Parts.Count; i++)
				if (Parts[i].GetId() == index)
					return Parts[i].GetTransform();
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
			for (var i = 0; i < Parameters.Count; i++)
				if (Parameters[i].Name == n)
					return Parameters[i];
			return null;
		}

		public IParameter GetParameter(int hash) {
			for (var i = 0; i < Parameters.Count; i++)
				if (Parameters[i].Key == hash)
					return Parameters[i];
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
