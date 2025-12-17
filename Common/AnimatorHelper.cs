using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using System.Collections.Generic;

namespace Nox.CCK.Avatars.Common {
	public static class AnimatorHelper {
		public static AnimatorControllerPlayable[] GetControllers(this Animator animator) {
			var controllers = new List<AnimatorControllerPlayable>();
			if (!animator) return controllers.ToArray();
			for (var i = 0; i < animator.playableGraph.GetRootPlayableCount(); i++)
				controllers.AddRange(animator.playableGraph.GetRootPlayable(i).RecursiveController());
			return controllers.ToArray();
		}

		public static AnimatorControllerPlayable[] RecursiveController(this Playable playable) {
			var controllers = new List<AnimatorControllerPlayable>();

			if (!playable.IsValid())
				return controllers.ToArray();

			if (playable.GetPlayableType() == typeof(AnimatorControllerPlayable))
				controllers.Add((AnimatorControllerPlayable)playable);

			for (var i = 0; i < playable.GetInputCount(); i++) {
				var input = playable.GetInput(i);
				controllers.AddRange(RecursiveController(input));
			}

			return controllers.ToArray();
		}
	}
}