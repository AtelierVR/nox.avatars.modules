using System;
using System.Linq;
using Nox.Avatars.Hand;

namespace Nox.CCK.Avatars.Hand {
	public static class HandExtensions {
		/// <summary>
		/// Validate that a hand has a valid type and fingers,
		/// and that all fingers are distinct.
		/// </summary>
		/// <param name="hand"></param>
		/// <returns></returns>
		public static bool IsValid(this IHand hand, out Exception error) {
			error = null;

			if (hand == null) {
				error = new ArgumentNullException(nameof(hand));
				return false;
			}

			if (hand.Type == HandType.None) {
				error = new ArgumentException("Hand type cannot be None.", nameof(hand));
				return false;
			}

			if (!hand.Anchor) {
				error = new ArgumentException("Hand must have an anchor transform.", nameof(hand));
				return false;
			}

			if (hand.Palm && !hand.Palm.IsChildOf(hand.Anchor)) {
				error = new ArgumentException("Palm transform must be a child of the anchor transform.", nameof(hand));
				return false;
			}

			if (hand.Fingers == null) {
				error = new ArgumentException("Hand must have a fingers array.", nameof(hand));
				return false;
			}

			Exception fingerError = null;
			if (!hand.Fingers.All(f => f != null && f.IsValid(out fingerError))) {
				error = new ArgumentException("All fingers must be valid. " + string.Join("; ", hand.Fingers.Select((f, i) => f == null ? $"Finger {i} is null." : fingerError != null ? $"Finger {i} is invalid: {fingerError.Message}" : "")), nameof(hand));
				return false;
			}

			if (hand.Fingers.Select(f => f.Type).Distinct().Count() != hand.Fingers.Length) {
				error = new ArgumentException("All fingers must have distinct types.", nameof(hand));
				return false;
			}

			error = null;
			return true;
		}

		/// <summary>
		/// Validate that a finger has a valid type and joint ranges,
		/// and that optional joints are consistent (e.g. no Intermediate without Proximal).
		/// </summary>
		/// <param name="finger"></param>
		/// <returns></returns>
		public static bool IsValid(this IFinger finger, out Exception error) {
			error = null;

			if (finger == null) {
				error = new ArgumentNullException(nameof(finger));
				return false;
			}

			if (finger.Type == FingerType.None) {
				error = new ArgumentException("Finger type cannot be None.", nameof(finger));
				return false;
			}

			if (!finger.Proximal) {
				error = new ArgumentException("Proximal joint must be active in the hierarchy.", nameof(finger));
				return false;
			}

			if (finger.Intermediate && !finger.Intermediate.IsChildOf(finger.Proximal)) {
				error = new ArgumentException("Intermediate joint must be a child of the proximal joint.", nameof(finger));
				return false;
			}

			if (finger.Distal && (!finger.Intermediate || !finger.Distal.IsChildOf(finger.Intermediate))) {
				error = new ArgumentException("Distal joint must be a child of the intermediate joint.", nameof(finger));
				return false;
			}

			if (finger.TipRadius < 0) {
				error = new ArgumentException("Tip radius must be non-negative.", nameof(finger));
				return false;
			}

			if (finger.Poses == null || finger.Poses.Length != Enum.GetValues(typeof(FingerCurl)).Length) {
				error = new ArgumentException($"Finger must have a pose for each FingerCurl value ({Enum.GetValues(typeof(FingerCurl)).Length} poses).", nameof(finger));
				return false;
			}

			Exception poseError = null;
			if (!finger.Poses.All(p => p != null && p.IsValid(out poseError))) {
				error = new ArgumentException("All finger poses must be valid. " + string.Join("; ", finger.Poses.Select((p, i) => p == null ? $"Pose {i} is null." : poseError != null ? $"Pose {i} is invalid: {poseError.Message}" : "")), nameof(finger));
				return false;
			}

			if (finger.Poses.Select(p => p.Curl).Distinct().Count() != finger.Poses.Length) {
				error = new ArgumentException("All finger poses must have distinct curl values.", nameof(finger));
				return false;
			}

			error = null;
			return true;
		}

		/// <summary>
		/// Validate that a pose has 4 values (e.g., for opened, closed, pinch opened and pinch closed)
		/// </summary>
		/// <param name="finger"></param>
		/// <returns></returns>
		public static bool IsValid(this IPose finger, out Exception error) {
			error = null;

			if (finger == null) {
				error = new ArgumentNullException(nameof(finger));
				return false;
			}

			if (finger.Values is not { Length: 4 }) {
				error = new ArgumentException("Pose must have exactly 4 values.", nameof(finger));
				return false;
			}

			error = null;
			return true;
		}
	}
}