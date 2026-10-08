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
		/// <summary>
		/// Base height of the model, in metres and without its scale: what the avatar would measure with a scale
		/// of 1. Measured once at <see cref="Setup"/> on the rest pose (see <see cref="MeasureInitialHeights"/>).
		/// The real height is <c>InitialHeight × Scale</c> (see <see cref="Height"/>).
		/// </summary>
		public float InitialHeight { get; private set; } = 1.7f;

		/// <summary>
		/// Scale the avatar was loaded with, used by <see cref="ScaleModified"/> to tell whether its size has
		/// been changed since. This is <b>not</b> a size (see <see cref="InitialHeight"/>).
		/// </summary>
		public float InitialScale { get; private set; } = 1f;

		/// <summary>Head bone height at rest, without scale (see <see cref="HeadHeight"/>).</summary>
		public float InitialHeadHeight { get; private set; } = 1.5f;

		/// <summary>Eye height at rest, without scale (see <see cref="EyeHeight"/>).</summary>
		public float InitialEyeHeight { get; private set; } = 1.6f;


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

		/// <summary>
		/// Head bone height above the feet, in metres, at the current scale (<c>InitialHeadHeight × Scale</c>).
		/// A property of the model: it does not follow the animation or the trackers (see
		/// <see cref="RealHeadHeight"/>).
		/// </summary>
		public float HeadHeight
			=> InitialHeadHeight * Scale;

		/// <summary>
		/// Eye height above the feet, in metres, at the current scale (<c>InitialEyeHeight × Scale</c>).
		/// A property of the model (see <see cref="RealEyeHeight"/> for the live pose).
		/// <para>
		/// Assigning it resizes the avatar so its eyes land at that height, like <see cref="Height"/> and
		/// <see cref="Scale"/>.
		/// </para>
		/// </summary>
		public float EyeHeight {
			get => InitialEyeHeight * Scale;
			set => Scale = value / InitialEyeHeight;
		}

		#region Real (live) measurements

		// Live measurements: they follow the current pose (animation, IK, trackers).

		/// <summary>
		/// Live height of the avatar, in metres: <c>distance(feet, head bone) + 2 ×
		/// distance(head bone, camera point)</c>, the eye offset being mirrored above the eyes to reach the top
		/// of the skull. Follows the current pose, unlike <see cref="Height"/>.
		/// </summary>
		public float RealHeight
			=> TryMeasure(out var anchor, out var head, out var camera)
				? Vector3.Distance(anchor, head) + 2f * Vector3.Distance(head, camera)
				: InitialHeight;

		/// <summary>Live height of the head bone above the feet, in metres.</summary>
		public float RealHeadHeight
			=> TryMeasure(out var anchor, out var head, out _)
				? Vector3.Distance(anchor, head)
				: 0f;

		/// <summary>Live height of the eyes above the feet, in metres.</summary>
		public float RealEyeHeight
			=> TryMeasure(out var anchor, out _, out var camera)
				? Vector3.Distance(anchor, camera)
				: 0f;

		#endregion

		/// <summary>
		/// Measures the three base heights of the model (total, head bone, eyes), in metres and without scale,
		/// once at <see cref="Setup"/>. They are measured on the skeleton's rest pose (the bind poses of the mesh,
		/// i.e. the file's T-pose), falling back to the live pose when the model exposes no usable bind pose.
		/// </summary>
		private void MeasureInitialHeights(IAvatarDescriptor descriptor) {
			var anchor = descriptor != null && descriptor.Anchor ? descriptor.Anchor.transform : null;

			// The default values are kept when the skeleton is not usable.
			if (!TryMeasureSkeleton(out var head, out var eyeOffset))
				return;

			// The measurement is in world space: divide by the current scale to get the base height.
			var scale = anchor ? Mathf.Abs(anchor.lossyScale.y) : 1f;
			if (scale <= 0.001f)
				scale = 1f;

			var headHeight = anchor ? Vector3.Distance(anchor.position, head) : 0f;
			var eyeLift    = eyeOffset.magnitude;

			InitialHeight     = (headHeight + 2f * eyeLift) / scale;
			InitialHeadHeight = headHeight / scale;
			InitialEyeHeight  = (headHeight + eyeLift) / scale;
		}

		/// <summary>
		/// The two skeleton measurements, in world space: head bone position and head → eye offset. The head is
		/// read on the rest pose (see <see cref="MeasureInitialHeights"/>), falling back to its live position.
		/// </summary>
		private bool TryMeasureSkeleton(out Vector3 headPosition, out Vector3 eyeOffset) {
			headPosition = default;
			eyeOffset    = default;

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

			eyeOffset = cameraModule != null ? cameraModule.GetOffset() : Vector3.zero;
			if (TryRestPosition(descriptor.Anchor, head, out headPosition))
				return true;

			headPosition = head.position;
			return true;
		}

		/// <summary>
		/// Position of <paramref name="bone"/> on the rest pose, in world space, read from the bind poses of the
		/// meshes using it: the inverse of the bind matrix gives the bone position in mesh space, i.e. in the
		/// model's pose. It goes through the renderer transform to stay in the anchor's frame.
		/// </summary>
		private static bool TryRestPosition(GameObject root, Transform bone, out Vector3 position) {
			foreach (var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)) {
				var mesh = skin ? skin.sharedMesh : null;
				if (mesh == null || skin.bones == null)
					continue;

				var bindPoses = mesh.bindposes;
				for (var i = 0; i < skin.bones.Length && i < bindPoses.Length; i++) {
					if (skin.bones[i] != bone)
						continue;

					position = skin.transform.TransformPoint(bindPoses[i].inverse.MultiplyPoint3x4(Vector3.zero));
					return true;
				}
			}

			position = default;
			return false;
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
			MeasureInitialHeights(runtimeAvatar.Descriptor);
			InitialScale = Scale;

			// One triple per measure (initial / current / real), see IScaleAvatarModule.
			_parameters.Clear();
			_parameters.Add(new ScaleEditedParameter(this));
			_parameters.Add(new InitialScaleParameter(this));
			_parameters.Add(new ScaleParameter(this));
			_parameters.Add(new InitialHeightParameter(this));
			_parameters.Add(new HeightParameter(this));
			_parameters.Add(new RealHeightParameter(this));
			_parameters.Add(new InitialHeadHeightParameter(this));
			_parameters.Add(new HeadHeightParameter(this));
			_parameters.Add(new RealHeadHeightParameter(this));
			_parameters.Add(new InitialEyeHeightParameter(this));
			_parameters.Add(new EyeHeightParameter(this));
			_parameters.Add(new RealEyeHeightParameter(this));

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