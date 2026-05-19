#if UNITY_EDITOR
using AvatarHand = Nox.CCK.Avatars.Hand.Hand;
using AvatarFinger = Nox.CCK.Avatars.Hand.Finger;
using UnityEditor;
using UnityEngine;

namespace Nox.CCK.Avatars.Modules.Editor {
	[CustomEditor(typeof(AvatarHand))]
	public class HandEditor : UnityEditor.Editor {
		private bool _editingGrip;

		private AvatarHand hand => target as AvatarHand;

		public override void OnInspectorGUI() {
			DrawDefaultInspector();

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Pivot Offset Tools", EditorStyles.boldLabel);

			using (new EditorGUILayout.HorizontalScope()) {
				if (GUILayout.Button("Detect")) {
					DetectPivotOffset();
				}

				var editLabel = _editingGrip ? "Done" : "Edit Pivot";
				if (GUILayout.Button(editLabel)) {
					_editingGrip = !_editingGrip;
					SceneView.RepaintAll();
				}
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Colliders", EditorStyles.boldLabel);
			if (GUILayout.Button("Setup Colliders")) {
				SetupColliders();
			}
		}

		private void OnSceneGUI() {
			if (!_editingGrip || !hand.Anchor) return;

			var anchor = hand.Anchor;
			var worldPos = anchor.TransformPoint(hand.pivotPositionOffset);
			var worldRot = anchor.rotation * hand.pivotRotationOffset;

			EditorGUI.BeginChangeCheck();

			worldPos = Handles.PositionHandle(worldPos, worldRot);
			worldRot = Handles.RotationHandle(worldRot, worldPos);

			if (EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(hand, "Edit Hand Grip Offset");
				hand.pivotPositionOffset = anchor.InverseTransformPoint(worldPos);
				hand.pivotRotationOffset = Quaternion.Inverse(anchor.rotation) * worldRot;
				EditorUtility.SetDirty(hand);
			}
		}

		private void DetectPivotOffset() {
			if (!hand.palm) {
				Debug.LogWarning("[HandEditor] Cannot detect pivot offset: no palm transform assigned.", hand);
				return;
			}

			// Collect proximal positions in anchor-local space
			var anchor     = hand.Anchor;
			var palmLocal  = anchor.InverseTransformPoint(hand.palm.position);

			var proximalSum   = Vector3.zero;
			int proximalCount = 0;
			foreach (var f in hand.fingers) {
				if (f == null || !f.proximal) continue;
				if (f.type == Nox.Avatars.Hand.FingerType.Thumb) continue;
				proximalSum += anchor.InverseTransformPoint(f.proximal.position);
				proximalCount++;
			}

			Undo.RecordObject(hand, "Detect Hand Pivot Offset");

			if (proximalCount == 0) {
				// No fingers: fall back to palm
				hand.pivotPositionOffset = palmLocal;
			} else {
				var proximalAvg = proximalSum / proximalCount;
				// Midpoint between proximal centroid and palm in all axes
				hand.pivotPositionOffset = (proximalAvg + palmLocal) * 0.5f;
			}

			hand.pivotRotationOffset = Quaternion.Inverse(anchor.rotation) * hand.palm.rotation;
			EditorUtility.SetDirty(hand);
		}

		private void SetupColliders() {
			Undo.SetCurrentGroupName("Setup Hand Colliders");
			var group = Undo.GetCurrentGroup();

			var anchor = hand.Anchor;

			// Palm collider — skip if one already exists
			if (!anchor.GetComponent<Collider>()) {
				var box = Undo.AddComponent<BoxCollider>(anchor.gameObject);

				if (hand.fingers.Length == 0) {
					box.size   = new Vector3(0.08f, 0.02f, 0.08f);
					box.center = Vector3.zero;
				} else {
					var bounds = new Bounds(Vector3.zero, Vector3.zero);
					float avgTipRadius = 0f;
					int count = 0;
					foreach (var f in hand.fingers) {
						if (f == null || !f.proximal) continue;
						bounds.Encapsulate(anchor.InverseTransformPoint(f.proximal.position));
						avgTipRadius += f.tipRadius;
						count++;
					}

					if (count > 0) avgTipRadius /= count;
					float thickness = Mathf.Max(avgTipRadius * 3f, 0.015f);
					var size = bounds.size;
					size.x = Mathf.Max(size.x, 0.02f);
					size.y = Mathf.Max(size.y, 0.02f);
					size.z = Mathf.Max(size.z, 0.02f);

					// Collapse the thinnest axis to form a palm slab
					if (size.x <= size.y && size.x <= size.z)      size.x = thickness;
					else if (size.y <= size.z)                      size.y = thickness;
					else                                             size.z = thickness;

					box.size   = size;
					box.center = bounds.center;
				}
			}

			// Finger colliders
			foreach (var finger in hand.fingers) {
				if (finger == null) continue;
				SetupFingerColliders(finger);
			}

			Undo.CollapseUndoOperations(group);
		}

		private static void SetupFingerColliders(AvatarFinger finger) {
			Transform[] bones = { finger.proximal, finger.intermediate, finger.distal };
			for (int i = 0; i < bones.Length; i++) {
				if (!bones[i]) continue;
				if (bones[i].GetComponent<Collider>()) continue;

				var next = (i + 1 < bones.Length && bones[i + 1]) ? bones[i + 1] : null;
				float length = next ? Vector3.Distance(bones[i].position, next.position) : 0.02f;
				if (length < 0.005f) length = 0.02f;

				float radius = finger.tipRadius > 0 ? finger.tipRadius : 0.008f;

				var capsule       = Undo.AddComponent<CapsuleCollider>(bones[i].gameObject);
				capsule.radius    = radius;
				capsule.height    = length;
				capsule.direction = 2; // Z-axis
				capsule.center    = Vector3.forward * (length * 0.5f);
			}
		}
	}
}
#endif
