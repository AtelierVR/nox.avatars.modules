namespace Nox.Avatars.StateMachines {
	/// <summary>
	/// Poses the standard <c>Pose</c> playable layer exposes, selected by its <c>Pose</c> integer parameter
	/// (the layer key and the parameter share that name: <c>Pose</c>). An avatar only implements the ones it
	/// has: its layer stays in <see cref="Normal"/> for the values it does not handle.
	/// <para/>
	/// These values are part of the avatar contract — the game writes the integer and the avatar's own state
	/// machine decides which tracking it cuts (<c>TrackingControl</c>) and which layers it stops
	/// (<c>PlayableLayerControl</c>). Nothing else about the avatar has to be known to ask for a pose, which is
	/// why this replaces per-feature flags such as a dedicated "T-pose during calibration" setting.
	/// </summary>
	public enum AvatarPose {
		/// <summary>Normal: the avatar is fully driven by the player (tracking, IK, locomotion).</summary>
		Normal = 0,

		/// <summary>Presentation: a showcase pose, tracking cut (avatar menus, photo mode...).</summary>
		Presentation = 1,

		/// <summary>Calibration: the reference pose (T-pose) used to measure the avatar, tracking cut.</summary>
		Calibration = 2,

		/// <summary>Sitting: a seated pose, tracking cut (used when the player sits).</summary>
		Sitting = 3
	}

	/// <summary>Ce que la couche de pose attend et expose (voir <see cref="AvatarPose"/>).</summary>
	public static class AvatarPoseConstants {
		/// <summary>Nom de l'entier que la couche de pose lit (la clé de la couche, elle, vient de <c>PlayableLayerNaming.Pose</c>).</summary>
		public const string ParameterName = "Pose";
	}
}
