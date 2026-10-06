using System;
using UnityEngine;

namespace Nox.CCK.Avatars.Playable {
	/// <summary>
	/// How the key (name) used to address a playable layer is resolved.
	/// <para/>
	/// A "playable layer" is one Animator Controller blended by the
	/// <see cref="PlayableAvatarModule"/> layer mixer. Each layer has a weight: it only
	/// contributes to the final pose while its weight is above zero, so layers can be
	/// started/stopped (or blended in and out) independently.
	/// </summary>
	public enum PlayableLayerNaming {
		/// <summary>
		/// Uses the Animator Controller asset name as the key (no explicit naming).
		/// Pick this when the asset already has a clear name, or when you don't need
		/// to address the layer by name at all.
		/// </summary>
		Controller,

		/// <summary>
		/// Base layer: the avatar's default/rest pose and the foundation every other
		/// layer blends on top of. Should always be present as layer index 0 and kept
		/// at full weight, since disabling it leaves the avatar with no base pose.
		/// </summary>
		Default,

		/// <summary>
		/// Locomotion layer: animates the avatar from player movement (idle, walk, run,
		/// jump, fall, crouch...). Blends onto the base pose and is normally always active,
		/// except when another layer needs to take over the body (e.g. calibration).
		/// </summary>
		Locomotion,

		/// <summary>
		/// Calibration layer: poses used to (re)align the rig, such as a T-Pose or A-Pose.
		/// Starting it overrides the limbs to a known reference pose so the IK/full-body
		/// tracking can be recalibrated; stopping it (blending back to 0) returns control
		/// to locomotion. Prefer the <see cref="Pose"/> layer, which groups every whole-body
		/// pose (calibration included) behind a single integer; this role is kept for avatars
		/// reduced to a standalone T-Pose layer.
		/// </summary>
		Calibration,

		/// <summary>
		/// Pose layer: the avatar's whole-body poses (normal, presentation, calibration, sitting...)
		/// selected by an integer parameter — see <c>AvatarPose</c> in
		/// <c>Nox.Avatars.StateMachines</c>. Only one such layer should exist per avatar and it replaces
		/// the dedicated calibration layer: its "normal" state does nothing, so the game can ask for a
		/// calibration (T-pose) pose at any time by setting the parameter, and the layer itself decides
		/// which tracking it cuts (<c>TrackingControl</c>) and which layers it stops
		/// (<c>PlayableLayerControl</c>).
		/// </summary>
		Pose,

		/// <summary>
		/// FX layer: non-movement show such as face expressions (blinking, visemes),
		/// props, toggles and particle effects. Typically always active as an additive
		/// overlay independent from locomotion.
		/// </summary>
		FX,

		/// <summary>
		/// Uses a free-form custom name as the key (see <see cref="PlayableLayer.custom"/>).
		/// Pick this when you need a project-specific key that doesn't match the asset name
		/// or any of the standard layers above.
		/// </summary>
		Custom
	}

	/// <summary>
	/// A single playable layer: an Animator Controller plus the key used to address it by name.
	/// In the inspector you can either pick a standard name, type a custom one, or pick
	/// <see cref="PlayableLayerNaming.Controller"/> to fall back to the controller asset name.
	/// </summary>
	[Serializable]
	public class PlayableLayer {
		[Tooltip("Animator Controller used as a playable layer.")]
		public RuntimeAnimatorController controller;

		[Tooltip("How the layer key is resolved: the controller asset name, a standard name (Default/Locomotion/Calibration/FX) or a custom name.")]
		public PlayableLayerNaming naming = PlayableLayerNaming.Controller;

		[Tooltip("Custom key used when Naming is Custom.")]
		public string custom;

		/// <summary>
		/// Resolved key used to address this layer by name.
		/// Falls back to the controller asset name when empty (or when there is no controller).
		/// </summary>
		public string Key {
			get {
				var key = naming switch {
					PlayableLayerNaming.Controller => controller ? controller.name : string.Empty,
					PlayableLayerNaming.Custom     => custom ?? string.Empty,
					_                              => naming.ToString()
				};

				if (string.IsNullOrWhiteSpace(key))
					key = controller ? controller.name : string.Empty;

				return key ?? string.Empty;
			}
		}

		/// <summary>Whether this layer resolves to a usable (non-empty) key.</summary>
		public bool HasKey
			=> !string.IsNullOrEmpty(Key);
	}
}
