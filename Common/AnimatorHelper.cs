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

		/// <summary>
		/// Controller playables actually mixed by the graph, i.e. reached through the input ports of a root
		/// playable. The Animator's own controller (its <c>runtimeAnimatorController</c>) is a root nothing
		/// feeds, so it is excluded: writing a parameter to it would leave the layers built by the playable
		/// module untouched. Duplicates (a controller reachable from several roots) are kept once.
		/// </summary>
		public static AnimatorControllerPlayable[] GetLayerControllers(this Animator animator) {
			var controllers = new List<AnimatorControllerPlayable>();
			if (!animator) return controllers.ToArray();

			for (var i = 0; i < animator.playableGraph.GetRootPlayableCount(); i++) {
				var root = animator.playableGraph.GetRootPlayable(i);
				for (var input = 0; input < root.GetInputCount(); input++)
					AddControllers(root.GetInput(input), controllers);
			}

			// No mixed layer at all (a graph made of bare roots): fall back to every controller found.
			if (controllers.Count == 0)
				controllers.AddRange(animator.GetControllers());

			return controllers.ToArray();
		}

		private static void AddControllers(Playable playable, List<AnimatorControllerPlayable> controllers) {
			if (!playable.IsValid())
				return;

			if (playable.GetPlayableType() == typeof(AnimatorControllerPlayable)) {
				var controller = (AnimatorControllerPlayable)playable;
				if (!controllers.Contains(controller))
					controllers.Add(controller);
			}

			for (var i = 0; i < playable.GetInputCount(); i++)
				AddControllers(playable.GetInput(i), controllers);
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
