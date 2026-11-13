using System;
using System.Collections.Generic;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Nox.CCK.Avatars.Parameters {
	/// <summary>
	/// A parameter backed by controller playables. One key of the <c>PlayableAvatarModule</c> may own
	/// several controllers (e.g. two FX controllers) and each <see cref="AnimatorControllerPlayable"/> keeps
	/// its own parameter storage, so the value is written into every controller declaring the parameter.
	/// <para/>
	/// The playables are checked for validity on every access: the playable module rebuilds its layers (and
	/// destroys the previous controllers) when the avatar is set up again, and a disposed handle throws
	/// instead of simply reading stale data.
	/// </summary>
	public class PlayableBaseParameter : BaseParameter {
		internal AnimatorControllerPlayable[] Controllers;

		override protected void SetFloat(float value) {
			foreach (var controller in ValidControllers())
				controller.SetFloat(AnimatorHash, value);
		}

		override protected void SetInteger(int value) {
			foreach (var controller in ValidControllers())
				controller.SetInteger(AnimatorHash, value);
		}

		override protected void SetBool(bool value) {
			foreach (var controller in ValidControllers())
				controller.SetBool(AnimatorHash, value);
		}

		override protected bool GetBool()
			=> TryGetController(out var controller)
				&& controller.GetBool(AnimatorHash);

		override protected float GetFloat()
			=> TryGetController(out var controller)
				? controller.GetFloat(AnimatorHash)
				: default;

		override protected int GetInteger()
			=> TryGetController(out var controller)
				? controller.GetInteger(AnimatorHash)
				: default;

		/// <summary>Controllers still alive: a rebuilt layer leaves the previous playables disposed.</summary>
		private AnimatorControllerPlayable[] ValidControllers() {
			if (Controllers == null)
				return Array.Empty<AnimatorControllerPlayable>();

			var valid = new List<AnimatorControllerPlayable>(Controllers.Length);
			foreach (var controller in Controllers)
				if (controller.IsValid())
					valid.Add(controller);

			return valid.ToArray();
		}

		private bool TryGetController(out AnimatorControllerPlayable controller) {
			if (Controllers != null)
				foreach (var candidate in Controllers)
					if (candidate.IsValid()) {
						controller = candidate;
						return true;
					}

			controller = default;
			return false;
		}
	}
}
