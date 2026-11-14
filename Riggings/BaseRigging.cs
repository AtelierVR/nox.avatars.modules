using System.Collections.Generic;
using System.Linq;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using Nox.Avatars.Rigging;
using Nox.CCK.Entities;
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

			var effective = IsActive(bone);
			Logger.LogDebug($"[Rig:{Id}] controller {bone} = {active} → applied {effective} (avatar override: {GetTracking(bone)})");

			ApplyActive(bone, effective);
		}

		/// <summary>
		/// Writes both channels again on the backend.
		/// <para>
		/// A backend whose solver is created lazily cannot write anything before it exists: <see cref="SetActive"/> and
		/// <see cref="SetTracking"/> then only record the state, and without this the slots keep the generator's
		/// defaults (VRIK spine at 0, arms animation-driven) for good. Backends call it as soon as their solver is
		/// available, so a channel declared early is honoured.
		/// </para>
		/// </summary>
		public void ReapplyActive() {
			Logger.LogDebug($"[Rig:{Id}] re-apply: {_active.Count} controller channel(s), {_tracking.Count} avatar channel(s)");

			foreach (var bone in _active.Keys)
				ApplyActive(bone, IsActive(bone));

			foreach (var bone in _tracking.Keys)
				if (!_active.ContainsKey(bone))
					ApplyActive(bone, IsActive(bone));
		}

		public bool HasControllerActive(HumanBodyBones bone)
			=> _active.TryGetValue(bone, out var active) && active;

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

			var effective = IsActive(bone);
			Logger.LogDebug($"[Rig:{Id}] avatar {bone} = {mode} → applied {effective} (controller: {(_active.TryGetValue(bone, out var controller) ? controller.ToString() : "none")})");

			ApplyActive(bone, effective);
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

		/* ------------------------------------------------------------------ */
		/* Reference pose                                                       */
		/* ------------------------------------------------------------------ */

		/// <summary>Spine bones, root first: only the ones the avatar exposes are used.</summary>
		private static readonly HumanBodyBones[] SpineChain = {
			HumanBodyBones.Hips,
			HumanBodyBones.Spine,
			HumanBodyBones.Chest,
			HumanBodyBones.UpperChest,
			HumanBodyBones.Neck,
			HumanBodyBones.Head
		};

		private bool _referencePose;

		/// <summary>
		/// Holds a reference pose (body upright, limbs straight, feet on the ground) until it is released.
		/// </summary>
		public bool ReferencePose {
			get => _referencePose;
			set {
				if (_referencePose == value)
					return;

				_referencePose = value;
				Logger.LogDebug($"[Rig:{Id}] reference pose = {value}");

				ApplyReferencePoseControl(value);

				if (value)
					TickReferencePose();
				else
					ReapplyActive();
			}
		}

		/// <summary>
		/// Backend hook called when the reference pose is taken and released. A backend whose solver poses the body
		/// on its own (FinalIK solves the spine towards the head target) cuts it here, otherwise the solver would
		/// fold the avatar again on its next late update.
		/// </summary>
		protected virtual void ApplyReferencePoseControl(bool enabled) { }

		/// <summary>
		/// Writes the reference pose on the bones. Called by <see cref="RigManagerModule"/> at the very end of its
		/// late update, after the animation and after the backend wrote its own bones, so the pose wins over both.
		/// </summary>
		public void TickReferencePose() {
			if (!_referencePose)
				return;

			var anchor = Descriptor?.Anchor?.transform;
			if (!anchor)
				return;

			var up    = anchor.up;
			var right = anchor.right;

			AlignSpine(up);

			StraightenLimb(HumanBodyBones.LeftUpperLeg,  HumanBodyBones.LeftLowerLeg,  HumanBodyBones.LeftFoot,  -up);
			StraightenLimb(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, -up);
			StraightenLimb(HumanBodyBones.LeftUpperArm,  HumanBodyBones.LeftLowerArm,  HumanBodyBones.LeftHand,  -right);
			StraightenLimb(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, right);
		}

		/// <summary>
		/// Turns every spine bone so the next one sits directly above it: the body stops bending, whatever the rest
		/// pose of the avatar and the muscle values a humanoid clip leaves on it.
		/// </summary>
		private void AlignSpine(Vector3 up) {
			Transform previous = null;

			foreach (var bone in SpineChain) {
				var current = GetBone(bone);
				if (!current)
					continue;

				if (previous) {
					var segment = current.position - previous.position;
					if (segment.sqrMagnitude > 0.0001f)
						previous.rotation = Quaternion.FromToRotation(segment.normalized, up.normalized) * previous.rotation;
				}

				previous = current;
			}
		}

		/// <summary>
		/// Points a two bone limb along <paramref name="direction"/> and aligns its second segment on the first: the
		/// knee or the elbow stops bending and the limb takes the shape of the reference pose, keeping its length.
		/// </summary>
		private void StraightenLimb(HumanBodyBones root, HumanBodyBones middle, HumanBodyBones end, Vector3 direction) {
			var upper = GetBone(root);
			var mid   = GetBone(middle);
			var tip   = GetBone(end);
			if (!upper || !mid || !tip || direction.sqrMagnitude < 0.0001f)
				return;

			var origin  = upper.position;
			var current = mid.position - origin;
			if (current.sqrMagnitude < 0.0001f)
				return;

			upper.rotation = Quaternion.FromToRotation(current.normalized, direction.normalized) * upper.rotation;

			var pivot   = mid.position;
			var segment = tip.position - pivot;
			var aligned = pivot - origin;
			if (segment.sqrMagnitude < 0.0001f || aligned.sqrMagnitude < 0.0001f)
				return;

			mid.rotation = Quaternion.FromToRotation(segment.normalized, aligned.normalized) * mid.rotation;
		}

		/// <summary>
		/// Called every frame by <see cref="RigManagerModule"/> from its own <c>LateUpdate</c>, i.e. after the
		/// Animator, the animation graph and the backend's solver evaluated.
		/// Backends drive their parts from a solver that runs at the very end (VRIK solves in <c>LateUpdate</c>)
		/// and do not need it; backends whose solver cannot reach the bone (<c>RigBuilderRig</c>, see its override)
		/// write the tracked bones here.
		/// </summary>
		public virtual void LateUpdate() { }

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

		/* ------------------------------------------------------------------ */
		/* External targets                                                     */
		/* ------------------------------------------------------------------ */

		/// <summary>
		/// Binds a rig slot to an external transform: the solver reads it instead of the target
		/// the rig generated, so a tracked part can drive the avatar without a per-frame pose copy.
		/// </summary>
		/// <param name="id">Rig slot, as an <see cref="EntityRig"/> index.</param>
		/// <param name="target">Transform the solver must read, or <c>null</c> to restore the generated one.</param>
		/// <returns><c>false</c> when the rig exposes no such slot.</returns>
		public bool SetTarget(ushort id, Transform target) {
			if (!TryGetPart(id, out _))
				return false;

			var bone = id.ToEntityRig().ToHumanBodyBones();
			if (bone == HumanBodyBones.LastBone)
				return false;

			// The backend owns the binding: it points its solver on the target, and it is the only
			// place the binding lives. Nothing is kept here, so nothing can drift from what the
			// solver actually reads.
			ApplyTarget(bone, target);
			return true;
		}

		/// <summary>
		/// Target the solver reads for a rig slot: the external one when it is bound, the generated
		/// one otherwise.
		/// </summary>
		public Transform GetTarget(ushort id) {
			var bone = id.ToEntityRig().ToHumanBodyBones();
			return bone == HumanBodyBones.LastBone ? null : GetTarget(bone);
		}

		/// <summary>
		/// Target the solver reads for a humanoid bone.
		/// <para>
		/// The backend answers, because it is the one that pointed its solver: the rig never keeps
		/// a second copy of the binding.
		/// </para>
		/// </summary>
		public virtual Transform GetTarget(HumanBodyBones bone)
			=> GetPart(bone);

		/// <summary>
		/// Backend hook: re-point the solver on a new target for a rig slot. Passing <c>null</c>
		/// restores the target the rig generated.
		/// <para>
		/// The default implementation does nothing: a backend whose solver reads the generated
		/// target does not have to be told.
		/// </para>
		/// </summary>
		protected virtual void ApplyTarget(HumanBodyBones bone, Transform target) { }

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
