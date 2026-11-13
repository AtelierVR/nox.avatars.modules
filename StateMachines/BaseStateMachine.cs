using Nox.Avatars;
using Nox.Avatars.StateMachines;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.StateMachines {
	/// <summary>
	/// Base of every avatar state behaviour: it gives access to the avatar it drives and warns the behaviour when
	/// the module set of that avatar changes.
	/// </summary>
	public abstract class BaseStateMachine : StateMachineBehaviour, IStateMachine {
		private IRuntimeAvatar _runtime;

		/// <summary>Avatar this behaviour drives, or <c>null</c> when it was never set up.</summary>
		protected IRuntimeAvatar RuntimeAvatar
			=> _runtime;

		/// <summary>
		/// Descriptor of the avatar this behaviour drives, or <c>null</c> while <see cref="Setup"/> was never
		/// called. Only the setup carries the avatar, so nothing is cached here.
		/// </summary>
		protected IAvatarDescriptor Descriptor
			=> _runtime?.Descriptor;

		/// <summary>
		/// Binds the behaviour to the avatar that owns it and watches the module set of that avatar: the setup is
		/// called again as soon as a module joins or leaves the avatar, so a behaviour never keeps a rig, a module or
		/// a resolved target from a previous state.
		/// </summary>
		/// <param name="runtime">Avatar being set up.</param>
		public virtual bool Setup(IRuntimeAvatar runtime) {
			Unwatch();
			_runtime = runtime;
			Watch();

			Logger.LogDebug($"Setting up state machine {GetType().Name} in {runtime?.Descriptor}", this);
			return true;
		}

		private void Watch() {
			var descriptor = Descriptor;
			if (descriptor == null)
				return;

			descriptor.OnModuleRegistered.AddListener(OnModuleChanged);
			descriptor.OnModuleUnregistered.AddListener(OnModuleChanged);
		}

		private void Unwatch() {
			var descriptor = Descriptor;
			if (descriptor == null)
				return;

			descriptor.OnModuleRegistered.RemoveListener(OnModuleChanged);
			descriptor.OnModuleUnregistered.RemoveListener(OnModuleChanged);
		}

		/// <summary>A module joined or left the avatar: the behaviour is set up again on the same avatar.</summary>
		private void OnModuleChanged(IAvatarModule module)
			=> Setup(_runtime);

		private void OnDestroy()
			=> Unwatch();
	}
}
