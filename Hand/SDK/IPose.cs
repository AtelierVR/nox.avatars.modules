using UnityEngine;

namespace Nox.Avatars.Hand {
	public interface IPose {
		public FingerCurl Curl { get; }
		public Quaternion[] Values { get; }
	}
}