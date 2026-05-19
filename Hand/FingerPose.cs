using System;
using UnityEngine;
namespace Nox.Avatars.Hand {
	[Serializable]
	public class FingerPose : IPose {
		public FingerCurl curl = FingerCurl.TPose;

		public Quaternion[] values = {
			Quaternion.identity,
			Quaternion.identity,
			Quaternion.identity,
			Quaternion.identity
		};

		public FingerCurl Curl
			=> curl;

		public Quaternion[] Values
			=> values;
	}
}