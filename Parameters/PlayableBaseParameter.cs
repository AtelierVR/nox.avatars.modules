using UnityEngine;
using UnityEngine.Animations;

namespace Nox.CCK.Avatars.Parameters {
	public class PlayableBaseParameter : BaseParameter {
		internal AnimatorControllerPlayable Controller;

		override protected void SetFloat(float value)
			=> Controller.SetFloat(GetKey(), value);

		override protected void SetInteger(int value)
			=> Controller.SetInteger(GetKey(), value);

		override protected void SetBool(bool value)
			=> Controller.SetBool(GetKey(), value);

		override protected bool GetBool()
			=> Controller.GetBool(GetKey());

		override protected float GetFloat()
			=> Controller.GetFloat(GetKey());

		override protected int GetInteger()
			=> Controller.GetInteger(GetKey());
	}
}