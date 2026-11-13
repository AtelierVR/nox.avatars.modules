using System;
using System.Collections.Generic;
using System.Linq;
using Nox.Avatars;
using Nox.Avatars.Rigging;
using Nox.CCK.Entities;
using UnityEngine;

namespace Nox.CCK.Avatars.StateMachines {
	/// <summary>Un membre visé par un <see cref="TrackingControl"/> et le mode à lui appliquer.</summary>
	[Serializable]
	public class TrackingControlEntry {
		[Tooltip("Membre visé (EntityRig, comme les parties du rig).")]
		public EntityRig part = EntityRig.Head;

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
	/// Les membres non listés ne sont pas touchés. Le réglage s'applique tant que l'état est actif : à la sortie,
	/// les membres que cet état a réglés et que personne d'autre n'a repris depuis repassent en
	/// <see cref="RiggingTrackingMode.Normal"/>, le contrôleur reprenant la main (sinon une pose de calibration
	/// laisserait un membre en <c>Animation</c> pour toujours, et le rig ne le piloterait plus jamais).
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
			new TrackingControlEntry { part = EntityRig.Head },
			new TrackingControlEntry { part = EntityRig.LeftHand },
			new TrackingControlEntry { part = EntityRig.RightHand },
			new TrackingControlEntry { part = EntityRig.Hips },
			new TrackingControlEntry { part = EntityRig.LeftFoot },
			new TrackingControlEntry { part = EntityRig.RightFoot },
		};

		/// <summary>
		/// Rig provider listening to the avatar's rig-change event. Kept only as the subscription handle of
		/// <see cref="IRigProvider.OnRigChanged"/>; the rig itself is never cached.
		/// </summary>
		private IRigProvider _watching;

		/// <summary>
		/// Whether the state owning this behaviour is running. The entries are written only while it is: a behaviour
		/// that is set up before any of its states ran must not touch the rig, or the rig would lose the tracking of
		/// the limbs it drives (the default entries hand every member back to the animation).
		/// </summary>
		private bool _active;

		public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
			base.OnStateEnter(animator, stateInfo, layerIndex);

			_active = true;
			Watch();
			Apply();
		}

		public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
			base.OnStateExit(animator, stateInfo, layerIndex);

			_active = false;
			Restore();
		}

		/// <summary>
		/// The rig provider is a module: it can be registered or replaced while the avatar lives, and the setup runs
		/// again when the module set of the avatar changes. The behaviour then listens to the new provider, and
		/// rewrites the entries on it only when its state is the one running.
		/// </summary>
		public override bool Setup(IRuntimeAvatar runtime) {
			if (!base.Setup(runtime))
				return false;

			Watch();
			if (_active)
				Apply();

			return true;
		}

		private void Apply() {
			var rig = Rig;
			if (rig == null)
				return;

			if (parts == null)
				return;

			foreach (var entry in parts)
				Set(rig, entry);
		}

		/// <summary>
		/// Hands the members back to the controller when the state leaves, for the entries it still owns: a member
		/// another state set in the meantime is left alone, and one the state did not touch is not restored at all.
		/// <para>
		/// Nothing else clears the avatar channel: a state that leaves a member in <c>Animation</c> (a calibration
		/// pose) would otherwise keep the rig from ever driving it again.
		/// </para>
		/// </summary>
		private void Restore() {
			var rig = Rig;
			if (rig == null)
				return;

			if (parts == null)
				return;

			foreach (var entry in parts) {
				if (entry == null)
					continue;

				var bone = entry.part.ToHumanBodyBones();
				if (bone == HumanBodyBones.LastBone)
					continue;

				if (rig.GetTracking(bone) != entry.mode)
					continue;

				rig.SetTracking(bone, RiggingTrackingMode.Normal);
			}
		}

		/// <summary>
		/// Listens to the rig-change event of the current rig provider (backend hot-swap, rig disposed). The
		/// provider is resolved through the descriptor, and a provider already listened to is left untouched, so
		/// the subscription survives a module event that did not touch the provider.
		/// </summary>
		private void Watch() {
			var provider = Provider;
			if (ReferenceEquals(provider, _watching))
				return;

			_watching?.OnRigChanged.RemoveListener(OnRigChanged);
			_watching = provider;
			_watching?.OnRigChanged.AddListener(OnRigChanged);
		}

		/// <summary>
		/// A new rig is exposed by the provider (or the current one was disposed): the entries are written again on
		/// it, but only when their state is the one running.
		/// </summary>
		private void OnRigChanged(IRigging rig) {
			if (_active)
				Apply();
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

		/// <summary>Rig provider of the avatar, or <c>null</c> while the avatar exposes none.</summary>
		private IRigProvider Provider
			=> Descriptor
				?.GetModules<IRigProvider>()
				.FirstOrDefault();

		/// <summary>
		/// Rig currently exposed by the avatar's rig provider module, or <c>null</c> while there is none. It is read
		/// from the avatar each time: the module list changes are announced by the descriptor, and the rig itself by
		/// <see cref="IRigProvider.OnRigChanged"/>.
		/// </summary>
		private IRigging Rig
			=> Provider?.Rig;
	}
}
