using UnityEngine;

namespace Nox.CCK.Avatars.Parameters {
	public class AnimatorBaseParameter : BaseParameter {
		internal Animator Animator;

		override protected void SetFloat(float value)
			=> Animator.SetFloat(GetKey(), value);

		override protected void SetInteger(int value)
			=> Animator.SetInteger(GetKey(), value);

		override protected void SetBool(bool value)
			=> Animator.SetBool(GetKey(), value);

		override protected float GetFloat()
			=> Animator.GetFloat(GetKey());

		override protected int GetInteger()
			=> Animator.GetInteger(GetKey());

		override protected bool GetBool()
			=> Animator.GetBool(GetKey());
	}
}