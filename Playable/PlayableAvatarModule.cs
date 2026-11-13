using System;
using System.Collections.Generic;
using System.Linq;
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
			public int Layer;
			public float From;
			public float To;
			public float Duration;
			public float Elapsed;
		}

		/// <summary>A layer as it plays: every controller sharing the same key, stacked in one mixer.</summary>
		private sealed class LayerGroup {
			public string Key;
			public PlayableLayerNaming Naming;

			/// <summary>Indices of the owning entries in <see cref="controllers"/>.</summary>
			public readonly List<int> Indices = new();

			/// <summary>Private mixer stacking the controllers, only valid when the key owns several of them.</summary>
			public AnimationLayerMixerPlayable Mixer;
		}

		private readonly List<LayerBlend> _layerBlends = new();

		/// <summary>Playable layers, one per distinct key (a key may own several controllers).</summary>
		private readonly List<LayerGroup> _layerGroups = new();

		private float[] _layerWeights;

		/// <summary>
		/// Outputs created by this module: the one driving the layer mixer and one per FX controller (see
		/// <see cref="Setup"/>), destroyed when the layers are rebuilt or the graph goes away.
		/// </summary>
		private readonly List<AnimationPlayableOutput> _outputs = new();

		/// <summary>Mixer input index of each playable layer (FX is mapped to the bottom).</summary>
		private int[] _mixerIndices;

		/// <summary>Whether the layers of <see cref="Setup"/> are built and still alive in the graph.</summary>
		private bool _layersBuilt;

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

			// Destroying the graph also destroys its outputs (including the direct FX ones).
			if (_graph.IsValid())
				_graph.Destroy();
		}

		/// <summary>
		/// Destroys the playables and outputs a previous <see cref="Setup"/> created, so that a second run of
		/// the setup (an avatar initialised again, the module re-created) drives the Animator with a single
		/// mixer instead of stacking a duplicate set of layers. Left in place, the duplicates keep the
		/// parameter bindings pointing at controllers that no longer affect the renderer, and the avatar
		/// stops following its parameters (no locomotion, no head tracking).
		/// </summary>
		private void TeardownPrevious() {
			if (!_graph.IsValid())
				return;

			foreach (var output in _outputs)
				if (output.IsOutputValid())
					_graph.DestroyOutput(output);
			_outputs.Clear();

			if (ControllerPlayables != null)
				foreach (var playable in ControllerPlayables)
					if (playable.IsValid())
						_graph.DestroyPlayable(playable);
			ControllerPlayables = null;

			foreach (var group in _layerGroups)
				if (group.Mixer.IsValid())
					_graph.DestroyPlayable(group.Mixer);

			if (_mixer.IsValid())
				_graph.DestroyPlayable(_mixer);

			_layersBuilt = false;
		}

		/// <summary>
		/// Whether the layers a previous <see cref="Setup"/> built can be kept: same graph, mixer and playables
		/// still alive. Keeping them is what keeps the parameters bound to the controllers driving the animator
		/// (an avatar initialised twice must not stack a second, unused set of layers).
		/// </summary>
		private bool CanReuseLayers(Animator animator)
			=> _layersBuilt
			   && _mixer.IsValid()
			   && _graph.IsValid()
			   && _graph.Equals(animator.playableGraph);

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

			var anchor = Descriptor.Anchor;
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

			if (CanReuseLayers(animator)) {
				Logger.LogDebug("Playable layers already built, keeping them.", tag: nameof(PlayableAvatarModule));
				return true;
			}

			TeardownPrevious();
			BuildLayerGroups();
			var layerCount = _layerGroups.Count;

			_graph = animator.playableGraph;
			var output = AnimationPlayableOutput.Create(_graph, "Animation", animator);
			_mixer = AnimationLayerMixerPlayable.Create(_graph, Mathf.Max(1, layerCount));

#if HAS_VISUALIZER
			GraphVisualizerClient.Show(_graph);
