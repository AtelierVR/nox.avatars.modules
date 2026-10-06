using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.StateMachines;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.Playable {
	public class PlayableAvatarModule : MonoBehaviour, IAvatarModule, IPlayableLayerModule {
		public static Func<RuntimeAnimatorController> GetAssetController;

		public PlayableLayer[] controllers;
		private PlayableGraph _graph;
		private AnimationLayerMixerPlayable _mixer;
		public AnimatorControllerPlayable[] ControllerPlayables { get; private set; }
		public IAvatarDescriptor Descriptor;

		/// <summary>An in-flight weight blend for a single playable layer.</summary>
		private struct LayerBlend {
			public int   Layer;
			public float From;
			public float To;
			public float Duration;
			public float Elapsed;
		}

		private readonly List<LayerBlend> _layerBlends = new();
		private float[] _layerWeights;

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
			_layerBlends.Clear();
			if (!_graph.IsValid())
				return;
			_graph.Stop();
		}

		private void OnDestroy() {
			_layerBlends.Clear();
			if (!_graph.IsValid())
				return;
			_graph.Destroy();
		}

		private void Update() {
			UpdateLayerBlends();
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


			controllers ??= Array.Empty<PlayableLayer>();
			if (controllers.Length == 0)
				Logger.LogWarning("No controllers have been setup for PlayableAvatarModule, the avatar may not animate correctly.", tag: nameof(PlayableAvatarModule));

			_graph = animator.playableGraph;
			var output = AnimationPlayableOutput.Create(_graph, "Animation", animator);
			_mixer = AnimationLayerMixerPlayable.Create(_graph, controllers.Length);

			#if HAS_VISUALIZER
			GraphVisualizerClient.Show(_graph);
			#endif

			_layerBlends.Clear();
			_layerWeights     = new float[controllers.Length];
			ControllerPlayables = new AnimatorControllerPlayable[ controllers.Length ];
			for (var i = 0; i < controllers.Length; i++) {
				var layer           = controllers[i];
				var layerController = layer?.controller;

				if (!layerController) {
					_layerWeights[i] = 0f;
					Logger.LogWarning($"Playable layer {i} has no controller assigned (key: '{layer?.Key}'); it will be skipped.", tag: nameof(PlayableAvatarModule));
					continue;
				}

				Logger.LogDebug($"Setting up layer {i} - {layerController.name} (key: '{layer.Key}')", layerController, tag: nameof(PlayableAvatarModule));
				var ctrlPlayable = AnimatorControllerPlayable.Create(_graph, layerController);
				_graph.Connect(ctrlPlayable, 0, _mixer, i);
				_mixer.SetInputWeight(i, 1f);
				_layerWeights[i] = 1f;
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

		#region Playable layers

		/// <summary>Number of playable layers available (0 until <see cref="Setup"/> has run).</summary>
		public int LayerCount
			=> controllers?.Length ?? 0;

		/// <summary>Whether the given layer index exists and the graph is ready.</summary>
		public bool IsValidLayer(int layer)
			=> _mixer.IsValid() && layer >= 0 && layer < LayerCount;

		/// <summary>Current weight of a playable layer, or 0 when the layer is invalid.</summary>
		public float GetLayerWeight(int layer)
			=> IsValidLayer(layer) ? _layerWeights[layer] : 0f;

		/// <summary>Key (name) of a playable layer, or an empty string when invalid.</summary>
		public string GetLayerKey(int layer)
			=> layer >= 0 && layer < LayerCount ? controllers[layer]?.Key ?? string.Empty : string.Empty;

		/// <summary>
		/// Rôle standard déclaré par l'avatar pour cette couche, ou une chaîne vide quand la clé est un nom
		/// libre / un nom d'asset (voir <see cref="PlayableLayerNaming"/>).
		/// </summary>
		public string GetLayerRole(int layer) {
			if (layer < 0 || layer >= LayerCount)
				return string.Empty;

			var naming = controllers[layer]?.naming ?? PlayableLayerNaming.Controller;
			return naming is PlayableLayerNaming.Controller or PlayableLayerNaming.Custom
				? string.Empty
				: naming.ToString();
		}

		/// <summary>Index of the first layer matching the given key, or -1 when none matches.</summary>
		public int FindLayer(string key) {
			if (string.IsNullOrEmpty(key) || controllers == null)
				return -1;

			for (var i = 0; i < controllers.Length; i++)
				if (string.Equals(controllers[i]?.Key, key, StringComparison.Ordinal))
					return i;

			return -1;
		}

		/// <summary>Whether a playable layer with the given key exists.</summary>
		public bool IsValidLayer(string key)
			=> FindLayer(key) >= 0;

		/// <summary>Current weight of the layer matching the given key, or 0.</summary>
		public float GetLayerWeight(string key)
			=> GetLayerWeight(FindLayer(key));

		/// <summary>Sets the weight of the layer matching the given key.</summary>
		public void SetLayerWeight(string key, float weight, float blendDuration = 0f)
			=> SetLayerWeight(FindLayer(key), weight, blendDuration);

		/// <summary>Starts (enables) the layer matching the given key.</summary>
		public void StartLayer(string key, float blendDuration = 0f)
			=> StartLayer(FindLayer(key), blendDuration);

		/// <summary>Stops (disables) the layer matching the given key.</summary>
		public void StopLayer(string key, float blendDuration = 0f)
			=> StopLayer(FindLayer(key), blendDuration);

		/// <summary>Starts (enables) a playable layer, blending its weight to 1.</summary>
		public void StartLayer(int layer, float blendDuration = 0f)
			=> SetLayerWeight(layer, 1f, blendDuration);

		/// <summary>Stops (disables) a playable layer, blending its weight to 0.</summary>
		public void StopLayer(int layer, float blendDuration = 0f)
			=> SetLayerWeight(layer, 0f, blendDuration);

		/// <summary>
		/// Sets the weight of a playable layer, optionally blending to it over <paramref name="blendDuration"/> seconds.
		/// Layer 0 is the base layer but, unlike VRChat, it can also be blended down like any other layer
		/// (e.g. to reveal a T-pose/calibration pose on top of it).
		/// </summary>
		public void SetLayerWeight(int layer, float weight, float blendDuration = 0f) {
			if (!IsValidLayer(layer)) {
				Logger.LogWarning($"Cannot set weight of playable layer {layer}: invalid layer index (valid range 0..{LayerCount - 1}).", tag: nameof(PlayableAvatarModule));
				return;
			}

			weight = Mathf.Clamp01(weight);

			// Drop any in-flight blend that was already targeting this layer.
			for (var i = _layerBlends.Count - 1; i >= 0; i--)
				if (_layerBlends[i].Layer == layer)
					_layerBlends.RemoveAt(i);

			if (blendDuration <= 0f || Mathf.Approximately(_layerWeights[layer], weight)) {
				ApplyLayerWeight(layer, weight);
				return;
			}

			_layerBlends.Add(new LayerBlend {
				Layer    = layer,
				From     = _layerWeights[layer],
				To       = weight,
				Duration = blendDuration,
				Elapsed  = 0f
			});
		}

		private void ApplyLayerWeight(int layer, float weight) {
			if (!IsValidLayer(layer))
				return;
			_layerWeights[layer] = weight;
			_mixer.SetInputWeight(layer, weight);
		}

		private void UpdateLayerBlends() {
			if (_layerBlends.Count == 0 || !_mixer.IsValid())
				return;

			var deltaTime = Time.deltaTime;
			for (var i = _layerBlends.Count - 1; i >= 0; i--) {
				var blend = _layerBlends[i];
				blend.Elapsed += deltaTime;

				var t = blend.Duration <= 0f ? 1f : Mathf.Clamp01(blend.Elapsed / blend.Duration);
				ApplyLayerWeight(blend.Layer, Mathf.Lerp(blend.From, blend.To, t));

				if (t >= 1f)
					_layerBlends.RemoveAt(i);
				else
					_layerBlends[i] = blend;
			}
		}

		#endregion

		public static bool Check(IAvatarDescriptor _)
			=> true;
	}
}