using Nox.Avatars;
using Nox.CCK.Avatars.Camera;
using Nox.CCK.Editor;
using Nox.CCK.Language;
using Nox.Search;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nox.CCK.Avatars.Modules.Editor {
	/// <summary>
	/// Inspector of the camera module: the bone the camera is attached to, and the offset added to it.
	/// <para>
	/// The offset can be dragged in the scene (<see cref="OnSceneGUI"/>) while the « Edit » button is on, which is
	/// why the button sits next to the field instead of being a toolbar of its own.
	/// </para>
	/// </summary>
	public class CameraAvatarModuleEditor : LocalizedInspectorEditor<CameraAvatarModule> {
		/// <summary>Prefix of the language entries of this inspector (see the mod lang.asset).</summary>
		private const string LangPrefix = "avatars.modules.camera.inspector";

		/// <summary>Icon of the component: the « videocam » glyph rendered in the assets of the <c>ui</c> mod.</summary>
		protected override ResourceTextureSource Icon
			=> "ui:icons/camera.png";

		protected override string TitleKey
			=> LangPrefix + ".title";

		private IAvatarDescriptor Descriptor
			=> Target.GetComponentInParent<IAvatarDescriptor>();

		/// <summary>Whether the offset handle is drawn in the scene.</summary>
		private bool _editingOffset;

		/// <summary>The button toggling <see cref="_editingOffset"/>; its text is localized.</summary>
		private Button _edit;

		public override VisualElement CreateInspectorGUI() {
			var root = base.CreateInspectorGUI();
			if (Content == null)
				return root;

			// The GUI is rebuilt on every hierarchy change while the base keeps its root: never add twice.
			Content.Clear();

			var uxml = Resources.Load<VisualTreeAsset>(nameof(CameraAvatarModuleEditor));
			if (uxml == null) {
				Content.Add(new Label($"The {nameof(CameraAvatarModuleEditor)} uxml template is not found in Resources."));
				return root;
			}

			var ui = uxml.CloneTree();
			Content.Add(ui);

			// Fields are bound by name: an InspectorElement(serializedObject) would run this very editor again.
			BindField(ui, nameof(CameraAvatarModule.headTransform));
			BindField(ui, nameof(CameraAvatarModule.cameraOffset));

			if (ui.Q<Button>("detect") is { } detect)
				detect.clicked += DetectHead;

			_edit = ui.Q<Button>("edit");
			if (_edit != null) {
				_edit.clicked += ToggleOffsetEditing;
				RefreshEdit();
			}

			return root;
		}

		private void BindField(VisualElement root, string propertyName) {
			var field    = root.Q<PropertyField>(propertyName);
			var property = serializedObject.FindProperty(propertyName);
			if (field != null && property != null)
				field.BindProperty(property);
		}

		/// <summary>Picks the head bone of the animator of the avatar, so it does not have to be dragged in.</summary>
		private void DetectHead() {
			var head = Descriptor?.Animator?.GetBoneTransform(HumanBodyBones.Head);
			if (head == null)
				return;

			var property = serializedObject.FindProperty(nameof(CameraAvatarModule.headTransform));

			serializedObject.Update();
			property.objectReferenceValue = head;
			serializedObject.ApplyModifiedProperties();
		}

		private void ToggleOffsetEditing() {
			_editingOffset = !_editingOffset;

			RefreshEdit();
			SceneView.RepaintAll();
		}

		/// <summary>The button says what it does next: edit, or be done editing.</summary>
		private void RefreshEdit() {
			if (_edit != null)
				_edit.text = LanguageManager.Get(LangPrefix + (_editingOffset ? ".done" : ".edit"));
		}

		/// <summary>Texts the markup cannot localize by itself: the toggle of the offset handle.</summary>
		protected override void LocalizeTexts() {
			base.LocalizeTexts();
			RefreshEdit();
		}

		/// <summary>Handle of the offset in the scene, so it can be placed by looking at the avatar.</summary>
		private void OnSceneGUI() {
			if (!_editingOffset)
				return;

			var module = Target;
			if (!module)
				return;

			var head     = module.headTransform
				? module.headTransform
				: module.transform;
			var origin   = head.position;
			var rotation = head.rotation;

			EditorGUI.BeginChangeCheck();
			var point = Handles.PositionHandle(origin + rotation * module.cameraOffset, rotation);

			if (EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(module, "Change Camera Offset");
				module.cameraOffset = Quaternion.Inverse(rotation) * (point - origin);
				EditorUtility.SetDirty(module);
			}

			Handles.color = Color.cyan;
			Handles.DrawLine(origin, origin + rotation * module.cameraOffset);
		}
	}
}
