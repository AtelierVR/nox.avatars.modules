using UnityEngine;

namespace Nox.CCK.Avatars.Parameters {
	public class AnimatorBaseParameter : BaseParameter {
		internal Animator Animator;

		override protected void SetFloat(float value)
			=> Animator.SetFloat(AnimatorHash, value);

		override protected void SetInteger(int value)
			=> Animator.SetInteger(AnimatorHash, value);

		override protected void SetBool(bool value)
			=> Animator.SetBool(AnimatorHash, value);

		override protected float GetFloat()
			=> Animator.GetFloat(AnimatorHash);

		override protected int GetInteger()
			=> Animator.GetInteger(AnimatorHash);

		override protected bool GetBool()
			=> Animator.GetBool(AnimatorHash);
	}
}