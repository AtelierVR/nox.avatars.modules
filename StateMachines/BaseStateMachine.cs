using Nox.Avatars;
using Nox.Avatars.StateMachines;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.StateMachines {
	public abstract class BaseStateMachine : StateMachineBehaviour, IStateMachine {
		protected IRuntimeAvatar RuntimeAvatar;

		public virtual bool Setup(IRuntimeAvatar runtime) {
			RuntimeAvatar = runtime;
			Logger.LogDebug($"Setting up state machine {GetType().Name} in {runtime.GetDescriptor()}", this);
			return true;
		}
	}
}