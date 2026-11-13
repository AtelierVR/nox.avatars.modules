using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using Nox.CCK.Avatars.Common;
using Nox.CCK.Events;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.Parameters {
	public class AvatarParameterModule : MonoBehaviour, IParameterModule {
		public static bool Check(IAvatarDescriptor descriptor) {
			var modules = descriptor.GetModules<AvatarParameterModule>();

			var module = modules.Length switch {
				1 => modules.FirstOrDefault(),
				0 => descriptor.Anchor.AddComponent<AvatarParameterModule>(),
				_ => null
			};

			if (!module) {
				Logger.LogError("Verify that the Avatar prefab has a valid AvatarParameterModule component.");
				return false;
			}

			return true;
		}

		public AvatarParameters parameters;
		public IRuntimeAvatar Runtime;

		/// <summary>
		/// Paramètres standards toujours exposés, même quand l'Animator de l'avatar ne les déclare
		/// pas : garantit que <see cref="GetParameter(string)"/> ne renvoie jamais <c>null</c> pour
		/// ces noms (Velocity = Vector3 zéro, Grounded = Bool false, Pose = Int 0).
		/// <para>
		/// <c>Sync</c> carries the synchronization direction used by the relay when it mirrors the
		/// parameter as an entity property (<see cref="ParameterFlags.OwnerSyncsToViewers"/>,
		/// <see cref="ParameterFlags.ViewerSyncsToOwner"/>).
		/// </para>
		/// </summary>
		public static readonly (string Name, ParameterType Type, object Default, ParameterFlags Sync, string Category)[] DefaultParameters = {
			("Velocity", ParameterType.Vector3, Vector3.zero, ParameterFlags.OwnerBroadcast, ParameterCategory.Movement),
			("Grounded", ParameterType.Bool,    false,        ParameterFlags.OwnerBroadcast, ParameterCategory.Movement),
			("Pose",     ParameterType.Int,     0,            ParameterFlags.OwnerBroadcast, null),
			// Owned by the game (voice status, voice volume, team): never edited nor synchronized,
			// their value comes from the session state (see VoiceStatusAvatarModule and Player).
			("Voice/Muted",    ParameterType.Bool,  false, ParameterFlags.None, ParameterCategory.Audio),
			// Volume of the player's own voice (0..2), read from the microphone device
			// (see VoiceStatusAvatarModule).
			("Voice/Volume",   ParameterType.Float, 1f,    ParameterFlags.None, ParameterCategory.Audio),
			("Voice/Deafened", ParameterType.Bool,  false, ParameterFlags.None, ParameterCategory.Audio),
			// Team the player belongs to (`player.team`): whether it has one, its colour and its
			// relation to the local team. The Animator has no colour parameter: Team/Color is composed
			// from its scalar components (Team/Color.R, .G, .B, .A), see CompositeParameter.
			("Team/Assigned", ParameterType.Bool,  false,       ParameterFlags.None, ParameterCategory.Team),
			("Team/Color",    ParameterType.Color, Color.clear, ParameterFlags.None, ParameterCategory.Team),
			("Team/Relation", ParameterType.Int,   0,           ParameterFlags.None, ParameterCategory.Team)
		};

		/// <summary>
		/// Separator between a complex parameter and the name of its component
		/// (<c>Velocity/X</c>, <c>Team/Color/R</c>).
		/// </summary>
		private const string ComponentSeparator = "/";

		/// <summary>
		/// Delay before the persistent values are written after a change (see <see cref="Flush"/>): long
		/// enough that a session of tweaking only saves once, short enough that nothing is lost if the game
		/// never unloads the avatar. The avatar closing (see <see cref="OnDestroy"/>) flushes at once.
		/// </summary>
		private const int FlushDelay = 15000;

		private readonly List<IParameter> _paramList = new();
		private readonly Dictionary<string, IParameter> _byName = new();
		private readonly Dictionary<int, IParameter> _byHash = new();

		/// <summary>Values of the persistent parameters as last stored (see <see cref="ParameterPersistence"/>).</summary>
		private Dictionary<string, ParameterValue> _persisted;

		/// <summary>Whether the saved data of the avatar was read: the restore waits for both it and the parameters.</summary>
		private bool _ready;

		/// <summary>Pending debounced save of the persistent values, cancelled and restarted on every write.</summary>
		private CancellationTokenSource _flush;

		public IParameter[] Parameters { get; private set; } = Array.Empty<IParameter>();

		public int Priority
			=> 10;

		public UnityEvent<IParameter> OnRegistred { get; } = new();

		public UnityEvent<IParameter> OnUnRegistred { get; } = new();

		/// <summary>
		/// Raised after the value of a parameter was written (see <see cref="IParameter.OnValueChanged"/>), so a
		/// package outside the avatar (relay, controllers, editors…) follows the changes instead of polling.
		/// </summary>
		public UnityEvent<IParameter> OnValueChanged { get; } = new NoxEvent<IParameter>();

		public async UniTask<bool> Setup(IRuntimeAvatar runtimeAvatar, AvatarModulePhase phase, CancellationToken token = default) {
			await UniTask.Yield(cancellationToken: token);
			switch (phase) {
				case AvatarModulePhase.Init:
					Runtime = runtimeAvatar;
					return true;
				case AvatarModulePhase.Post:
					PopulateParameters();
					// Awaited: the avatar only counts as loaded once its saved values are on it, otherwise the
					// Animator defaults stay visible for a moment (an "off" toggle flashing back on).
					await RestorePersistentParameters(runtimeAvatar, token);
					return true;
				default:
					return true;
			}
		}

		/// <summary>
		/// Reads the values of the parameters flagged <see cref="ParameterFlags.Persistent"/> back from the
		/// data of the avatar (see <see cref="ParameterPersistence"/>), then keeps a snapshot of them so
		/// <see cref="Flush"/> only writes when one moves.
		/// <para>
		/// Awaited by the setup: reading the data may reach the table of the account, and the parameters of
		/// the avatar must hold the saved values before anything else uses them. Until it completes,
		/// <see cref="_persisted"/> stays <c>null</c> and nothing is written, so the stored values are never
		/// overwritten by the defaults of the Animator.
		/// </para>
		/// </summary>
		private async UniTask RestorePersistentParameters(IRuntimeAvatar runtime, CancellationToken token) {
			try {
				await runtime.Data.Load();

				// The avatar may have been swapped or unloaded while the data was loading.
				if (token.IsCancellationRequested || Runtime != runtime || runtime is UnityEngine.Object element && !element)
					return;

				_ready = true;

				// The graph of the avatar may still be building, in which case there is no parameter to write yet:
				// the restore then runs from PopulateParameters, when the module registers them.
				TryRestore();
			} catch (OperationCanceledException) {
				// The avatar went away: nothing to restore and nothing to keep.
			}
		}

		/// <summary>
		/// The avatar is identified only once it is fully loaded, so its saved data was read after this module
		/// was set up (see <see cref="IRuntimeAvatar.DataLoadedEvent"/>): the persistent values can be applied
		/// now, and the snapshot is taken again from what they give.
		/// </summary>
		public void OnBusEvent(string name, IBusContext context = null) {
			if (name != IRuntimeAvatar.DataLoadedEvent)
				return;

			_persisted = null;
			TryRestore();
		}

		/// <summary>
		/// Applies the saved values once both the data of the avatar and its parameters are known: whichever of
		/// <see cref="RestorePersistentParameters"/> (data read) and <see cref="PopulateParameters"/> (parameters
		/// registered) comes last calls it, so nothing has to poll for the other. Idempotent.
		/// </summary>
		private void TryRestore() {
			if (!_ready || _persisted != null || Parameters.Length == 0 || Runtime == null)
				return;

			var restored = ParameterPersistence.Restore(this, Runtime);
			if (restored > 0)
				Logger.LogDebug($"{restored} persistent parameter(s) restored.", tag: nameof(AvatarParameterModule));

			_persisted = ParameterPersistence.Capture(this);
		}

		/// <summary>
		/// Persists the parameters flagged <see cref="ParameterFlags.Persistent"/> when they are written, and
		/// announces the write to every listener (see <see cref="OnValueChanged"/>): the parameters have no
		/// event of their own, only the ones that implement <see cref="IParameter.OnValueChanged"/> report here.
		/// <para>
		/// The values travel with the avatar at once (see <see cref="ParameterPersistence.Stash" />), so the
		/// shutdown always finds them in the data; only the write to the table is deferred (see
		/// <see cref="FlushDelay"/>): a burst of changes restarts the timer and the values are stored once,
		/// fifteen seconds after the last one.
		/// </para>
		/// </summary>
		private void OnParameterValueChanged(IParameter parameter) {
			OnValueChanged.Invoke(parameter);

			// A loading or error avatar has no identifier: nothing to persist for it.
			if (_persisted == null || Runtime == null || !Runtime.IsLocal || !Runtime.Identifier.IsValid() || parameter == null)
				return;

			if (!parameter.Flags.HasFlag(ParameterFlags.Persistent))
				return;

			if (ParameterPersistence.TryCapture(this, _persisted, out var values))
				ParameterPersistence.Stash(Runtime, _persisted = values);

			_flush?.Cancel();
			_flush?.Dispose();

			// Tied to the avatar: a pending save is dropped when it unloads (the dispose flushes what matters).
			_flush = CancellationTokenSource.CreateLinkedTokenSource(Runtime.Token);

			FlushLaterAsync(_flush.Token).Forget();
		}

		private async UniTaskVoid FlushLaterAsync(CancellationToken token) {
			try {
				await UniTask.Delay(FlushDelay, DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, token);
			} catch (OperationCanceledException) {
				return;
			}

			Flush();
		}

		/// <summary>
		/// Puts what is left of the persistent values in the data of the avatar and starts its write. Runs on
		/// the deferred timer of a change (see <see cref="FlushDelay"/>) and when the avatar goes away (see
		/// <see cref="OnDestroy"/>); the avatar's disposal awaits the write (see <see cref="IRuntimeAvatar.Dispose"/>).
		/// </summary>
		private void Flush() {
			_flush?.Cancel();
			_flush?.Dispose();
			_flush = null;

			if (_persisted == null || Runtime == null)
				return;

			if (ParameterPersistence.TryCapture(this, _persisted, out var values))
				ParameterPersistence.Stash(Runtime, _persisted = values);

			Runtime.Data.Save().Forget();
		}

		private void OnDisable()
			=> Flush();

		/// <summary>
		/// The avatar is going away: what is left of the deferred save is written now. A remote avatar, or one
		/// whose data was never loaded, writes nothing (see <see cref="Flush"/>).
		/// </summary>
		private void OnDestroy()
			=> Flush();

		public void RegisterParameter(IParameter parameter) {
			var key = parameter.Key;
			if (_byHash.ContainsKey(key)) {
				Logger.LogDebug($"Parameter '{parameter.Name}' (key={key}) is already registered by '{_byHash[key].Name}'; skipped.", tag: nameof(AvatarParameterModule));
				return;
			}

			_paramList.Add(parameter);
			_byName.TryAdd(parameter.Name, parameter);
			_byHash[key] = parameter;
			Parameters = _paramList.ToArray();

			parameter.OnValueChanged?.AddListener(OnParameterValueChanged);

			OnRegistred.Invoke(parameter);
		}

		public void UnregisterParameter(IParameter parameter) {
			_paramList.Remove(parameter);
			_byName.Remove(parameter.Name);
			_byHash.Remove(parameter.Key);
			Parameters = _paramList.ToArray();

			parameter.OnValueChanged?.RemoveListener(OnParameterValueChanged);

			OnUnRegistred.Invoke(parameter);
		}

		private void PopulateParameters() {
			var animator = Runtime?.Descriptor?.Animator;
			if (animator && animator.runtimeAnimatorController && animator.playableGraph.IsValid()) {
				RegisterAnimatorParameters(animator);
				RebindPlayableParameters(animator);
			}

			// The parameters are known now: apply the saved values if the data of the avatar was already read.
			TryRestore();
		}

		/// <summary>
		/// Registers every parameter the avatar declares (playable layers and Animator parameters), the
		/// composed parameters and the <see cref="DefaultParameters"/> fallbacks.
		/// </summary>
		private void RegisterAnimatorParameters(Animator animator) {
			var entries = parameters?.parameters ?? Array.Empty<ParameterEntry>();

			foreach (var pair in CollectPlayableTargets(animator)) {
				var entry = entries.FirstOrDefault(e => e.GetNameHash() == pair.Key);
				RegisterParameter(new PlayableBaseParameter {
					Controllers = pair.Value.Playables.ToArray(),
					Parameter = pair.Value.Declaration,
					Entry = entry
				});
			}

			foreach (var parameter in animator.parameters) {
				var entry = entries.FirstOrDefault(e => e.GetNameHash() == parameter.nameHash);
				RegisterParameter(new AnimatorBaseParameter { Animator = animator, Parameter = parameter, Entry = entry });
			}

			RegisterCompositeParameters(entries);

			RegisterDefaultParameters();
		}

		/// <summary>Controllers declaring a parameter, and the declaration kept as reference.</summary>
		private sealed class PlayableTargets {
			public AnimatorControllerParameter Declaration;
			public readonly List<AnimatorControllerPlayable> Playables = new();
		}

		/// <summary>
		/// One entry per parameter name, listing every controller playable that declares it: a key of the
		/// <c>PlayableAvatarModule</c> owns all the controllers sharing it (e.g. two FX controllers) and each
		/// <c>AnimatorControllerPlayable</c> keeps its own parameter storage, so a value must be written into
		/// all of them.
		/// </summary>
		private static Dictionary<int, PlayableTargets> CollectPlayableTargets(Animator animator) {
			var targets = new Dictionary<int, PlayableTargets>();

			foreach (var controller in animator.GetLayerControllers())
				for (var i = 0; i < controller.GetParameterCount(); i++) {
					var cp = controller.GetParameter(i);

					if (!targets.TryGetValue(cp.nameHash, out var target)) {
						target = new PlayableTargets { Declaration = cp };
						targets[cp.nameHash] = target;
					} else if (target.Declaration.type != cp.type) {
						Logger.LogDebug($"Parameter '{cp.name}' is declared as {target.Declaration.type} here and {cp.type} elsewhere; that controller is not bound to it.", tag: nameof(AvatarParameterModule));
						continue;
					}

					if (!target.Playables.Contains(controller))
						target.Playables.Add(controller);
				}

			return targets;
		}

		/// <summary>
		/// Points the parameters that are already registered to the current layer controllers. Called after the
		/// parameters of an avatar are (re)registered: the playable module may have rebuilt its layers, and the
		/// previous controllers are destroyed then — keeping their handles would leave the avatar unanimated.
		/// </summary>
		private void RebindPlayableParameters(Animator animator) {
			if (!animator)
				return;

			var targets = CollectPlayableTargets(animator);
			foreach (var parameter in _paramList) {
				if (parameter is not PlayableBaseParameter playable)
					continue;

				playable.Controllers = targets.TryGetValue(playable.AnimatorHash, out var target)
					? target.Playables.ToArray()
					: Array.Empty<AnimatorControllerPlayable>();
			}
		}

		/// <summary>
		/// Enregistre les <see cref="DefaultParameters"/> absents de l'Animator afin que
		/// <see cref="GetParameter(string)"/> ne renvoie jamais <c>null</c> pour ces noms standards.
		/// Les paramètres réellement déclarés par l'avatar (et les paramètres complexes composés)
		/// gardent la priorité (enregistrés d'abord).
		/// </summary>
		private void RegisterDefaultParameters() {
			foreach (var (name, type, defaultValue, sync, category) in DefaultParameters) {
				if (_byName.ContainsKey(name))
					continue;

				RegisterParameter(new DefaultParameter(name, type, defaultValue, sync, category));
			}
		}

		/// <summary>
		/// Composes every complex parameter (Color, Vector3, Quaternion) declared by the avatar asset or
		/// by <see cref="DefaultParameters"/> from the parameters the avatar declares, one per component
		/// (<c>Name/Component</c>, recursivement pour une composante complexe). A complex parameter with
		/// no component stays a plain fallback value (see <see cref="RegisterDefaultParameters"/>).
		/// </summary>
		private void RegisterCompositeParameters(ParameterEntry[] entries) {
			foreach (var entry in entries) {
				if (entry == null)
					continue;

				// A parameter the game owns (Velocity, Voice/...) keeps the category this module declares for it:
				// the authored entry may not carry one, and it must not move the parameter out of its section.
				TryRegisterComposite(entry.name, entry.type, entry.flags, CategoryOf(entry.name));
			}

			foreach (var (name, type, _, sync, category) in DefaultParameters)
				TryRegisterComposite(name, type, sync, category);
		}

		/// <summary>
		/// Category a parameter is registered with: the one <see cref="DefaultParameters"/> declares for it.
		/// A parameter the avatar authors itself belongs to the general section (<c>null</c>).
		/// </summary>
		internal static string CategoryOf(string name) {
			foreach (var (defaultName, _, _, _, defaultCategory) in DefaultParameters)
				if (defaultName == name)
					return defaultCategory;

			return null;
		}

		/// <summary>
		/// Registers a <see cref="CompositeParameter"/> for <paramref name="name"/> when the avatar
		/// declares at least one of its components (see <see cref="ParameterTypeProviders"/>).
		/// </summary>
		private void TryRegisterComposite(
			string name,
			ParameterType type,
			ParameterFlags flags,
			string category = null
		) {
			if (string.IsNullOrEmpty(name) || !ParameterTypeProviders.TryGet(type, out var provider))
				return;

			// Already a real parameter (or already composed): keep it.
			if (_byName.ContainsKey(name))
				return;

			var composite = Compose(name, provider, flags, category, out var found);
			if (found)
				RegisterParameter(composite);
		}

		/// <summary>
		/// Builds the composite of <paramref name="name"/>: a component is either a parameter of the
		/// avatar (<c>name/Component</c>) or, when its type is complex, a composite built recursively
		/// (<c>name/Component/Sub</c>). <paramref name="found"/> tells whether anything resolved.
		/// </summary>
		private CompositeParameter Compose(
			string name,
			ParameterTypeProvider provider,
			ParameterFlags flags,
			string category,
			out bool found
		) {
			var components = new IParameter[provider.Components.Length];
			found = false;

			for (var i = 0; i < components.Length; i++) {
				var component = provider.Components[i];
				var path = name + ComponentSeparator + component.Name;

				if (ParameterTypeProviders.TryGet(component.Type, out var nested)) {
					var composite = Compose(path, nested, flags, category, out var nestedFound);
					if (nestedFound) {
						components[i] = composite;
						found = true;
					}
					continue;
				}

				if (_byName.TryGetValue(path, out var scalar)) {
					components[i] = scalar;
					found = true;
				}
			}

			return new CompositeParameter(name, provider, components, flags, category);
		}

		public IParameter[] GetParameters()
			=> Parameters;

		/// <summary>
		/// Captures the parameters flagged <see cref="ParameterFlags.Persistent"/> as a named profile of
		/// the local user for this avatar. A remote avatar saves nothing.
		/// </summary>
		public bool SaveProfile(string profileName)
			=> AvatarParameterProfiles.Save(Runtime, profileName, this);

		/// <summary>
		/// Applies the named profile of the local user (or the template of the same name) to the savable
		/// parameters of this avatar. Returns the number of parameters applied.
		/// </summary>
		public int ApplyProfile(string profileName)
			=> AvatarParameterProfiles.Apply(Runtime, profileName, this);

		public IParameter GetParameter(string n)
			=> _byName.GetValueOrDefault(n);

		public IParameter GetParameter(int hash)
			=> _byHash.GetValueOrDefault(hash);
	}
}
