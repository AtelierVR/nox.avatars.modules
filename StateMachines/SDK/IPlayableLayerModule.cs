namespace Nox.Avatars.StateMachines {
	/// <summary>
	/// Exposes control over an avatar's playable layers, i.e. the layers of the
	/// <see cref="UnityEngine.Playables.PlayableGraph"/> built by the playable avatar module.
	/// <para/>
	/// It is implemented by the module that owns the graph, so state behaviors can
	/// start/stop (or blend) a given layer without referencing its concrete type.
	/// </summary>
	public interface IPlayableLayerModule : IAvatarModule {
		/// <summary>
		/// Number of playable layers currently available.
		/// </summary>
		public int LayerCount { get; }

		/// <summary>
		/// Whether the given layer index can be controlled (in range and graph valid).
		/// </summary>
		public bool IsValidLayer(int layer);

		/// <summary>
		/// Returns the key (name) of a playable layer, or an empty string when the layer is invalid.
		/// </summary>
		public string GetLayerKey(int layer);

		/// <summary>
		/// Returns the standard role declared by the avatar for a playable layer ("Default", "Locomotion",
		/// "Calibration", "Pose", "FX"), or an empty string when the layer has none (free-form or asset-name key).
		/// <para/>
		/// Unlike <see cref="GetLayerKey"/> this does not depend on how the key is written: use it to find
		/// a layer by purpose (e.g. the calibration/T-pose layer) whatever the avatar named it.
		/// </summary>
		public string GetLayerRole(int layer);

		/// <summary>
		/// Finds the index of the first playable layer whose key matches <paramref name="key"/>,
		/// or -1 when none matches.
		/// </summary>
		public int FindLayer(string key);

		/// <summary>Whether a playable layer with the given key exists.</summary>
		public bool IsValidLayer(string key);

		/// <summary>
		/// Returns the current weight of a playable layer, or 0 when the layer is invalid.
		/// </summary>
		public float GetLayerWeight(int layer);

		/// <summary>Returns the current weight of the layer matching the given key, or 0.</summary>
		public float GetLayerWeight(string key);

		/// <summary>
		/// Sets the weight of a playable layer, optionally blending to it over
		/// <paramref name="blendDuration"/> seconds (0 means instant).
		/// </summary>
		public void SetLayerWeight(int layer, float weight, float blendDuration = 0f);

		/// <summary>Sets the weight of the layer matching the given key.</summary>
		public void SetLayerWeight(string key, float weight, float blendDuration = 0f);

		/// <summary>
		/// Starts (enables) a playable layer by blending its weight to 1.
		/// </summary>
		public void StartLayer(int layer, float blendDuration = 0f);

		/// <summary>Starts (enables) the layer matching the given key.</summary>
		public void StartLayer(string key, float blendDuration = 0f);

		/// <summary>
		/// Stops (disables) a playable layer by blending its weight to 0.
		/// </summary>
		public void StopLayer(int layer, float blendDuration = 0f);

		/// <summary>Stops (disables) the layer matching the given key.</summary>
		public void StopLayer(string key, float blendDuration = 0f);
	}
}
