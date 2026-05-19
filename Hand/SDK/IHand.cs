using UnityEngine;

namespace Nox.Avatars.Hand {
	public interface IHand {
		/// <summary>Which hand this entry describes.</summary>
		HandType Type { get; }

		/// <summary>Position offset relative to the tracker transform.</summary>
		Transform Anchor { get; }

		/// <summary>
		/// Position offset for the palm relative to the tracker transform.
		/// </summary>
		Transform Palm { get; }

		/// <summary>
		/// Local position offset from Anchor to the hand pivot point (the point that should
		/// align with the tracker/controller position).
		/// </summary>
		Vector3 PositionOffset { get; }

		/// <summary>
		/// Local rotation offset from Anchor to the hand pivot orientation (the rotation that
		/// should match the tracker/controller rotation).
		/// </summary>
		Quaternion RotationOffset { get; }

		/// <summary>
		/// Finger offset descriptors for this hand.
		/// The order of fingers should be consistent across all hands (e.g., thumb, index, middle, ring, pinky).
		/// </summary>
		IFinger[] Fingers { get; }
	}
}