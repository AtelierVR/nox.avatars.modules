using System;
using System.Collections.Generic;
using System.Linq;
using Nox.Avatars.Editor;
using Nox.CCK.Avatars.Playable;
using Nox.CCK.Avatars.StateMachines;
using UnityEditor;
using UnityEngine;

namespace Nox.CCK.Avatars.Modules.Editor {
	/// <summary>
	/// Inspector for <see cref="PlayableLayerControl"/> state behaviors.
	/// Keys are listed as: <c>(None)</c>, the standard keys (in enum order), the keys detected
	/// on the current avatar's <see cref="PlayableAvatarModule"/> (alphabetical, see
	/// <see cref="AvatarDescriptorHelper.CurrentAvatar"/>) and finally <see cref="PlayableLayerNaming.Custom"/>
	/// for a free-form key. Also configures the enter/exit actions with a live summary.
	/// </summary>
	[CustomEditor(typeof(PlayableLayerControl))]
	public class PlayableLayerControlEditor : UnityEditor.Editor {
		private const string NoneOption = "(None)";

		/// <summary>
		/// Keys selectable as-is: the playable layer naming values (in enum declaration order),
		/// excluding <see cref="PlayableLayerNaming.Controller"/> (uses the asset name) and
		/// <see cref="PlayableLayerNaming.Custom"/> (handled by the free-form field).
		/// </summary>
		private static readonly string[] StandardKeys =
			Enum.GetNames(typeof(PlayableLayerNaming))
				.Where(name => name != nameof(PlayableLayerNaming.Controller) && name != nameof(PlayableLayerNaming.Custom))
				.ToArray();

		/// <summary>
		/// Layer keys declared on the current avatar's playable layer module(s), sorted
		/// alphabetically and excluding the standard keys. Empty when no avatar is present.
		/// </summary>
		private static List<string> GetDetectedKeys() {
			var keys = new List<string>();

			var avatar = AvatarDescriptorHelper.CurrentAvatar;
			if (!avatar)
				return keys;

			foreach (var module in avatar.GetComponentsInChildren<PlayableAvatarModule>(true)) {
				if (module.controllers == null)
					continue;

				foreach (var layer in module.controllers) {
					var key = layer?.Key;
					if (!string.IsNullOrEmpty(key) && Array.IndexOf(StandardKeys, key) < 0 && !keys.Contains(key))
						keys.Add(key);
				}
			}

			keys.Sort(StringComparer.OrdinalIgnoreCase);
			return keys;
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();

			DrawTarget();

			EditorGUILayout.Space();
			DrawPhase(
				"On Enter",
				serializedObject.FindProperty(nameof(PlayableLayerControl.onEnter)),
				serializedObject.FindProperty(nameof(PlayableLayerControl.enterWeight)),
				serializedObject.FindProperty(nameof(PlayableLayerControl.enterBlendDuration)));

			EditorGUILayout.Space();
			DrawPhase(
				"On Exit",
				serializedObject.FindProperty(nameof(PlayableLayerControl.onExit)),
				serializedObject.FindProperty(nameof(PlayableLayerControl.exitWeight)),
				serializedObject.FindProperty(nameof(PlayableLayerControl.exitBlendDuration)));

			EditorGUILayout.Space();
			DrawSummary();

			serializedObject.ApplyModifiedProperties();
		}

		private void DrawTarget() {
			var keyProp    = serializedObject.FindProperty(nameof(PlayableLayerControl.layerKey));
			var customProp = serializedObject.FindProperty(nameof(PlayableLayerControl.customKey));
			var current    = keyProp.stringValue ?? string.Empty;

			var detected = GetDetectedKeys();

			EditorGUILayout.LabelField("Target Layer", EditorStyles.boldLabel);
			EditorGUI.indentLevel++;

			// (None) → standard keys (enum order) → detected keys (alphabetical) → Custom
			var options = new List<string> { NoneOption };
			options.AddRange(StandardKeys);
			options.AddRange(detected);
			options.Add(nameof(PlayableLayerNaming.Custom));

			var customIndex = options.Count - 1;
			var foundIndex  = options.IndexOf(current);

			// The Custom option stays selected thanks to the editor flag (kept even with an empty key).
			var currentIndex = customProp.boolValue
				? customIndex
				: string.IsNullOrEmpty(current)
					? 0
					: foundIndex > 0
						? foundIndex
						: customIndex;

			var newIndex = EditorGUILayout.Popup("Key", currentIndex, options.ToArray());
			if (newIndex != currentIndex) {
				if (newIndex == 0) {
					keyProp.stringValue   = string.Empty;
					customProp.boolValue  = false;
				} else if (newIndex == customIndex) {
					keyProp.stringValue   = string.Empty;
					customProp.boolValue  = true;
				} else {
					keyProp.stringValue   = options[newIndex];
					customProp.boolValue  = false;
				}
			}

			// Show the free-form field when the Custom option is selected or the key isn't listed.
			var value         = keyProp.stringValue;
			var editingCustom = newIndex == customIndex;
			var isKnown       = !string.IsNullOrEmpty(value) && options.IndexOf(value) > 0 && options.IndexOf(value) != customIndex;
			if (editingCustom || (!isKnown && !string.IsNullOrEmpty(value)))
				keyProp.stringValue = EditorGUILayout.TextField("Custom Key", value);

			if (string.IsNullOrEmpty(keyProp.stringValue))
				EditorGUILayout.HelpBox("No layer key set: this behavior will do nothing.", MessageType.Warning);
			else if (detected.Count == 0)
				EditorGUILayout.HelpBox("No playable layer detected on the current avatar. Standard keys are still available; select your avatar or configure its PlayableAvatarModule to see its own keys.", MessageType.Info);

			EditorGUI.indentLevel--;
		}

		private static void DrawPhase(string title, SerializedProperty actionProp, SerializedProperty weightProp, SerializedProperty blendProp) {
			EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;

			EditorGUILayout.PropertyField(actionProp, new GUIContent("Action"));

			var action = (PlayableLayerAction)actionProp.enumValueIndex;

			if (action == PlayableLayerAction.SetWeight)
				EditorGUILayout.PropertyField(weightProp, new GUIContent("Goal Weight"));

			if (action != PlayableLayerAction.None)
				EditorGUILayout.PropertyField(blendProp, new GUIContent("Blend Duration"));

			EditorGUI.indentLevel--;
		}

		private void DrawSummary() {
			var control = (PlayableLayerControl)target;
			var key     = string.IsNullOrEmpty(control.layerKey) ? "<none>" : control.layerKey;
			var enter   = Describe(control.onEnter, control.enterWeight, control.enterBlendDuration);
			var exit    = Describe(control.onExit, control.exitWeight, control.exitBlendDuration);

			EditorGUILayout.HelpBox(
				$"Layer '{key}'\nEnter: {enter}\nExit:  {exit}",
				MessageType.Info);
		}

		private static string Describe(PlayableLayerAction action, float weight, float blend) {
			if (action == PlayableLayerAction.None)
				return "no change";

			var goal = action switch {
				PlayableLayerAction.Start => "weight 1",
				PlayableLayerAction.Stop  => "weight 0",
				_                         => $"weight {weight:0.##}"
			};

			return blend <= 0f
				? $"{action} ({goal}, instant)"
				: $"{action} ({goal}, {blend:0.##}s)";
		}
	}
}
