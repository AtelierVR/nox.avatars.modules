using System;
using System.Collections.Generic;
using Nox.Avatars;
using Nox.CCK.Avatars.Camera;
using Nox.CCK.Avatars.EyeLooks;
using Nox.CCK.Avatars.Parameters;
using Nox.CCK.Avatars.Playable;
using Nox.CCK.Avatars.Rigging;
using Nox.CCK.Avatars.Scale;
using Nox.CCK.Avatars.Voice;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.Controllers;
using UnityEngine;
using Nox.Avatars.Rigging;

namespace Nox.Avatars.Modules.Runtime {
	public class Main : IMainModInitializer, IRiggingBackendRegistry {
		internal static IMainModCoreAPI CoreAPI;
		private         EventSubscription[] _events;

		internal static IControllerAPI ControllerAPI
			=> CoreAPI?.ModAPI
				.GetMod("controller")
				?.GetInstance<IControllerAPI>();

		public void OnInitializeMain(IMainModCoreAPI api) {
			CoreAPI = api;
			_events = new[] {
				api.EventAPI.Subscribe("avatar_check_request", OnCheckRequest),
			};
			PlayableAvatarModule.GetAssetController = () => CoreAPI.AssetAPI.GetAsset<RuntimeAnimatorController>("avatar:animations/Default.controller");

			CoreAPI.LoggerAPI.LogDebug("Avatar modules initialized.");
		}

		private static void OnCheckRequest(EventData context) {
			if (!context.TryGet<IAvatarDescriptor>(0, out var descriptor))
				return;
			var valid = true;
			valid &= CameraAvatarModule.Check(descriptor);
			valid &= CameraChopModule.Check(descriptor);
			valid &= AvatarParameterModule.Check(descriptor);
			valid &= PlayableAvatarModule.Check(descriptor);
			valid &= RiggingSetupModule.Check(descriptor);
			valid &= EyeLookAvatarModule.Check(descriptor);
			valid &= VoiceAvatarModule.Check(descriptor);
			valid &= ScaleAvatarModule.Check(descriptor);

			context.Callback(valid);
		}

		public void OnDisposeMain() {
			RiggingBackendRegistry.Clear();
			foreach (var e in _events)
				CoreAPI.EventAPI.Unsubscribe(e);
			_events                                 = Array.Empty<EventSubscription>();
			PlayableAvatarModule.GetAssetController = null;
			CoreAPI                                 = null;
		}

		// ── IRiggingBackendRegistry ──────────────────────────────────────────────

		void IRiggingBackendRegistry.Register(IRiggingBackend backend)                          
			=> RiggingBackendRegistry.Register(backend);

		void IRiggingBackendRegistry.Unregister(IRiggingBackend backend)                           
			=> RiggingBackendRegistry.Unregister(backend);

		IRiggingBackend IRiggingBackendRegistry.Resolve(IRuntimeAvatar context) 
			=> RiggingBackendRegistry.Resolve(context);

		IRiggingBackend[] IRiggingBackendRegistry.GetBackends()                                  
			=> RiggingBackendRegistry.GetBackends();
	}
}