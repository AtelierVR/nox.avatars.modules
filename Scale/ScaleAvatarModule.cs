using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Camera;
using Nox.Avatars.Parameters;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.Scale {
	public class ScaleAvatarModule : MonoBehaviour, IAvatarModule, IParameterGroup {
		[NonSerialized]
		public float InitialHeight = 1.7f;

		[NonSerialized]
		public float InitialScale = 1f;


		private readonly List<IParameter> _parameters = new();
		private          IRuntimeAvatar   _runtimeAvatar;

		public int GetPriority()
			=> 1;

		public float Scale {
			get => _runtimeAvatar.GetDescriptor().GetAnchor().transform.localScale.y;
			set => _runtimeAvatar.GetDescriptor().GetAnchor().transform.localScale = new Vector3(value, value, value);
		}

		private float RuntimeHeight() {
			var cameraModule = _runtimeAvatar.GetDescriptor()
				.GetModules<ICameraModule>()
				.FirstOrDefault();
			var head = cameraModule != null
				? cameraModule.GetAnchor().position + cameraModule.GetOffset()
				: _runtimeAvatar.GetDescriptor().GetAnimator().GetBoneTransform(HumanBodyBones.Head).position;
			var anchor = _runtimeAvatar.GetDescriptor().GetAnchor().transform;
			return anchor.InverseTransformPoint(head).y;
		}

		public float Height {
			get => InitialHeight                 * Scale / InitialScale;
			set => Scale = value / InitialHeight * InitialScale;
		}

		public bool ScaleModified {
			get => !Mathf.Approximately(Scale, InitialScale);
			set => Scale = value ? Scale : InitialScale;
		}

		public UniTask<bool> Setup(IRuntimeAvatar runtimeAvatar) {
			_runtimeAvatar = runtimeAvatar;
			InitialHeight  = RuntimeHeight();
			InitialScale   = Scale;

			// Add parameters
			_parameters.Clear();
			_parameters.Add(new ScaleEditedParameter(this));
			_parameters.Add(new ScaleParameter(this));
			_parameters.Add(new EyeHeightParameter(this));

			return UniTask.FromResult(true);
		}

		public IParameter[] GetParameters()
			=> _parameters.ToArray();

		public IParameter GetParameter(string n)
			=> _parameters.FirstOrDefault(p => p.GetName() == n);

		public IParameter GetParameter(int hash)
			=> _parameters.FirstOrDefault(p => p.GetKey() == hash);

		public static bool Check(IAvatarDescriptor descriptor) {
			var modules = descriptor.GetModules<ScaleAvatarModule>();

			var module = modules.Length switch {
				1 => modules.FirstOrDefault(),
				0 => descriptor.GetAnchor().AddComponent<ScaleAvatarModule>(),
				_ => null
			};

			if (module)
				return true;

			var cameraModules = descriptor.GetModules<ICameraModule>();
			if (cameraModules.Length == 0) {
				Logger.LogError($"{nameof(ScaleAvatarModule)} requires {nameof(ICameraModule)} to be present on the avatar.", descriptor.GetAnchor());
				return false;
			}

			Logger.LogError($"Verify that the Avatar prefab has a valid {nameof(ScaleAvatarModule)} component.", descriptor.GetAnchor());
			return false;
		}
	}
}