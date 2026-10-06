namespace Nox.Avatars.Rigging {
	/// <summary>
	/// Avatar-side tracking choice for a member, applied in parallel with the controller's own choice
	/// (<see cref="IRigging.SetActive"/>). The avatar channel wins over the controller while it is set;
	/// <see cref="Normal"/> clears it so the controller decides again.
	/// </summary>
	public enum RiggingTrackingMode {
		/// <summary>
		/// No override: the player controller (Desktop/XR/Remote) decides. On desktop the members without
		/// tracking stay animated (no full-body tracking will ever arrive); on XR the <c>XRController</c>
		/// enables/disables FBT from the present trackers.
		/// </summary>
		Normal = 0,

		/// <summary>The member follows the avatar's animation (IK cut for it).</summary>
		Animation = 1,

		/// <summary>The member follows the IK (trackers, controllers, head).</summary>
		Tracking = 2
	}
}
