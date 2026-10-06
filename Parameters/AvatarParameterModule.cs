using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using Nox.CCK.Avatars.Common;
using UnityEngine;
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
		public IRuntimeAvatar   Runtime;

		/// <summary>
		/// Paramètres standards toujours exposés, même quand l'Animator de l'avatar ne les déclare
		/// pas : garantit que <see cref="GetParameter(string)"/> ne renvoie jamais <c>null</c> pour
		/// ces noms (VelocityX/Y/Z = Float 0, Grounded = Bool false, Pose = Int 0).
		/// </summary>
		public static readonly (string Name, ParameterType Type, object Default)[] DefaultParameters = {
			("VelocityX", ParameterType.Float, 0f),
			("VelocityY", ParameterType.Float, 0f),
			("VelocityZ", ParameterType.Float, 0f),
			("Grounded",  ParameterType.Bool,  false),
			("Pose",      ParameterType.Int,   0)
		};

		private readonly List<IParameter>              _paramList = new();
		private readonly Dictionary<string, IParameter> _byName   = new();
		private readonly Dictionary<int,    IParameter> _byHash   = new();

		public IParameter[] Parameters { get; private set; } = Array.Empty<IParameter>();

		public int Priority
			=> 10;

        public UnityEvent<IParameter> OnRegistred { get; } = new();

        public UnityEvent<IParameter> OnUnRegistred { get; } = new();

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
			var key = parameter.Key;
			if (_byHash.ContainsKey(key)) {
				Logger.LogDebug($"Parameter '{parameter.Name}' (key={key}) is already registered by '{_byHash[key].Name}'; skipped.", tag: nameof(AvatarParameterModule));
				return;
			}
			
			_paramList.Add(parameter);
			_byName.TryAdd(parameter.Name, parameter);
			_byHash[key] = parameter;
			Parameters   = _paramList.ToArray();
			OnRegistred.Invoke(parameter);
		}

		public void UnregisterParameter(IParameter parameter) {
			_paramList.Remove(parameter);
			_byName.Remove(parameter.Name);
			_byHash.Remove(parameter.Key);
			Parameters = _paramList.ToArray();
			OnUnRegistred.Invoke(parameter);
		}

		private void PopulateParameters() {
			var animator = Runtime?.Descriptor?.Animator;
			if (!animator || !animator.runtimeAnimatorController || !animator.playableGraph.IsValid()) return;

			var entries = parameters?.parameters ?? Array.Empty<ParameterEntry>();

			foreach (var controller in animator.GetControllers())
				for (var i = 0; i < controller.GetParameterCount(); i++) {
					var cp    = controller.GetParameter(i);
					var entry = entries.FirstOrDefault(e => e.GetNameHash() == cp.nameHash);
					RegisterParameter(new PlayableBaseParameter { Controller = controller, Parameter = cp, Entry = entry });
				}

			foreach (var parameter in animator.parameters) {
				var entry = entries.FirstOrDefault(e => e.GetNameHash() == parameter.nameHash);
				RegisterParameter(new AnimatorBaseParameter { Animator = animator, Parameter = parameter, Entry = entry });
			}

			RegisterDefaultParameters();
		}

		/// <summary>
		/// Enregistre les <see cref="DefaultParameters"/> absents de l'Animator afin que
		/// <see cref="GetParameter(string)"/> ne renvoie jamais <c>null</c> pour ces noms standards.
		/// Les paramètres réellement déclarés par l'avatar gardent la priorité (enregistrés d'abord).
		/// </summary>
		private void RegisterDefaultParameters() {
			foreach (var (name, type, defaultValue) in DefaultParameters) {
				if (_byName.ContainsKey(name))
					continue;

				RegisterParameter(new DefaultParameter(name, type, defaultValue));
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