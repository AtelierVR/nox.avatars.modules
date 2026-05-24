using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using Nox.CCK.Avatars.Common;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
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
		public IRuntimeAvatar   Runtime;

		private readonly Dictionary<int, object>       _history   = new();
		private readonly List<IParameter>              _paramList = new();
		private readonly Dictionary<string, IParameter> _byName   = new();
		private readonly Dictionary<int,    IParameter> _byHash   = new();

		public IParameter[] Parameters { get; private set; } = Array.Empty<IParameter>();

		public int Priority
			=> 10;

		public async UniTask<bool> Setup(IRuntimeAvatar runtimeAvatar, AvatarModulePhase phase, CancellationToken token = default) {
			await UniTask.Yield(cancellationToken: token);
			switch (phase) {
				case AvatarModulePhase.Init:
					Runtime    = runtimeAvatar;
					return true;
				case AvatarModulePhase.Post:
					PopulateParameters();
					return true;
				default:
					return true;
			}
		}

		public void RegisterParameter(IParameter parameter) {
			var key = parameter.GetKey();
			if (_byHash.ContainsKey(key)) return;
			_paramList.Add(parameter);
			_byName.TryAdd(parameter.GetName(), parameter);
			_byHash[key] = parameter;
			Parameters   = _paramList.ToArray();
		}

		public void UnregisterParameter(IParameter parameter) {
			_paramList.Remove(parameter);
			_byName.Remove(parameter.GetName());
			_byHash.Remove(parameter.GetKey());
			Parameters = _paramList.ToArray();
		}

		private void PopulateParameters() {
			var animator = Runtime?.Descriptor?.Animator;
			if (!animator || !animator.runtimeAnimatorController || !animator.playableGraph.IsValid()) return;

			var entries = parameters?.parameters ?? Array.Empty<ParameterEntry>();

			// Paramètres des contrôleurs du playable graph (priorité haute)
			foreach (var controller in animator.GetControllers()) {
				for (var i = 0; i < controller.GetParameterCount(); i++) {
					var cp    = controller.GetParameter(i);
					var entry = entries.FirstOrDefault(e => e.GetNameHash() == cp.nameHash);
					RegisterParameter(new PlayableBaseParameter { Controller = controller, Parameter = cp, Entry = entry });
				}
			}

			// Paramètres de l'Animator de base (dédupliqués si déjà enregistrés)
			foreach (var parameter in animator.parameters) {
				var entry = entries.FirstOrDefault(e => e.GetNameHash() == parameter.nameHash);
				RegisterParameter(new AnimatorBaseParameter { Animator = animator, Parameter = parameter, Entry = entry });
			}
		}

		public IParameter[] GetParameters()
			=> Parameters;

		public IParameter GetParameter(string n)    
			=> _byName.GetValueOrDefault(n);

		public IParameter GetParameter(int    hash) 
			=> _byHash.GetValueOrDefault(hash);
	}
}