#endif

			_layerBlends.Clear();
			_layerWeights = new float[layerCount];
			_mixerIndices = new int[layerCount];
			ControllerPlayables = new AnimatorControllerPlayable[controllers.Length];

			var mixIndex = 0;
			foreach (var layer in Enumerable.Range(0, layerCount)
											.OrderBy(i => _layerGroups[i].Naming == PlayableLayerNaming.FX ? 0 : 1))
				_mixerIndices[layer] = mixIndex++;

			for (var layer = 0; layer < layerCount; layer++) {
				var group = _layerGroups[layer];
				var count = group.Indices.Count;
				var playables = new AnimatorControllerPlayable[count];

				for (var c = 0; c < count; c++) {
					var entry = group.Indices[c];
					var controller = controllers[entry].controller;
					var playable = AnimatorControllerPlayable.Create(_graph, controller);

					ControllerPlayables[entry] = playable;
					playables[c] = playable;
					Logger.LogDebug($"Setting up layer '{group.Key}' [{c + 1}/{count}] - {controller.name}", controller, tag: nameof(PlayableAvatarModule));
				}

				if (count == 1) {
					_graph.Connect(playables[0], 0, _mixer, _mixerIndices[layer]);
				} else {
					var stacked = AnimationLayerMixerPlayable.Create(_graph, count);
					for (var c = 0; c < count; c++) {
						_graph.Connect(playables[c], 0, stacked, c);
						stacked.SetInputWeight(c, 1f);
					}

					_graph.Connect(stacked, 0, _mixer, _mixerIndices[layer]);
					group.Mixer = stacked;
				}

				_mixer.SetInputWeight(_mixerIndices[layer], 1f);
				_layerWeights[layer] = 1f;
			}

			await UniTask.NextFrame(cancellationToken: token);

			isHide = !anchor.activeSelf;
			if (isHide) {
				anchor.SetActive(true);
				await UniTask.NextFrame(cancellationToken: token);
			}

			output.SetSourcePlayable(_mixer);
			_outputs.Clear();
			_outputs.Add(output);

			for (var layer = 0; layer < layerCount; layer++) {
				var group = _layerGroups[layer];
				if (group.Naming != PlayableLayerNaming.FX)
					continue;

				foreach (var entry in group.Indices) {
					var playable = ControllerPlayables[entry];
					if (!playable.IsValid())
						continue;

					AnimationPlayableOutput fxOutput = AnimationPlayableOutput.Create(_graph, $"FX {controllers[entry].controller.name}", animator);
					fxOutput.SetSourcePlayable(playable);
					_outputs.Add(fxOutput);
					Logger.LogDebug($"FX layer '{group.Key}': '{controllers[entry].controller.name}' bound to its own output (material properties)", tag: nameof(PlayableAvatarModule));
				}
			}

			_layersBuilt = true;

			await UniTask.NextFrame(cancellationToken: token);

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

		/// <summary>
		/// Groups the configured entries by key. Several controllers may share the same key (two FX
		/// controllers, a Base plus an extra one…) and then play together as a single layer. Entries without
		/// a controller or with an unusable key are reported and skipped; first-appearance order is kept.
		/// </summary>
		private void BuildLayerGroups(bool logSkipped = true) {
			_layerGroups.Clear();

			var groups = new Dictionary<string, LayerGroup>(StringComparer.Ordinal);
			for (var i = 0; i < controllers.Length; i++) {
				var layer = controllers[i];
				var controller = layer?.controller;

				if (!controller) {
					if (logSkipped)
						Logger.LogWarning($"Playable layer {i} has no controller assigned (key: '{layer?.Key}'); it will be skipped.", tag: nameof(PlayableAvatarModule));
					continue;
				}

				var key = layer.Key;
				if (string.IsNullOrEmpty(key)) {
					if (logSkipped)
						Logger.LogWarning($"Playable layer {i} ({controller.name}) has no usable key; it will be skipped.", controller, tag: nameof(PlayableAvatarModule));
					continue;
				}

				if (!groups.TryGetValue(key, out var group)) {
					group = new LayerGroup { Key = key, Naming = layer.naming };
					groups[key] = group;
					_layerGroups.Add(group);
				}

				group.Indices.Add(i);
			}
		}

		/// <summary>Builds the layer groups on first use, so key lookups work before <see cref="Setup"/>.</summary>
		private void EnsureLayerGroups() {
			if (_layerGroups.Count == 0 && controllers is { Length: > 0 })
				BuildLayerGroups(logSkipped: false);
		}

		/// <summary>Number of playable layers (distinct keys) available (0 until <see cref="Setup"/> has run).</summary>
		public int LayerCount {
			get {
				EnsureLayerGroups();
				return _layerGroups.Count;
			}
		}

		/// <summary>Whether the given layer index exists and the graph is ready.</summary>
		public bool IsValidLayer(int layer)
			=> _mixer.IsValid() && layer >= 0 && layer < _layerGroups.Count;

		/// <summary>Current weight of a playable layer, or 0 when the layer is invalid.</summary>
		public float GetLayerWeight(int layer)
			=> IsValidLayer(layer) ? _layerWeights[layer] : 0f;

		/// <summary>Key (name) of a playable layer, or an empty string when invalid.</summary>
		public string GetLayerKey(int layer)
			=> layer >= 0 && layer < _layerGroups.Count ? _layerGroups[layer].Key : string.Empty;

		/// <summary>
		/// Rôle standard déclaré par l'avatar pour cette couche, ou une chaîne vide quand la clé est un nom
		/// libre / un nom d'asset (voir <see cref="PlayableLayerNaming"/>).
		/// </summary>
		public string GetLayerRole(int layer) {
			if (layer < 0 || layer >= _layerGroups.Count)
				return string.Empty;

			var naming = _layerGroups[layer].Naming;
			return naming is PlayableLayerNaming.Controller or PlayableLayerNaming.Custom
				? string.Empty
				: naming.ToString();
		}

		/// <summary>Number of controllers behind a layer (1 unless several entries share the same key).</summary>
		public int GetControllerCount(int layer)
			=> layer >= 0 && layer < _layerGroups.Count ? _layerGroups[layer].Indices.Count : 0;

		/// <summary>Index of the n-th controller of a layer in <see cref="controllers"/>, or -1.</summary>
		public int GetControllerEntry(int layer, int index) {
			if (layer < 0 || layer >= _layerGroups.Count)
				return -1;

			var entries = _layerGroups[layer].Indices;
			return index >= 0 && index < entries.Count ? entries[index] : -1;
		}

		/// <summary>Index of the layer owning the given controller entry, or -1.</summary>
		public int GetLayerIndexForController(int controllerIndex) {
			EnsureLayerGroups();

			for (var layer = 0; layer < _layerGroups.Count; layer++)
				if (_layerGroups[layer].Indices.Contains(controllerIndex))
					return layer;

			return -1;
		}

		/// <summary>Playable of the n-th controller of a layer; invalid when out of range.</summary>
		public AnimatorControllerPlayable GetControllerPlayable(int layer, int index) {
			var entry = GetControllerEntry(layer, index);
			return entry >= 0 && ControllerPlayables != null ? ControllerPlayables[entry] : default;
		}

		/// <summary>Weight of the n-th controller of a layer (0 when out of range).</summary>
		public float GetControllerWeight(int layer, int index) {
			if (!IsValidLayer(layer) || GetControllerEntry(layer, index) < 0)
				return 0f;

			var group = _layerGroups[layer];
			return group.Mixer.IsValid()
				? group.Mixer.GetInputWeight(index) * _layerWeights[layer]
				: _layerWeights[layer];
		}

		/// <summary>
		/// Sets the weight of the n-th controller of a layer, relative to its siblings. When the layer owns
		/// a single controller this is the layer weight itself.
		/// </summary>
		public void SetControllerWeight(int layer, int index, float weight) {
			if (!IsValidLayer(layer) || GetControllerEntry(layer, index) < 0) {
				Logger.LogWarning($"Cannot set weight of controller {index} on playable layer {layer}: invalid layer or controller index.", tag: nameof(PlayableAvatarModule));
				return;
			}

			var group = _layerGroups[layer];
			if (!group.Mixer.IsValid()) {
				SetLayerWeight(layer, weight);
				return;
			}

			group.Mixer.SetInputWeight(index, Mathf.Clamp01(weight));
		}

		/// <summary>Index of the first layer matching the given key, or -1 when none matches.</summary>
		public int FindLayer(string key) {
			if (string.IsNullOrEmpty(key))
				return -1;

			EnsureLayerGroups();

			for (var i = 0; i < _layerGroups.Count; i++)
				if (string.Equals(_layerGroups[i].Key, key, StringComparison.Ordinal))
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
				Logger.LogWarning($"Cannot set weight of playable layer {layer}: invalid layer index (valid range 0..{_layerGroups.Count - 1}).", tag: nameof(PlayableAvatarModule));
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
				Layer = layer,
				From = _layerWeights[layer],
				To = weight,
				Duration = blendDuration,
				Elapsed = 0f
			});
		}

		private void ApplyLayerWeight(int layer, float weight) {
			if (!IsValidLayer(layer))
				return;
			_layerWeights[layer] = weight;
			_mixer.SetInputWeight(_mixerIndices?[layer] ?? layer, weight);
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
