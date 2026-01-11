using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Camera;
using Nox.Avatars.Parameters;
using Nox.Avatars.Scale;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.Scale {
	public class ScaleAvatarModule : MonoBehaviour, IScaleAvatarModule {
		[NonSerialized]
		public float InitialHeight = 1.7f;

		[NonSerialized]
		public float InitialScale = 1f;


		private readonly List<IParameter> _parameters = new();
		private IRuntimeAvatar _runtimeAvatar;

		public int GetPriority()
			=> 1;

		public float Scale {
			get => _runtimeAvatar.GetDescriptor().GetAnchor().transform.localScale.y;
			set => _runtimeAvatar.GetDescriptor().GetAnchor().transform.localScale = new Vector3(value, value, value);
		}

		private float RuntimeHeight() {
			var anchor = _runtimeAvatar.GetDescriptor().GetAnchor().transform;
			var module = _runtimeAvatar.GetDescriptor()
				.GetModules<ICameraModule>()
				.FirstOrDefault();

			Vector3 headWorldPos;
			if (module != null) {
				// Get the head anchor position
				var headAnchor = module.GetAnchor();
				headWorldPos = headAnchor.position;

				// GetOffset() returns cameraOffset * lossyScale.y (as per CameraAvatarModule logic)
				// To get the unscaled offset, we need to divide by lossyScale.y
				var scaledOffset = module.GetOffset();
				var lossyScale = headAnchor.lossyScale.y;

				if (lossyScale > 0.001f) {
					// cameraOffset is calculated in world space (already scaled), then GetOffset() multiplies by lossyScale again
					// So we need to divide by lossyScale² to get the truly unscaled offset
					var unscaledOffset = scaledOffset / (lossyScale * lossyScale);
					// Transform back to world space with current scale
					unscaledOffset *= lossyScale;
					headWorldPos += unscaledOffset;
				}
			}
			else {
				headWorldPos = _runtimeAvatar.GetDescriptor().GetAnimator()
					.GetBoneTransform(HumanBodyBones.Head).position;
			}

			// Calculate height in world space, then divide by current scale to get the base height
			var worldHeight = headWorldPos.y - anchor.position.y;
			return worldHeight / anchor.lossyScale.y;
		}

		public float Height {
			get => InitialHeight * Scale;
			set => Scale = value / InitialHeight;
		}

		public bool ScaleModified {
			get => !Mathf.Approximately(Scale, InitialScale);
			set => Scale = value ? Scale : InitialScale;
		}

		public UniTask<bool> Setup(IRuntimeAvatar runtimeAvatar) {
			_runtimeAvatar = runtimeAvatar;
			InitialHeight = RuntimeHeight();
			InitialScale = Scale;

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