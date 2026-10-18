using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Audio.Players;
using Nox.Avatars.Parameters;
using Nox.CCK.Mods.Events;
using Nox.Sessions;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Avatars.Modules.Runtime {
	/// <summary>
	/// Binds the avatar parameters <c>Muted</c> and <c>Deafened</c> to the local
	/// <see cref="IPlayerVoice"/>, using its events only (no per-frame polling).
	/// <para>
	/// The module only runs on the local avatar (the avatar loader passes <c>local=true</c> in
	/// <see cref="IRuntimeAvatar.Arguments"/>). A remote avatar receives those values from the
	/// relay through the avatar parameter synchronization, or directly from
	/// <c>Player.ApplyVoiceStatusToAvatar()</c>.
	/// </para>
	/// </summary>
	public class VoiceStatusAvatarModule : MonoBehaviour, IAvatarModule {
		private const string MutedParameter    = "Muted";
		private const string DeafenedParameter = "Deafened";
		private const string LocalArgument     = "local";

		private IParameter _muted;
		private IParameter _deafened;

		private IPlayerVoice _voice;
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

			_muted    = parameters?.GetParameter(MutedParameter);
			_deafened = parameters?.GetParameter(DeafenedParameter);

			SubscribeSessionEvents();
			BindVoice();

			return true;
		}

		private static bool IsLocal(IRuntimeAvatar avatar)
			=> avatar?.Arguments != null
				&& avatar.Arguments.TryGetValue(LocalArgument, out var local)
				&& local is true;

		private void OnDestroy() {
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

		/// <summary>The local player (re)appeared: bind its voice events instead of polling.</summary>
		private void OnSessionPlayerChanged(EventData context)
			=> BindVoice();

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

			Refresh();
		}

		private void UnbindVoice() {
			if (_voice == null)
				return;

			_voice.OnSelfMuteChanged.RemoveListener(OnSelfMuteChanged);
			_voice.OnDeafenChanged.RemoveListener(OnDeafenChanged);
			_voice = null;
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
		}

		private static void SetBool(IParameter parameter, bool value) {
			if (parameter == null || parameter.ValueType != ParameterType.Bool)
				return;

			if (parameter.Value is bool current && current == value)
				return;

			parameter.Value = value;
		}

		private static IPlayerVoice LocalVoice {
			get {
				var api = Main.CoreAPI?.ModAPI
					.GetMod("session")
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
