using UnityEngine;
using UnityEngine.Animations;

namespace Nox.CCK.Avatars.Parameters {
	public class PlayableBaseParameter : BaseParameter {
		internal AnimatorControllerPlayable Controller;
		internal Animator                   Animator;

		override protected void SetFloat(float value)
			=> Animator.SetFloat(GetKey(), value);

		override protected void SetInteger(int value)
			=> Animator.SetInteger(GetKey(), value);

		override protected void SetBool(bool value)
			=> Animator.SetBool(GetKey(), value);

		override protected bool GetBool()
			=> Controller.GetBool(GetKey());

		override protected float GetFloat()
			=> Controller.GetFloat(GetKey());

		override protected int GetInteger()
			=> Controller.GetInteger(GetKey());
	}
}