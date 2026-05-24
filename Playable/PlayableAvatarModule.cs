using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.StateMachines;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.Playable {
	public class PlayableAvatarModule : MonoBehaviour, IAvatarModule {
		public static Func<RuntimeAnimatorController> GetAssetController;

		public RuntimeAnimatorController[] controllers;
		private PlayableGraph _graph;
		private AnimationLayerMixerPlayable _mixer;
		public AnimatorControllerPlayable[] ControllerPlayables { get; private set; }
		public IAvatarDescriptor Descriptor;

		private void Start() {
			if (!_graph.IsValid())
				return;
			_graph.Play();
		}

		private void OnEnable() {
			if (!_graph.IsValid())
				return;
			_graph.Play();
		}

		private void OnDisable() {
			if (!_graph.IsValid())
				return;
			_graph.Stop();
		}

		private void OnDestroy() {
			if (!_graph.IsValid())
				return;
			_graph.Destroy();
		}

		public int Priority
			=> 100;

		public async UniTask<bool> Setup(IRuntimeAvatar runtimeAvatar, AvatarModulePhase phase, CancellationToken token = default) {
			if (phase != AvatarModulePhase.Init) return true;
			Descriptor = runtimeAvatar.Descriptor;
			if (Descriptor == null) {
				Logger.LogError("Avatar descriptor is not set, cannot play avatar module.");
				return false;
			}

			var  anchor = Descriptor.Anchor;
			bool isHide;

			var animator = Descriptor.Animator;
			if (!animator) {
				Logger.LogWarning("Animator is not set, PlayableAvatarModule will be disabled for this avatar.");
				return false;
			}

			animator.runtimeAnimatorController ??= GetAssetController();

			if (!animator.playableGraph.IsValid()) {

				// Activate the anchor to ensure the animator initializes its playable graph
				isHide = !anchor.activeSelf;
				if (isHide)
					anchor.SetActive(true);

				var cancelled = await UniTask.WaitUntil(
					() => animator.playableGraph.IsValid(),
					cancellationToken: token
				).SuppressCancellationThrow();

				if (isHide)
					anchor.SetActive(false);
				if (cancelled)
					return false;

				if (!animator.playableGraph.IsValid()) {
					Logger.LogError("Animator's playable graph never became valid, cannot play avatar module.");
					return false;
				}
			}


			controllers ??= Array.Empty<RuntimeAnimatorController>();
			if (controllers.Length == 0)
				Logger.LogWarning("No controllers have been setup for PlayableAvatarModule, the avatar may not animate correctly.", tag: nameof(PlayableAvatarModule));

			_graph = animator.playableGraph;
			var output = AnimationPlayableOutput.Create(_graph, "Animation", animator);
			_mixer = AnimationLayerMixerPlayable.Create(_graph, controllers.Length);

			#if HAS_VISUALIZER
			GraphVisualizerClient.Show(_graph);
			#endif

			ControllerPlayables = new AnimatorControllerPlayable[ controllers.Length ];
			for (var i = 0; i < controllers.Length; i++) {
				Logger.LogDebug($"Setting up controller {i} - {controllers[i]?.name ?? "null"}", controllers[i], tag: nameof(PlayableAvatarModule));
				var ctrlPlayable = AnimatorControllerPlayable.Create(_graph, controllers[i]);
				_graph.Connect(ctrlPlayable, 0, _mixer, i);
				_mixer.SetInputWeight(i, 1f);
				ControllerPlayables[i] = ctrlPlayable;
			}

			await UniTask.NextFrame(cancellationToken: token);

			isHide = !anchor.activeSelf;
			if (isHide) {
				anchor.SetActive(true);
				await UniTask.NextFrame(cancellationToken: token);
			}

			output.SetSourcePlayable(_mixer);
			await UniTask.NextFrame(cancellationToken: token);

			// Wait until the graph starts playing, or cancel if requested
			if (!_graph.IsPlaying()) {
				var graphCancelled = await UniTask.WaitUntil(
					() => _graph.IsPlaying(),
					cancellationToken: token
				).SuppressCancellationThrow();
				if (graphCancelled) {
					if (isHide)
						anchor.SetActive(false);
					return false;
				}
			}

			if (isHide)
				anchor.SetActive(false);

			var behaviors = animator.GetBehaviours<StateMachineBehaviour>();
			foreach (var behavior in behaviors) {
				if (behavior is not IStateMachine state)
					continue;
				Logger.LogDebug($"Setting up state machine behavior {behavior.GetType().Name}", behavior, tag: nameof(PlayableAvatarModule));
				state.Setup(runtimeAvatar);
			}

			return true;
		}

		public static bool Check(IAvatarDescriptor _)
			=> true;
	}
}