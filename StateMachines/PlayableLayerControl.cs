using System.Linq;
using Nox.Avatars;
using Nox.Avatars.StateMachines;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.StateMachines {
	/// <summary>
	/// What to do with a playable layer when a state is entered or exited.
	/// </summary>
	public enum PlayableLayerAction {
		/// <summary>Do nothing.</summary>
		None,

		/// <summary>Start (enable) the layer: blend its weight to 1.</summary>
		Start,

		/// <summary>Stop (disable) the layer: blend its weight to 0.</summary>
		Stop,

		/// <summary>Blend the layer weight to an explicit value.</summary>
		SetWeight
	}

	/// <summary>
	/// State behavior that starts/stops (or blends) a playable layer when entering and/or
	/// leaving a state, targeting it by its key (name) instead of a raw index.
	/// </summary>
	/// <remarks>
	/// Useful to force a layer such as <c>Pose</c> (T-Pose) while in a state, then
	/// release it on exit. The layer is resolved once during setup by looking up its key on
	/// the avatar's playable layer module, or from the state's Animator when the behavior
	/// was never set up by the module (the controllers played by the playable avatar
	/// module are not always returned by <c>Animator.GetBehaviours</c>).
	/// Any layer, including layer 0 (base), can be started/stopped.
	/// </remarks>
	public class PlayableLayerControl : BaseStateMachine {
		[Header("Target")]
		[Tooltip("Key (name) of the playable layer to control, as set on the PlayableAvatarModule (Default, Locomotion, Calibration, Pose, FX or a custom name).")]
		public string layerKey;

		/// <summary>
		/// Editor-only hint: keeps the inspector on the free-form key field while
		/// <see cref="layerKey"/> is empty. Not used at runtime.
		/// </summary>
		[HideInInspector] public bool customKey;

		[Header("On Enter")]
		[Tooltip("Action to run when entering the state.")]
		public PlayableLayerAction onEnter = PlayableLayerAction.Start;

		[Tooltip("Goal weight used when the enter action is SetWeight.")]
		[Range(0f, 1f)] public float enterWeight = 1f;

		[Tooltip("Blend duration in seconds used on enter. 0 means instant.")]
		public float enterBlendDuration;

		[Header("On Exit")]
		[Tooltip("Action to run when leaving the state.")]
		public PlayableLayerAction onExit = PlayableLayerAction.None;

		[Tooltip("Goal weight used when the exit action is SetWeight.")]
		[Range(0f, 1f)] public float exitWeight;

		[Tooltip("Blend duration in seconds used on exit. 0 means instant.")]
		public float exitBlendDuration;

		private IPlayableLayerModule _layers;
		private int                _target = -1;

		public override bool Setup(IRuntimeAvatar runtime) {
			_layers = runtime?.Descriptor?
				.GetModules<IPlayableLayerModule>()
				.FirstOrDefault();

			ResolveTarget();

			return base.Setup(runtime);
		}

		public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
			base.OnStateEnter(animator, stateInfo, layerIndex);
			Apply(animator, onEnter, enterWeight, enterBlendDuration, "enter");
		}

		public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
			base.OnStateExit(animator, stateInfo, layerIndex);
			Apply(animator, onExit, exitWeight, exitBlendDuration, "exit");
		}

		/// <summary>
		/// Résout la couche ciblée. Le module de couches est d'abord demandé à l'avatar (via
		/// <see cref="Setup"/> ou le descripteur), puis retrouvé depuis l'Animator de l'état : les controllers
		/// joués par le module playable ne passent pas tous par <see cref="Setup"/>. Rien n'est journalisé
		/// depuis l'exécution (log: false) pour ne pas spammer à chaque entrée/sortie d'état.
		/// </summary>
		private void ResolveTarget(Animator animator = null, bool log = true) {
			if (_layers == null)
				_layers = RuntimeAvatar?.Descriptor?
					.GetModules<IPlayableLayerModule>()
					.FirstOrDefault();

			if (_layers == null && animator) {
				var go = animator.gameObject;
				_layers = go.GetComponentInParent<IPlayableLayerModule>(true)
				          ?? go.GetComponentInChildren<IPlayableLayerModule>(true);
			}

			if (_layers == null) {
				if (log)
					Logger.LogWarning($"{nameof(PlayableLayerControl)} could not find a playable layer module on the avatar; it will do nothing.", this);
				return;
			}

			if (string.IsNullOrEmpty(layerKey)) {
				if (log && _target < 0)
					Logger.LogWarning($"{nameof(PlayableLayerControl)} has no layer key set; it will do nothing.", this);
				return;
			}

			_target = _layers.FindLayer(layerKey);
			if (_target < 0 && log)
				Logger.LogWarning($"{nameof(PlayableLayerControl)} targets layer key '{layerKey}' but no playable layer has this name.", this);
		}

		private void Apply(Animator animator, PlayableLayerAction action, float weight, float duration, string phase) {
			if (action == PlayableLayerAction.None)
				return;

			ResolveTarget(animator, log: false);
			if (_layers == null || _target < 0)
				return;

			switch (action) {
				case PlayableLayerAction.Start:
					_layers.StartLayer(_target, duration);
					break;
				case PlayableLayerAction.Stop:
					_layers.StopLayer(_target, duration);
					break;
				case PlayableLayerAction.SetWeight:
					_layers.SetLayerWeight(_target, weight, duration);
					break;
				default:
					return;
			}

			Logger.LogDebug($"{nameof(PlayableLayerControl)} {phase}: {action} layer '{layerKey}' (#{_target}) (blend {duration}s).", this);
		}
	}
}
