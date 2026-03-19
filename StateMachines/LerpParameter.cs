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
		private IParameter       _inputParam;
		private IParameter       _outputParam;
		private ParameterType    _valueType;

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
			if (_module != null) {
				_inputParam  = _module.GetParameter(input);
				_outputParam = _module.GetParameter(output);
				if (_outputParam != null)
					_valueType = _outputParam.GetValueType();
			}
			return base.Setup(runtime);
		}


		private static double LerpDouble(double a, double b, float t)
			=> a + (b - a) * t;

		public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
			base.OnStateUpdate(animator, stateInfo, layerIndex);
			if (_inputParam == null || _outputParam == null) return;
			var t      = Time.deltaTime * speed;
			var iRaw   = _inputParam.Get();
			var oRaw   = _outputParam.Get();
			_outputParam.Set(
				_valueType switch {
					ParameterType.Float => Mathf.Lerp(
						oRaw.ToFloat(),
						iRaw.ToFloat(),
						t
					),
					ParameterType.Double => LerpDouble(
						oRaw.ToDouble(),
						iRaw.ToDouble(),
						t
					),
					ParameterType.Vector3 => Vector3.Lerp(
						oRaw.ToVector3(),
						iRaw.ToVector3(),
						t
					),
					ParameterType.Quaternion => Quaternion.Lerp(
						oRaw.ToQuaternion(),
						iRaw.ToQuaternion(),
						t
					),
					_ => Mathf.Lerp(
						oRaw.ToFloat(),
						iRaw.ToFloat(),
						t
					),
				}
			);
		}
	}
}