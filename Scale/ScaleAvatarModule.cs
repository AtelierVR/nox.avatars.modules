using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Camera;
using Nox.Avatars.Parameters;
using Nox.Avatars.Scale;
using Nox.CCK.Development;
using UnityEngine;
using Gizmos = Nox.CCK.Development.Gizmos;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.Scale {
	[Gizmos("cck.avatars.scale")]
	public class ScaleAvatarModule : MonoBehaviour, IScaleAvatarModule, IGizmos {
		[NonSerialized]
		public float InitialHeight = 1.7f;

		[NonSerialized]
		public float InitialScale = 1f;


		private readonly List<IParameter> _parameters    = new();
		private          IRuntimeAvatar   _runtimeAvatar;
		private          IParameterModule _parameterModule;

		// Per-frame measurement cache: the anchor, head bone and camera points, shared by every
		// live parameter and by the gizmo so the rig is only resolved once per frame.
		private int     _measureFrame = -1;
		private bool    _measureValid;
		private Vector3 _anchorPosition;
		private Vector3 _headPosition;
		private Vector3 _cameraPosition;

		public int Priority
			=> 60;

		/// <summary>
		/// Effective (world) scale of the avatar, i.e. the anchor's <see cref="Transform.lossyScale"/>.
		/// This is the synced value: unlike a live height measurement it is stable (independent of the
		/// current animation pose) and already accounts for any scale applied by the parent rig.
		/// Assigning it rescales the avatar so its effective scale matches the requested value; the parent
		/// scale is divided out because <c>localScale</c> is parent-relative.
		/// </summary>
		public float Scale {
			get => _runtimeAvatar.Descriptor.Anchor.transform.lossyScale.y;
			set {
				var anchor      = _runtimeAvatar.Descriptor.Anchor.transform;
				var parentScale = anchor.parent ? anchor.parent.lossyScale.y : 1f;
				var local       = Mathf.Abs(parentScale) > 0.001f ? value / parentScale : value;
				anchor.localScale = new Vector3(local, local, local);
			}
		}

		/// <summary>
		/// Resolves the three points used to measure the avatar: the anchor (feet), the head bone and
		/// the camera/eye point. The head bone comes from the camera module (or the animator head bone
		/// as a fallback) and the camera point is the camera module anchor plus its offset
		/// (see <see cref="ICameraModule"/>).
		/// Resolved once per frame so every caller can query it cheaply.
		/// </summary>
		private bool TryMeasure(out Vector3 anchorPosition, out Vector3 headPosition, out Vector3 cameraPosition) {
			if (_measureFrame != Time.frameCount) {
				_measureFrame = Time.frameCount;
				_measureValid = ResolveMeasure(out _anchorPosition, out _headPosition, out _cameraPosition);
			}

			anchorPosition = _anchorPosition;
			headPosition   = _headPosition;
			cameraPosition = _cameraPosition;
			return _measureValid;
		}

		private bool ResolveMeasure(out Vector3 anchorPosition, out Vector3 headPosition, out Vector3 cameraPosition) {
			anchorPosition = default;
			headPosition   = default;
			cameraPosition = default;

			var descriptor = _runtimeAvatar?.Descriptor ?? GetComponentInParent<IAvatarDescriptor>();
			if (descriptor == null || !descriptor.Anchor)
				return false;

			var cameraModule = descriptor.GetModules<ICameraModule>().FirstOrDefault()
				?? descriptor.Anchor.GetComponentsInChildren<ICameraModule>(true).FirstOrDefault();

			var head = cameraModule?.GetAnchor();
			if (!head && descriptor.Animator)
				head = descriptor.Animator.GetBoneTransform(HumanBodyBones.Head);
			if (!head)
				return false;

			anchorPosition = descriptor.Anchor.transform.position;
			headPosition   = head.position;
			cameraPosition = cameraModule != null
				? headPosition + cameraModule.GetOffset()
				: headPosition;

			return true;
		}

		/// <summary>Live distance from the anchor (feet) to the head bone.</summary>
		public float HeadHeight
			=> TryMeasure(out var anchor, out var head, out _)
				? Vector3.Distance(anchor, head)
				: 0f;

		/// <summary>Live distance from the anchor (feet) to the camera (eye) point.</summary>
		public float EyeHeight
			=> TryMeasure(out var anchor, out _, out var camera)
				? Vector3.Distance(anchor, camera)
				: 0f;

		/// <summary>
		/// Live avatar height, estimated from the anchor (feet) as
		/// <c>distance(anchor, head bone) + 2 * distance(head bone, camera)</c>:
		/// the head bone -> camera distance is mirrored past the eyes to reach the top of the skull.
		/// </summary>
		public float MeasuredHeight
			=> TryMeasure(out var anchor, out var head, out var camera)
				? Vector3.Distance(anchor, head) + 2f * Vector3.Distance(head, camera)
				: InitialHeight;

		private float RuntimeHeight() {
			if (!TryMeasure(out _, out _, out _))
				return InitialHeight;

			// The measurement is in world space, divide by the current scale to get the base height
			var anchor     = _runtimeAvatar.Descriptor.Anchor.transform;
			var lossyScale = anchor.lossyScale.y;
			return lossyScale > 0.001f ? MeasuredHeight / lossyScale : MeasuredHeight;
		}

		public void OnDrawGizmos() {
			var descriptor = _runtimeAvatar?.Descriptor ?? GetComponentInParent<IAvatarDescriptor>();
			var anchor     = descriptor != null && descriptor.Anchor ? descriptor.Anchor.transform : transform;

			if (!TryMeasure(out var anchorPosition, out var headPosition, out var cameraPosition)) {
				Gizmos.Color = Color.orange;
				Gizmos.DrawLabel(transform.position, $"{nameof(ScaleAvatarModule)}: no head reference found.");
				return;
			}

			var headDistance   = Vector3.Distance(anchorPosition, headPosition);
			var eyeDistance    = Vector3.Distance(anchorPosition, cameraPosition);
			var cameraDistance = Vector3.Distance(headPosition, cameraPosition);
			var worldHeight    = headDistance + 2f * cameraDistance;

			var top        = anchorPosition + Vector3.up * worldHeight;
			var lossyScale = anchor.lossyScale.y;
			var radius     = Mathf.Max(0.05f, Mathf.Abs(worldHeight) * 0.03f);

			// Anchor (feet) reference
			Gizmos.Color = Color.cyan;
			Gizmos.DrawWireDisc(anchorPosition, Vector3.up, radius);

			// Anchor -> head bone
			Gizmos.Color = Color.yellow;
			Gizmos.DrawLine(anchorPosition, headPosition);
			Gizmos.DrawWireSphere(headPosition, radius * 0.5f);
			Gizmos.DrawLabel(headPosition, $"Head bone ({headDistance:0.00} m)");

			// Head bone -> camera, mirrored past the camera to estimate the top of the skull
			Gizmos.Color = Color.magenta;
			Gizmos.DrawLine(headPosition, cameraPosition);
			Gizmos.DrawWireSphere(cameraPosition, radius * 0.35f);
			Gizmos.DrawDottedLine(headPosition, headPosition + Vector3.up * (2f * cameraDistance));
			Gizmos.DrawLabel(cameraPosition, $"Camera ({eyeDistance:0.00} m)");

			// Estimated height
			Gizmos.Color = Color.white;
			Gizmos.DrawLine(anchorPosition, top);
			Gizmos.DrawWireDisc(top, Vector3.up, radius * 0.5f);

			// Base (unscaled) height reference
			if (lossyScale > 0.001f && !Mathf.Approximately(lossyScale, 1f)) {
				var baseHeight = worldHeight / lossyScale;
				var baseTop    = anchorPosition + Vector3.up * baseHeight;
				Gizmos.Color = new Color(0.6f, 0.6f, 0.6f, 0.85f);
				Gizmos.DrawDottedLine(anchorPosition, baseTop);
				Gizmos.DrawLabel(baseTop, $"Base height ({baseHeight:0.00} m)");
			}

			Gizmos.Color = Color.white;
			Gizmos.DrawLabel(top + Vector3.up * radius, $"Height: {worldHeight:0.00} m\nScale: {lossyScale:0.00}×");
		}

		public float Height {
			get => InitialHeight * Scale;
			set => Scale = value / InitialHeight;
		}

		public bool ScaleModified {
			get => !Mathf.Approximately(Scale, InitialScale);
			set => Scale = value ? Scale : InitialScale;
		}

		public UniTask<bool> Setup(IRuntimeAvatar runtimeAvatar, AvatarModulePhase phase, CancellationToken token = default) {
			if (phase != AvatarModulePhase.Init) return UniTask.FromResult(true);
			_runtimeAvatar = runtimeAvatar;
			InitialHeight = RuntimeHeight();
			InitialScale = Scale;

			// Add parameters
			_parameters.Clear();
			_parameters.Add(new ScaleEditedParameter(this));
			_parameters.Add(new ScaleParameter(this));
			_parameters.Add(new HeightParameter(this));
			_parameters.Add(new EyeHeightParameter(this));
			_parameters.Add(new HeadHeightParameter(this));

			_parameterModule = runtimeAvatar.Descriptor.GetModules<IParameterModule>().FirstOrDefault();
			foreach (var p in _parameters)
				_parameterModule?.RegisterParameter(p);

			return UniTask.FromResult(true);
		}

		private void OnDestroy() {
			foreach (var p in _parameters)
				_parameterModule?.UnregisterParameter(p);
		}

		public IParameter[] GetParameters()
			=> _parameters.ToArray();

		public IParameter GetParameter(string n)
			=> _parameters.FirstOrDefault(p => p.Name == n);

		public IParameter GetParameter(int hash)
			=> _parameters.FirstOrDefault(p => p.Key == hash);

		public static bool Check(IAvatarDescriptor descriptor) {
			var modules = descriptor.GetModules<ScaleAvatarModule>();

			var module = modules.Length switch {
				1 => modules.FirstOrDefault(),
				0 => descriptor.Anchor.AddComponent<ScaleAvatarModule>(),
				_ => null
			};

			if (module)
				return true;

			var cameraModules = descriptor.GetModules<ICameraModule>();
			if (cameraModules.Length == 0) {
				Logger.LogError($"{nameof(ScaleAvatarModule)} requires {nameof(ICameraModule)} to be present on the avatar.", descriptor.Anchor);
				return false;
			}

			Logger.LogError($"Verify that the Avatar prefab has a valid {nameof(ScaleAvatarModule)} component.", descriptor.Anchor);
			return false;
		}
	}
}