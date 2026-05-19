using UnityEngine;

namespace Nox.Avatars.Hand {
	public interface IHandModule : IAvatarModule {
		/// <summary>Hand offset descriptors for this avatar.</summary>
		IHand[] Hands { get; }
	}
}
