using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Hand;
using UnityEngine;

namespace Nox.CCK.Avatars.Hand {

	public class HandAvatarModule : MonoBehaviour, IHandModule {
		[Tooltip("Hand offset descriptors. Add one entry per hand you want to configure.")]
		public Hand[] hands = Array.Empty<Hand>();

		public int GetPriority()
			=> 0;

		public UniTask<bool> Setup(IRuntimeAvatar runtimeAvatar)
			=> UniTask.FromResult(true);

		public void Dispose() { }

		public IHand[] Hands
			=> hands.ToArray<IHand>();

		/// <summary>
		/// Ensures a HandAvatarModule is present on the descriptor.
		/// If none exists, a default one is added silently (zero offsets).
		/// Returns false only when multiple conflicting modules are found.
		/// </summary>
		public static bool Check(IAvatarDescriptor descriptor) {
			var modules = descriptor.GetModules<HandAvatarModule>();
			switch (modules.Length) {
				case 0:
					descriptor.Anchor.AddComponent<HandAvatarModule>();
					return true;
				case 1:
					return true;
				default:
					Nox.CCK.Utils.Logger.LogError(
						"Multiple HandAvatarModule components found. Keep only one.",
						descriptor.Anchor);
					return false;
			}
		}
	}
}