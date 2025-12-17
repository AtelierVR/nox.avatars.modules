using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.StateMachines;
using Nox.CCK.Avatars.Common;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.Playable {
	public class PlayableAvatarModule : MonoBehaviour, IAvatarModule {
		public static Func<RuntimeAnimatorController> GetAssetController;

		public  RuntimeAnimatorController[]  controllers;
		private PlayableGraph                _graph;
		private AnimationLayerMixerPlayable  _mixer;
		public  AnimatorControllerPlayable[] ControllerPlayables { get; private set; }
		public  IAvatarDescriptor            Descriptor;

		private void Start() {
			if (!_graph.IsValid()) return;
			_graph.Play();
		}

		private void OnEnable() {
			if (!_graph.IsValid()) return;
			_graph.Play();
		}

		private void OnDisable() {
			if (!_graph.IsValid()) return;
			_graph.Stop();
		}

		private void OnDestroy() {
			if (!_graph.IsValid()) return;
			_graph.Destroy();
		}

		public int GetPriority()
			=> 200;

		public async UniTask<bool> Setup(IRuntimeAvatar runtimeAvatar) {
			Descriptor = runtimeAvatar.GetDescriptor();
			var  anchor = Descriptor.GetAnchor();
			bool isHide;

			if (Descriptor == null) {
				Logger.LogError("Avatar descriptor is not set, cannot play avatar module.");
				return false;
			}

			var animator = Descriptor.GetAnimator();

			if (!animator) {
				Logger.LogWarning("Animator is not set, PlayableAvatarModule will be disabled for this avatar.");
				return false;
			}

			animator.runtimeAnimatorController ??= GetAssetController();

			if (!animator.playableGraph.IsValid()) {
				// Activate the anchor to ensure the animator initializes its playable graph
				isHide = !anchor.activeSelf;
				if (isHide) anchor.SetActive(true);
				var startTime = Time.realtimeSinceStartup;
				await UniTask.WaitUntil(() => animator.playableGraph.IsValid() || Time.realtimeSinceStartup - startTime > 15f);
				if (isHide) anchor.SetActive(false);

				if (Time.realtimeSinceStartup - startTime > 15) {
					Logger.LogError("Timed out waiting for Animator's playable graph to become valid, cannot play avatar module.");
					return false;
				}
			}

			controllers ??= Array.Empty<RuntimeAnimatorController>();
			if (controllers.Length == 0)
				Logger.LogWarning("No controllers have been setup for PlayableAvatarModule, the avatar may not animate correctly.", tag: nameof(PlayableAvatarModule));

			_graph = animator.playableGraph;
			var output = AnimationPlayableOutput.Create(_graph, "Animation", animator);
			_mixer = AnimationLayerMixerPlayable.Create(_graph, controllers.Length);
			await UniTask.Yield();

			ControllerPlayables = new AnimatorControllerPlayable[controllers.Length];
			for (var i = 0; i < controllers.Length; i++) {
				Logger.LogDebug($"Setting up controller {i} - {controllers[i]?.name ?? "null"}", controllers[i], tag: nameof(PlayableAvatarModule));
				var ctrlPlayable = AnimatorControllerPlayable.Create(_graph, controllers[i]);
				_graph.Connect(ctrlPlayable, 0, _mixer, i);
				_mixer.SetInputWeight(i, 1f);
				ControllerPlayables[i] = ctrlPlayable;
			}

			// Ensure the animator is active to update AnimatorControllerPlayable for detecting StateMachineBehaviour
			isHide = !anchor.activeSelf;
			if (isHide) anchor.SetActive(true);
			output.SetSourcePlayable(_mixer);
			await UniTask.Yield();
			if (isHide) anchor.SetActive(false);

			// Setup state machine behaviours (on Animator level)
			foreach (var behavior in animator.GetBehaviours<StateMachineBehaviour>()) {
				if (behavior is not IStateMachine state) continue;
				Logger.LogDebug($"Setting up state machine behavior {behavior.GetType().Name}", behavior, tag: nameof(PlayableAvatarModule));
				state.Setup(runtimeAvatar);
			}

			return true;
		}


		public static bool Check(IAvatarDescriptor _)
			=> true;
	}
}