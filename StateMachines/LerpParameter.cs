using System.Linq;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using Nox.CCK.Network;
using UnityEngine;

namespace Nox.CCK.Avatars.StateMachines {
	public class LerpParameter : BaseStateMachine {
		public string input;
		public string output;
		public float speed;
		private IParameterModule _module;

		public LerpParameter(string input, string output, float speed = 1f) {
			this.input  = input;
			this.output = output;
			this.speed  = speed;
		}

		public override bool Setup(IRuntimeAvatar runtime) {
			_module = runtime
				.Descriptor
				.GetModules<IParameterModule>()
				.FirstOrDefault();
			return base.Setup(runtime);
		}


		private static double LerpDouble(double a, double b, float t)
			=> a + (b - a) * t;

		public override void OnStateUpdate(Animator animator, AnimatorStateInfo state, int layer) {
			base.OnStateUpdate(animator, state, layer);
			
			if (_module == null)
				return;

			var outputParam = _module.GetParameter(output);
			var inputParam  = _module.GetParameter(input);

			if (outputParam == null || inputParam == null)
				return;

			var t           = Time.deltaTime * speed;
			var inputValue  = inputParam.Get();
			var outputValue = outputParam.Get();

			outputParam.Set(
				outputParam.GetValueType() switch {
					ParameterType.Float => Mathf.Lerp(
						outputValue.ToFloat(),
						inputValue.ToFloat(),
						t
					),
					ParameterType.Double => LerpDouble(
						outputValue.ToDouble(),
						inputValue.ToDouble(),
						t
					),
					ParameterType.Vector3 => Vector3.Lerp(
						outputValue.ToVector3(),
						inputValue.ToVector3(),
						t
					),
					ParameterType.Quaternion => Quaternion.Lerp(
						outputValue.ToQuaternion(),
						inputValue.ToQuaternion(),
						t
					),
					_ => Mathf.Lerp(
						outputValue.ToFloat(),
						inputValue.ToFloat(),
						t
					),
				}
			);
		}
	}
}