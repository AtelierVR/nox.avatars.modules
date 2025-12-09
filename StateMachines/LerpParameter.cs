using System.Linq;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using Nox.CCK.Network;
using UnityEngine;

namespace Nox.CCK.Avatars.StateMachines {
	public class LerpParameter : BaseStateMachine {
		public  string           input;
		public  string           output;
		public  float            speed;
		private IParameterModule _module;

		public LerpParameter(string input, string output, float speed = 1f) {
			this.input  = input;
			this.output = output;
			this.speed  = speed;
		}

		public override bool Setup(IRuntimeAvatar runtime) {
			_module = runtime
				.GetDescriptor()
				.GetModules<IParameterModule>()
				.FirstOrDefault();
			return base.Setup(runtime);
		}


		public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
			base.OnStateUpdate(animator, stateInfo, layerIndex);
			if (_module == null) return;
			var iValue = _module.GetParameter(input);
			var oValue = _module.GetParameter(output);
			if (iValue == null || oValue == null) return;
			oValue.Set(
				oValue.GetValueType() switch {
					ParameterType.Float => Mathf.Lerp(
						oValue.Get().ToFloat(),
						iValue.Get().ToFloat(),
						Time.deltaTime * speed
					),
					ParameterType.Double => Mathf.Lerp(
						(float)oValue.Get().ToDouble(),
						(float)iValue.Get().ToDouble(),
						Time.deltaTime * speed
					),
					ParameterType.Vector3 => Vector3.Lerp(
						oValue.Get().ToVector3(),
						iValue.Get().ToVector3(),
						Time.deltaTime * speed
					),
					ParameterType.Quaternion => Quaternion.Lerp(
						oValue.Get().ToQuaternion(),
						iValue.Get().ToQuaternion(),
						Time.deltaTime * speed
					),
					_ => Mathf.Lerp(
						oValue.Get().ToFloat(),
						iValue.Get().ToFloat(),
						Time.deltaTime * speed
					),
				}
			);
		}
	}
}