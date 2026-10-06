using System;
using System.Collections.Generic;
using Nox.Avatars.Rigging;
using Nox.CCK.Players;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.StateMachines {
	/// <summary>Un membre visé par un <see cref="TrackingControl"/> et le mode à lui appliquer.</summary>
	[Serializable]
	public class TrackingControlEntry {
		[Tooltip("Membre visé (PlayerRig, comme les parties du rig).")]
		public PlayerRig part = PlayerRig.Head;

		[Tooltip("Normal : le contrôleur décide. Animation : le membre suit l'animation de l'état (IK coupé). Tracking : il suit l'IK.")]
		public RiggingTrackingMode mode = RiggingTrackingMode.Animation;
	}

	/// <summary>
	/// Équivalent du « Animator Tracking Control » : depuis un état de l'avatar, place chaque membre listé en
	/// suivi (<see cref="RiggingTrackingMode.Tracking"/>) ou en animation (<see cref="RiggingTrackingMode.Animation"/>,
	/// l'IK est coupé et le membre respecte la pose du state machine).
	/// <para>
	/// C'est ce que la couche « Calibration » d'un avatar utilise pour afficher sa T-pose : sans ça, l'IK
	/// réécrit les bones par-dessus l'animation et la pose de calibration n'est jamais visible.
	/// </para>
	/// <para>
	/// Les membres non listés ne sont pas touchés, et rien n'est rétabli à la sortie de l'état : le réglage
	/// reste jusqu'à ce qu'un autre état le change (comme le state behavior de VRChat), le jeu remettant les
	/// membres en suivi à la fin d'une calibration.
	/// </para>
	/// <para>
	/// Le mode <see cref="RiggingTrackingMode.Normal"/> délègue au contraire la décision au contrôleur :
	/// le membre n'est pas modifié, ce qui évite de forcer un suivi absent sur la plateforme courante
	/// (p. ex. ne pas attendre un full-body tracking en desktop) et laisse le XRController gérer le FBT.
	/// </para>
	/// </summary>
	public class TrackingControl : BaseStateMachine {
		[Header("Tracking Control")]
		[Tooltip("Membres à modifier et comment. Par défaut les points de suivi standard, tous en Animation.")]
		public List<TrackingControlEntry> parts = new() {
			new TrackingControlEntry { part = PlayerRig.Head },
			new TrackingControlEntry { part = PlayerRig.LeftHand },
			new TrackingControlEntry { part = PlayerRig.RightHand },
			new TrackingControlEntry { part = PlayerRig.Hips },
			new TrackingControlEntry { part = PlayerRig.LeftFoot },
			new TrackingControlEntry { part = PlayerRig.RightFoot },
		};

		public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
			base.OnStateEnter(animator, stateInfo, layerIndex);
			Apply(animator);
		}

		private void Apply(Animator animator) {
			var rig = GetRig(animator);
			if (rig == null) {
				Logger.LogWarning($"{nameof(TrackingControl)}: aucun rig sur l'avatar, les membres ne sont pas modifiés.", this);
				return;
			}

			if (parts != null)
				foreach (var entry in parts)
					Set(rig, entry);
		}

		/// <summary>Applique le mode de l'entrée au rig, sur le canal « avatar » (indépendant du contrôleur).</summary>
		private static void Set(IRigging rig, TrackingControlEntry entry) {
			if (entry == null)
				return;

			var bone = entry.part.ToHumanBodyBones();
			if (bone == HumanBodyBones.LastBone)
				return;

			rig.SetTracking(bone, entry.mode);
		}

		/// <summary>
		/// Rig de l'avatar : par le descripteur quand le behavior a été initialisé, sinon depuis l'Animator
		/// de l'état — les behaviours des controllers du Playable Module ne passent pas tous par le Setup.
		/// </summary>
		private IRigging GetRig(Animator animator) {
			var anchor = RuntimeAvatar?.Descriptor?.Anchor;
			if (!anchor && animator)
				anchor = animator.gameObject;

			return anchor ? anchor.GetComponentInChildren<IRigProvider>(true)?.GetRig() : null;
		}
	}
}
