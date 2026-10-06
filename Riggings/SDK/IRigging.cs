using System;
using Nox.Avatars.Parameters;
using UnityEngine;

namespace Nox.Avatars.Rigging {
	/// <summary>
	/// A live rigging instance (non-MonoBehaviour) produced by an <see cref="IRiggingBackend"/>.
	/// Backends implement this instead of creating a MonoBehaviour module, so rigs can be
	/// created, swapped and disposed at runtime without touching the avatar's component graph.
	///
	/// The owning <see cref="IRigProvider"/> (a MonoBehaviour) exposes the current rig to the
	/// rest of the system.
	/// </summary>
	public interface IRigging : IDisposable {
		/// <summary>The id of the backend that created this rig (e.g. "rigbuilder", "finalik").</summary>
		string Id { get; }

		/// <summary>Resolves a generated IK target by its <see cref="PlayerRig"/> index.</summary>
		bool TryGetPart(ushort id, out IRigPart part);

		/// <summary>Returns all generated IK targets.</summary>
		IRigPart[] GetParts();

		/// <summary>Resolves a humanoid bone transform on the avatar.</summary>
		Transform GetBone(HumanBodyBones bone);

		/// <summary>Returns the rig's exposed parameters (weights, active flags, targets).</summary>
		IParameter[] GetParameters();

		/// <summary>
		/// Le contrôleur (XR) prend la main sur la <b>racine</b> de l'avatar : le rig arrête de la déplacer
		/// lui-même (locomotion, contrôleur de racine du bassin) et laisse l'appelant la positionner.
		/// Utilisé pendant la calibration full-body, où le jeu pose la racine (cap = tête, XZ selon le réglage)
		/// — sans ça VRIK la déplace d'après la tête et son rattrapage d'angle la translate.
		/// <c>false</c> rend la main au rig.
		/// </summary>
		void SetExternalRootControl(bool external);

		/// <summary>
		/// Effective state of the bone: the avatar's tracking override when one is set
		/// (<see cref="SetTracking"/>), otherwise the controller's own choice (<see cref="SetActive"/>),
		/// falling back to the backend's real state when neither was set.
		/// </summary>
		bool IsActive(HumanBodyBones bone);

		/// <summary>
		/// Controller channel: the player controller (Desktop/XR/Remote) declares whether it drives the bone.
		/// This is the value the avatar leaves alone when it clears its override with
		/// <see cref="SetTracking"/> (<see cref="RiggingTrackingMode.Normal"/>).
		/// </summary>
		void SetActive(HumanBodyBones bone, bool active);

		/// <summary>
		/// Avatar channel read: whether the avatar (e.g. a <c>TrackingControl</c>) currently requests tracking
		/// for the bone, i.e. its override is <see cref="RiggingTrackingMode.Tracking"/>. Returns <c>false</c>
		/// when there is no override (the controller decides — see <see cref="IsActive"/>) or the override is
		/// <see cref="RiggingTrackingMode.Animation"/>.
		/// </summary>
		bool IsTracking(HumanBodyBones bone);

		/// <summary>
		/// Avatar channel mode for the bone: <see cref="RiggingTrackingMode.Animation"/> or
		/// <see cref="RiggingTrackingMode.Tracking"/> when the avatar set an override,
		/// <see cref="RiggingTrackingMode.Normal"/> when it has none (the controller decides).
		/// </summary>
		RiggingTrackingMode GetTracking(HumanBodyBones bone);

		/// <summary>
		/// Avatar channel: a state behavior overrides tracking for the bone independently of the controller.
		/// <see cref="RiggingTrackingMode.Normal"/> clears the override so the controller decides again.
		/// The override wins over <see cref="SetActive"/> while it is set.
		/// </summary>
		void SetTracking(HumanBodyBones bone, RiggingTrackingMode mode);
	}
}
