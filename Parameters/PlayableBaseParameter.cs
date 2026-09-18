using UnityEngine;
using UnityEngine.Animations;

namespace Nox.CCK.Avatars.Parameters {
	public class PlayableBaseParameter : BaseParameter {
		internal AnimatorControllerPlayable Controller;

		override protected void SetFloat(float value)
			=> Controller.SetFloat(AnimatorHash, value);

		override protected void SetInteger(int value)
			=> Controller.SetInteger(AnimatorHash, value);

		override protected void SetBool(bool value)
			=> Controller.SetBool(AnimatorHash, value);

		override protected bool GetBool()
			=> Controller.GetBool(AnimatorHash);

		override protected float GetFloat()
			=> Controller.GetFloat(AnimatorHash);

		override protected int GetInteger()
			=> Controller.GetInteger(AnimatorHash);
	}
}