using System;
using System.Collections.Generic;
using System.Linq;
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
				0 => descriptor.GetAnchor().AddComponent<AvatarParameterModule>(),
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
		private          RuntimeAnimatorController     _lastController;
		private          bool                          _populated;

		public IParameter[] Parameters { get; private set; } = Array.Empty<IParameter>();

		public int GetPriority()
			=> 100;

		public async UniTask<bool> Setup(IRuntimeAvatar runtimeAvatar) {
			await UniTask.Yield();
			Runtime    = runtimeAvatar;
			_populated = false;
			return true;
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
			var animator = Runtime?.Descriptor?.GetAnimator();
			if (!animator || !animator.runtimeAnimatorController || !animator.playableGraph.IsValid()) return;

			_lastController = animator.runtimeAnimatorController;
			var entries = parameters?.parameters ?? Array.Empty<ParameterEntry>();

			// Paramètres de l'Animator
			foreach (var parameter in animator.parameters) {
				var entry = entries.FirstOrDefault(e => e.GetNameHash() == parameter.nameHash);
				RegisterParameter(new AnimatorBaseParameter { Animator = animator, Parameter = parameter, Entry = entry });
			}

			// Paramètres des contrôleurs du playable graph
			foreach (var controller in animator.GetControllers()) {
				for (var i = 0; i < controller.GetParameterCount(); i++) {
					var cp    = controller.GetParameter(i);
					var entry = entries.FirstOrDefault(e => e.GetNameHash() == cp.nameHash);
					RegisterParameter(new PlayableBaseParameter { Animator = animator, Controller = controller, Parameter = cp, Entry = entry });
				}
			}

			_populated = true;
		}

		private void RepopulateAnimatorParameters() {
			var animator = Runtime?.Descriptor?.GetAnimator();
			if (!animator) return;

			_lastController = animator.runtimeAnimatorController;
			_history.Clear();

			// Retirer les anciens paramètres liés à l'animator/playable
			foreach (var p in _paramList.OfType<AnimatorBaseParameter>().ToList())
				UnregisterParameter(p);
			foreach (var p in _paramList.OfType<PlayableBaseParameter>().ToList())
				UnregisterParameter(p);

			var entries = parameters?.parameters ?? Array.Empty<ParameterEntry>();

			foreach (var parameter in animator.parameters) {
				var entry = entries.FirstOrDefault(e => e.GetNameHash() == parameter.nameHash);
				RegisterParameter(new AnimatorBaseParameter { Animator = animator, Parameter = parameter, Entry = entry });
			}

			foreach (var controller in animator.GetControllers()) {
				for (var i = 0; i < controller.GetParameterCount(); i++) {
					var cp    = controller.GetParameter(i);
					var entry = entries.FirstOrDefault(e => e.GetNameHash() == cp.nameHash);
					RegisterParameter(new PlayableBaseParameter { Animator = animator, Controller = controller, Parameter = cp, Entry = entry });
				}
			}
		}

		public IParameter[] GetParameters()
			=> Parameters;

		public IParameter GetParameter(string n)    
			=> _byName.GetValueOrDefault(n);

		public IParameter GetParameter(int    hash) 
			=> _byHash.GetValueOrDefault(hash);

		public void Update() {
			var animator = Runtime?.Descriptor?.GetAnimator();
			if (!animator || !animator.runtimeAnimatorController) return;

			// Populate dès que le PlayableGraph est prêt
			if (!_populated) {
				if (animator.playableGraph.IsValid())
					PopulateParameters();
				return;
			}

			// Si le controller a changé, reconstruire les paramètres animator/playable
			if (_lastController != animator.runtimeAnimatorController)
				RepopulateAnimatorParameters();
		}
	}
}