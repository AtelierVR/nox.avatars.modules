using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Audio.Players;
using Nox.Avatars.Parameters;
using Nox.CCK.Mods.Events;
using Nox.Entities;
using Nox.Sessions;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Avatars.Modules.Runtime {
	/// <summary>
	/// Binds the voice and team avatar parameters of the local player to its live state:
	/// <c>Voice/Muted</c> and <c>Voice/Deafened</c> from the <see cref="IPlayerVoice"/> events,
	/// <c>Voice/Volume</c> from the level of the captured audio, and <c>Team/Assigned</c>,
	/// <c>Team/Color</c> and <c>Team/Relation</c> from the team of the player.
	/// <para>
	/// The module only runs on the local avatar (the avatar loader passes <c>local=true</c> in
	/// <see cref="IRuntimeAvatar.Arguments"/>). A remote avatar receives those values from the
	/// relay through the avatar parameter synchronization, or directly from
	/// <c>Player.ApplyVoiceStatusToAvatar()</c> and <c>Player.ApplyTeamToAvatar()</c>.
	/// </para>
	/// </summary>
	public class VoiceStatusAvatarModule : MonoBehaviour, IAvatarModule {
		private const string MutedParameter       = "Voice/Muted";
		private const string DeafenedParameter    = "Voice/Deafened";
		private const string VoiceVolumeParameter = "Voice/Volume";
		private const string TeamAssignedParameter = "Team/Assigned";
		private const string TeamColorParameter   = "Team/Color";
		private const string TeamRelationParameter = "Team/Relation";
		private const string LocalArgument        = "local";

		/// <summary>Entity data key carrying the team identifier (<c>0</c> meaning none).</summary>
		private const string TeamDataKey = "team";

		private IParameter _muted;
		private IParameter _deafened;
		private IParameter _voiceVolume;
		private IParameter _teamAssigned;
		private IParameter _teamColor;
		private IParameter _teamRelation;
		private IParameterModule _module;

		private IPlayerVoice _voice;

		/// <summary>Local player as an entity, to follow its data (the team).</summary>
		private IEntity _entity;

		/// <summary>Local player as a team entity, to read its team.</summary>
		private ITeamEntity _teamEntity;

		private EventSubscription[] _subscriptions = Array.Empty<EventSubscription>();

		/// <summary>After <c>AvatarParameterModule</c> (10), before <c>VoiceAvatarModule</c> (50).</summary>
		public int Priority
			=> 15;

		public async UniTask<bool> Setup(IRuntimeAvatar runtimeAvatar, AvatarModulePhase phase, CancellationToken token = default) {
			if (phase != AvatarModulePhase.Post || !IsLocal(runtimeAvatar))
				return true;

			await UniTask.Yield(cancellationToken: token);

			var parameters = runtimeAvatar.Descriptor
				?.GetModules<IParameterModule>()
				.FirstOrDefault();

			_module = parameters;
			if (_module != null) {
				_module.OnRegistred.RemoveListener(OnParameterChanged);
				_module.OnUnRegistred.RemoveListener(OnParameterChanged);
				_module.OnRegistred.AddListener(OnParameterChanged);
				_module.OnUnRegistred.AddListener(OnParameterChanged);
			}

			RefreshParameters();

			SubscribeSessionEvents();
			BindVoice();

			return true;
		}

		private static bool IsLocal(IRuntimeAvatar avatar)
			=> avatar?.Arguments != null
				&& avatar.Arguments.TryGetValue(LocalArgument, out var local)
				&& local is true;

		/// <summary>
		/// The parameters are re-resolved on every register/unregister: a cached instance becomes
		/// unregistered when the parameter module is repopulated (controller changed, rig swapped) and
		/// writing through it would silently do nothing.
		/// </summary>
		private void OnParameterChanged(IParameter _)
			=> RefreshParameters();

		private void RefreshParameters() {
			if (_module == null)
				return;

			_muted        = _module.GetParameter(MutedParameter);
			_deafened     = _module.GetParameter(DeafenedParameter);
			_voiceVolume  = _module.GetParameter(VoiceVolumeParameter);
			_teamAssigned = _module.GetParameter(TeamAssignedParameter);
			_teamColor    = _module.GetParameter(TeamColorParameter);
			_teamRelation = _module.GetParameter(TeamRelationParameter);
		}

		private void OnDestroy() {
			if (_module != null) {
				_module.OnRegistred.RemoveListener(OnParameterChanged);
				_module.OnUnRegistred.RemoveListener(OnParameterChanged);
			}

			UnbindVoice();

			var core = Main.CoreAPI;
			if (core != null)
				foreach (var subscription in _subscriptions)
					core.EventAPI.Unsubscribe(subscription);

			_subscriptions = Array.Empty<EventSubscription>();
		}

		private void SubscribeSessionEvents() {
			var core = Main.CoreAPI;
			if (core == null || _subscriptions.Length > 0)
				return;

			_subscriptions = new[] {
				core.EventAPI.Subscribe("session_player_joined", OnSessionPlayerChanged),
				core.EventAPI.Subscribe("session_player_left", OnSessionPlayerChanged)
			};
		}

		/// <summary>
		/// The captured level has no event: it is read once per frame and written only when it moves
		/// (the animator setter is cheap, and the write itself is skipped by <see cref="SetFloat"/> when
		/// the value is unchanged).
		/// </summary>
		private void Update() {
			if (_voice == null)
				return;

			SetFloat(_voiceVolume, _voice.Audio?.Level ?? 0f);
		}

		/// <summary>The local player (re)appeared: bind its voice events instead of polling.</summary>
		private void OnSessionPlayerChanged(EventData context)
			=> BindVoice();

		/// <summary>Entity data of the local player changed: the team may have moved.</summary>
		private void OnPlayerDataChanged(string[] key, object @new, object @old) {
			if (key is { Length: 1 } && key[0] == TeamDataKey)
				RefreshTeam();
		}

		private void BindVoice() {
			var voice = LocalVoice;
			if (ReferenceEquals(voice, _voice))
				return;

			UnbindVoice();
			_voice = voice;

			if (_voice == null)
				return;

			_voice.OnSelfMuteChanged.AddListener(OnSelfMuteChanged);
			_voice.OnDeafenChanged.AddListener(OnDeafenChanged);

			_entity     = _voice as IEntity;
			_teamEntity = _voice as ITeamEntity;

			_entity?.Data.OnChanged.AddListener(OnPlayerDataChanged);

			Refresh();
		}

		private void UnbindVoice() {
			if (_voice == null)
				return;

			_voice.OnSelfMuteChanged.RemoveListener(OnSelfMuteChanged);
			_voice.OnDeafenChanged.RemoveListener(OnDeafenChanged);

			_entity?.Data.OnChanged.RemoveListener(OnPlayerDataChanged);

			_entity     = null;
			_teamEntity = null;
			_voice      = null;
		}

		private void OnSelfMuteChanged(bool muted)
			=> SetBool(_muted, muted);

		private void OnDeafenChanged(bool deafened)
			=> SetBool(_deafened, deafened);

		private void Refresh() {
			if (_voice == null)
				return;

			SetBool(_muted, _voice.IsSelfMuted);
			SetBool(_deafened, _voice.IsDeafened);
			SetFloat(_voiceVolume, _voice.Audio?.Level ?? 0f);
			RefreshTeam();
		}

		private void RefreshTeam() {
			var team = _teamEntity?.Team;

			SetBool(_teamAssigned, team != null);
			SetColor(_teamColor, team?.Color ?? Color.clear);
			SetInt(_teamRelation, (int)RelationOf(team));
		}

		/// <summary>
		/// Relation between the team of the local player and <paramref name="team"/>: <c>Neutral</c>
		/// when either has no team, <c>Ally</c> for the same team, else the session relation matrix.
		/// </summary>
		private TeamRelation RelationOf(ITeam team) {
			if (team == null)
				return TeamRelation.Neutral;

			var localTeam = _teamEntity?.Team;
			if (localTeam == null)
				return TeamRelation.Neutral;

			if (localTeam.Id == team.Id)
				return TeamRelation.Ally;

			return CurrentTeams?.GetRelation(localTeam, team) ?? TeamRelation.Neutral;
		}

		/// <summary>Team registry of the current session, or <c>null</c> when it is not available.</summary>
		private static ITeamSession CurrentTeams {
			get {
				var api = Main.CoreAPI?.ModAPI
					.GetMod("sessions")
					?.GetInstance<ISessionAPI>();

				return api != null && api.TryGet(api.Current, out var session)
					? session as ITeamSession
					: null;
			}
		}

		private static void SetBool(IParameter parameter, bool value) {
			if (parameter == null || parameter.ValueType != ParameterType.Bool)
				return;

			if (parameter.Value is bool current && current == value)
				return;

			parameter.Value = value;
		}

		private static void SetFloat(IParameter parameter, float value) {
			if (parameter == null || parameter.ValueType != ParameterType.Float)
				return;

			if (parameter.Value is float current && Mathf.Approximately(current, value))
				return;

			parameter.Value = value;
		}

		private static void SetInt(IParameter parameter, int value) {
			if (parameter == null || parameter.ValueType != ParameterType.Int)
				return;

			if (parameter.Value is int current && current == value)
				return;

			parameter.Value = value;
		}

		/// <summary>
		/// Writes a colour: a complex parameter (<see cref="ParameterType.Color"/>) is either a
		/// fallback value or a composite of its scalar components (see <c>CompositeParameter</c>).
		/// </summary>
		private static void SetColor(IParameter parameter, Color value) {
			if (parameter == null || parameter.ValueType != ParameterType.Color)
				return;

			if (parameter.Value is Color currentColor && currentColor == value)
				return;

			parameter.Value = value;
		}

		private static IPlayerVoice LocalVoice {
			get {
				var api = Main.CoreAPI?.ModAPI
					.GetMod("sessions")
					?.GetInstance<ISessionAPI>();

				return api != null && api.TryGet(api.Current, out var session)
					? session.LocalPlayer as IPlayerVoice
					: null;
			}
		}

		/// <summary>Ensures the avatar prefab carries exactly one module.</summary>
		public static bool Check(IAvatarDescriptor descriptor) {
			var modules = descriptor.GetModules<VoiceStatusAvatarModule>();

			var module = modules.Length switch {
				1 => modules.FirstOrDefault(),
				0 => descriptor.Anchor.AddComponent<VoiceStatusAvatarModule>(),
				_ => null
			};

			if (module)
				return true;

			Logger.LogError(
				"Verify that the Avatar prefab has a valid VoiceStatusAvatarModule component.",
				descriptor.Anchor
			);
			return false;
		}
	}
}
