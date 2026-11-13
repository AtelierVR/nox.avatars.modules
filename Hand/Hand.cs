using System;
using System.Linq;
using Nox.Avatars.Hand;
using UnityEditor;
using UnityEngine;
using Gizmos = Nox.CCK.Development.Gizmos;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.Hand {
	public class Hand : MonoBehaviour, IHand {
		public HandType type;

		[Tooltip("The target transform that the hand should follow (e.g., a hand tracker).")]
		public Transform anchor;

		[Tooltip("The transform representing the palm of the hand. This is used for orienting the hand and fingers correctly.")]
		public Transform palm;

		[Tooltip("Local position offset from Anchor to the hand pivot (the point that should align with the tracker/controller position).")]
		public Vector3 pivotPositionOffset;

		[Tooltip("Local rotation offset from Anchor to the hand pivot orientation (the rotation that should match the tracker/controller rotation).")]
		public Quaternion pivotRotationOffset = Quaternion.identity;

		public Finger[] fingers = Array.Empty<Finger>();

		[Tooltip("The NearFar transform for this hand (used for near/far interactions).")]
		public Transform nearFar;

		HandType IHand.Type
			=> type;

		IFinger[] IHand.Fingers
			=> fingers.ToArray<IFinger>();

		public Transform Anchor
			=> anchor ?? transform;

		public Transform Palm
			=> palm ?? Anchor;

		public Transform NearFar
			=> nearFar;

		Vector3 IHand.PositionOffset => pivotPositionOffset;
		Quaternion IHand.RotationOffset => pivotRotationOffset;

		/// <summary>Always visible part of the overlay: the discs, arrows and finger links.</summary>
		private void OnDrawGizmos()
			=> DrawOverlay(false);

		/// <summary>Labels of the overlay, drawn only while the hand is selected so the texts do not clutter the scene.</summary>
		private void OnDrawGizmosSelected()
			=> DrawOverlay(true);

		/// <param name="labels">Draws the texts: only when the hand is selected.</param>
		private void DrawOverlay(bool labels) {
			if (!this.IsValid(out var error)) {
				if (labels) {
					Gizmos.Color = Color.red;
					Gizmos.DrawLabel(transform.position, "Invalid Hand: " + error.Message);
				}

				return;
			}

			if (!Palm) {
				if (labels) {
					Gizmos.Color = Color.orange;
					Gizmos.DrawLabel(transform.position, "Hand has no palm transform.");
				}
			} else {
				Gizmos.Color = Color.darkGreen;
				Gizmos.DrawWireDisc(Palm.position, Palm.rotation * Vector3.forward, 0.05f);
				Gizmos.DrawArrow(Palm.position, Palm.rotation * Vector3.forward, 0.05f);
			}

			Gizmos.Color = Color.cyan;
			foreach (var f in fingers) {
				Exception fingerError = null;
				if (!f || !f.IsValid(out fingerError)) {
					if (labels) {
						var pos = f ? f.transform.position : transform.position;
						Gizmos.Color = Color.red;
						Gizmos.DrawLabel(pos, "Invalid Finger: " + (f ? fingerError!.Message : "Finger is null."));
					}

					continue;
				}

				var proximal = f.Proximal
					? f.Proximal.position
					: f.transform.position;
				Gizmos.DrawLine(Anchor.position, proximal);
			}

			// Draw pivot offset
			if (Anchor) {
				var pivotWorldPos = Anchor.TransformPoint(pivotPositionOffset);
				var pivotWorldRot = Anchor.rotation * pivotRotationOffset;
				// Line from anchor to pivot
				Gizmos.Color = Color.yellow;
				Gizmos.DrawLine(Anchor.position, pivotWorldPos);
				// Disc at pivot
				Gizmos.DrawWireDisc(pivotWorldPos, pivotWorldRot * Vector3.up, 0.015f);
				// Pivot orientation axes
				Gizmos.Color = Color.blue;
				Gizmos.DrawArrow(pivotWorldPos, pivotWorldRot * Vector3.forward, 0.05f);
				Gizmos.Color = Color.red;
				Gizmos.DrawArrow(pivotWorldPos, pivotWorldRot * Vector3.right, 0.04f);
				Gizmos.Color = Color.green;
				Gizmos.DrawArrow(pivotWorldPos, pivotWorldRot * Vector3.up, 0.04f);

				if (labels) {
					Gizmos.Color = Color.yellow;
					Gizmos.DrawLabel(pivotWorldPos, "Pivot");
				}
			}
		}

		#if UNITY_EDITOR

		[ContextMenu("Print Pivot")]
		public void PrintPivot()
		{
			var pivotWorldPos = Anchor.TransformPoint(pivotPositionOffset);
			var pivotWorldRot = Anchor.rotation * pivotRotationOffset;
			Logger.Log($"Pivot {name}: {pivotWorldPos} {pivotWorldRot}");
		}

		[ContextMenu("Save Opened Poses")]
		public void SaveOpenedPoses()
			=> SavePoses(FingerCurl.Opened, true);

		[ContextMenu("Save Closed Poses")]
		public void SaveClosedPoses()
			=> SavePoses(FingerCurl.Closed, true);

		[ContextMenu("Save Pinched Opened Poses")]
		public void SavePinchedOpenedPoses()
			=> SavePoses(FingerCurl.PinchedOpened, true);

		[ContextMenu("Save Pinched Closed Poses")]
		public void SavePinchedClosedPoses()
			=> SavePoses(FingerCurl.PinchedClosed, true);

		[ContextMenu("Save TPose Poses")]
		public void SaveTPosePoses()
			=> SavePoses(FingerCurl.TPose, true);

		public void SavePoses(FingerCurl curl, bool manual = false) {
			if (manual && !Logger.OpenDialog("Save Poses", $"Are you sure you want to save the current finger poses as {curl}? This will overwrite any existing poses for this curl.", "Save", "Cancel")) {
				Logger.LogWarning($"Save {curl} poses cancelled by user.", this);
				return;
			}

			Undo.RecordObject(this, $"Save {curl} Poses");
			foreach (var f in fingers)
				f.SavePose(curl, false);
			EditorUtility.SetDirty(this);

			Logger.Log($"Saved {curl} poses for all fingers.", this);
		}

		[ContextMenu("Open Hand")]
		public void OpenHand()
			=> SetPoses(FingerCurl.Opened);

		[ContextMenu("Close Hand")]
		public void CloseHand()
			=> SetPoses(FingerCurl.Closed);

		[ContextMenu("Pinch Open Hand")]
		public void PinchOpenHand()
			=> SetPoses(FingerCurl.PinchedOpened);

		[ContextMenu("Pinch Closed Hand")]
		public void PinchClosedHand()
			=> SetPoses(FingerCurl.PinchedClosed);

		[ContextMenu("Goto TPose Hand")]
		public void GotoTPoseHand()
			=> SetPoses(FingerCurl.TPose);

		public void SetPoses(FingerCurl curl) {
			Undo.RecordObject(this, $"Set {curl} Poses");
			foreach (var f in fingers)
				f.SetPose(curl, false);
			EditorUtility.SetDirty(this);
		}

		#endif
	}
}
