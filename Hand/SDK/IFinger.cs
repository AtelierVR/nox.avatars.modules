using UnityEngine;
namespace Nox.Avatars.Hand {
	public interface IFinger {
		/// <summary>
		/// The type of the finger (e.g., Thumb, Index, etc.).
		/// </summary>
		FingerType Type { get; }

		public Transform Proximal { get; }
		public Transform Intermediate { get; }
		public Transform Distal { get; }

		public Transform Tip { get; }
		public float TipRadius { get; }

		public IPose[] Poses { get; }
	}
}