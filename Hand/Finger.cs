using System;
using System.Linq;
using Nox.Avatars.Hand;
using UnityEditor;
using UnityEngine;
using Gizmos = Nox.CCK.Development.Gizmos;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.Hand {
	public class Finger : MonoBehaviour, IFinger {
		public FingerType type = FingerType.None;

		[Header("Joints")]
		public Transform proximal;
		public Transform intermediate;
		public Transform distal;

		[Header("Tip")]
		[Tooltip("The target transform that the user can interact with ui")]
		public Transform tip;
		public float tipRadius = 0.01f;

		[Header("Pose")]
		[Tooltip("The differents levels of curl for this finger, from 0 (fully extended) to 1 (fully curled).")]
		public FingerPose[] poses = Enum.GetValues(typeof(FingerCurl))
			.Cast<FingerCurl>()
			.Select(c => new FingerPose { curl = c })
			.ToArray();

		private void OnDrawGizmos() {
			if (!this.IsValid(out var error)) {
				Gizmos.color = Color.red;
				Gizmos.DrawLabel(transform.position, "Invalid Finger: " + error.Message);
				return;
			}

			Gizmos.color = Color.cyan;
			var p = Proximal ? Proximal.position : transform.position;
			var i = Intermediate ? Intermediate.position : transform.position;
			var d = Distal ? Distal.position : transform.position;
			var t = Tip ? Tip.position : transform.position;

			Gizmos.DrawLine(p, i);
			Gizmos.DrawLine(i, d);
			Gizmos.DrawLine(d, t);

			Gizmos.DrawWireSphere(t, TipRadius);
		}

		public FingerType Type
			=> type;

		public Transform Proximal
			=> proximal;

		public Transform Intermediate
			=> intermediate;

		public Transform Distal
			=> distal;

		public Transform Tip
			=> tip;

		public float TipRadius
			=> tipRadius;

		public IPose[] Poses
			=> poses.ToArray<IPose>();


		#if UNITY_EDITOR

		public void OnValidate() {
			// add missing poses for any new FingerCurl values
			foreach (var curl in Enum.GetValues(typeof(FingerCurl)).Cast<FingerCurl>()) {
				if (poses.Any(p => p.curl == curl))
					continue;
				Array.Resize(ref poses, poses.Length + 1);
				poses[^1] = new FingerPose { curl = curl };
			}
		}

		public void SavePose(FingerCurl curl, bool undo = true) {
			var pose = poses.FirstOrDefault(p => p.curl == curl);
			if (pose == null)
				throw new InvalidOperationException("No pose found for Opened curl.");

			if (undo)
				Undo.RecordObject(this, $"Save {curl} Poses");

			pose.values = new[] {
				proximal.localRotation,
				intermediate.localRotation,
				distal.localRotation,
				tip.localRotation,
			};

			if (undo) {
				EditorUtility.SetDirty(this);
				Logger.Log($"Saved {curl} pose for {type} finger.", this);
			}
		}

		public void SetPose(FingerCurl curl, bool undo = true) {
			var pose = poses.FirstOrDefault(p => p.curl == curl);
			if (pose == null)
				throw new InvalidOperationException("No pose found for Opened curl.");
			if (pose.Values is not { Length: 4 })
				throw new InvalidOperationException("Pose values must be an array of 4 rotations.");
			if (undo)
				Undo.RecordObject(this, $"Set {curl} Poses");

			proximal.localRotation     = pose.Values[0];
			intermediate.localRotation = pose.Values[1];
			distal.localRotation       = pose.Values[2];
			tip.localRotation          = pose.Values[3];

			if (undo)
				EditorUtility.SetDirty(this);
		}

		[ContextMenu("Invert Pose")]
		public void InvertPose() {
		}

		#endif
	}
}