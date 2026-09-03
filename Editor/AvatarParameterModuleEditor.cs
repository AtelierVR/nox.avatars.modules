using System;
using System.Collections.Generic;
using System.Linq;
using Nox.Avatars.Parameters;
using Nox.CCK.Avatars.Parameters;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Nox.CCK.Avatars.Modules.Editor {
	[CustomEditor(typeof(AvatarParameterModule))]
	public class AvatarParameterModuleEditor : UnityEditor.Editor {
		private AvatarParameterModule _target;
		private VisualElement         _root;
		private Label                 _infoLabel;
		private VisualElement         _container;
		private PropertyField         _property;

		private Dictionary<string, ParameterFieldTracker> _parameterFields = new();
		private bool                                      _isPlaying;

		// Classe pour tracker les champs de paramètres
		private class ParameterFieldTracker {
			public VisualElement Container;
			public VisualElement Field;
			public IParameter    Parameter;
			public object        LastValue;
			public bool          IsFocused;
		}

		public override VisualElement CreateInspectorGUI() {
			_target = (AvatarParameterModule)target;

			// Charger le UXML
			_root = Resources
				.Load<VisualTreeAsset>("AvatarParameterModuleEditor")
				.CloneTree();

			// Récupérer les éléments
			_property   = _root.Q<PropertyField>("parameters-property");
			_infoLabel  = _root.Q<Label>("info-label");
			_container  = _root.Q<VisualElement>("parameters-container");

			// Bind la propriété
			_property.BindProperty(serializedObject.FindProperty("parameters"));

			// Mettre à jour l'affichage initial
			UpdateRuntimeSection();

			// Planifier les mises à jour en mode Play
			if (Application.isPlaying) {
				_isPlaying = true;
				_root.schedule.Execute(UpdateParameterValues).Every(100); // Update toutes les 100ms
			}

			// S'abonner aux changements de mode Play
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

			return _root;
		}

		private void OnDisable() {
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
		}

		private void OnPlayModeStateChanged(PlayModeStateChange state) {
			if (state == PlayModeStateChange.EnteredPlayMode) {
				_isPlaying = true;
				UpdateRuntimeSection();
				_root?.schedule.Execute(UpdateParameterValues).Every(100);
			}
			else if (state == PlayModeStateChange.ExitingPlayMode) {
				_isPlaying = false;
				_parameterFields.Clear();
				UpdateRuntimeSection();
			}
		}

		private void UpdateRuntimeSection() {
			if (!Application.isPlaying) {
				_infoLabel.text  = "Entrez en mode Play pour voir et modifier les valeurs des paramètres.";
				_infoLabel.style.display = DisplayStyle.Flex;
				_container.Clear();
				_parameterFields.Clear();
				return;
			}

			var runtimeParams = _target.GetParameters();

			if (runtimeParams.Length == 0) {
				_infoLabel.text  = "Aucun paramètre trouvé dans l'animateur.";
				_infoLabel.style.display = DisplayStyle.Flex;
				_container.Clear();
				return;
			}

			var animator = _target.Runtime.Descriptor?.Animator;
			if (!animator) {
				_infoLabel.text  = "Aucun animateur trouvé sur l'avatar.";
				_infoLabel.style.display = DisplayStyle.Flex;
				_container.Clear();
				return;
			}

			_infoLabel.text  = $"Paramètres runtime: {runtimeParams.Length}";
			_infoLabel.style.display = DisplayStyle.Flex;

			var isLocal = runtimeParams
					.FirstOrDefault(e => e.GetName().Contains("IsLocal"))
					?.Get().ToBool()
				?? true;

			// Créer les champs pour chaque paramètre
			_container.Clear();
			_parameterFields.Clear();

			foreach (var param in runtimeParams)
				CreateParameterField(param, isLocal);
		}

		private void CreateParameterField(IParameter param, bool isLocal) {
			var container = new VisualElement();
			container.AddToClassList("parameter-field");

			var editable = param.GetFlags().HasFlag(isLocal ? ParameterFlags.OwnerEditable : ParameterFlags.ViewerEditable);
			var field    = CreateFieldForParameter(param, editable);

			if (field != null) {
				container.Add(field);

				if (!editable) {
					var readonlyLabel = new Label("(Lecture seule)");
					readonlyLabel.AddToClassList("readonly-label");
					container.Add(readonlyLabel);
				}

				_container.Add(container);

				// Tracker ce champ
				var tracker = new ParameterFieldTracker {
					Container = container,
					Field     = field,
					Parameter = param,
					LastValue = param.Get(),
					IsFocused = false
				};

				_parameterFields[param.GetName()] = tracker;

				// Détecter le focus pour arrêter les updates
				field.RegisterCallback<FocusInEvent>(evt => {
					if (_parameterFields.TryGetValue(param.GetName(), out var t))
						t.IsFocused = true;
				});

				field.RegisterCallback<FocusOutEvent>(evt => {
					if (_parameterFields.TryGetValue(param.GetName(), out var t))
						t.IsFocused = false;
				});
			}
		}

		private VisualElement CreateFieldForParameter(IParameter param, bool editable) {
			var paramName = param.GetName();
			var type = param.GetValueType();
			var value = param.Get();

			if (type == ParameterType.ByteArray) {
				return ParameterFieldFactory.CreateField(paramName, type, editable, value, bytes => param.Set(bytes));
			}

			var field = ParameterFieldFactory.CreateFieldByType(type, paramName, value, editable);
			ParameterFieldFactory.RegisterCallback(field, type, bytes => param.Set(bytes));
			return field;
		}

		private void UpdateParameterValues() {
			if (!_isPlaying || _parameterFields.Count == 0)
				return;

			foreach (var kvp in _parameterFields) {
				var tracker = kvp.Value;

				// Ne pas mettre à jour si le champ a le focus
				if (tracker.IsFocused)
					continue;

				var currentValue = tracker.Parameter.Get();

				// Mettre à jour uniquement si la valeur a changé
				if (!ValuesAreEqual(tracker.LastValue, currentValue)) {
					ParameterFieldFactory.UpdateFieldValue(tracker.Field, tracker.Parameter.GetValueType(), currentValue);
					tracker.LastValue = currentValue;
				}
			}
		}

		private bool ValuesAreEqual(object val1, object val2) {
			if (val1 == null && val2 == null) return true;
			if (val1 == null || val2 == null) return false;

			if (val1 is Vector3 v1 && val2 is Vector3 v2)
				return v1 == v2;

			if (val1 is Quaternion q1 && val2 is Quaternion q2)
				return q1 == q2;

			if (val1 is byte[] b1 && val2 is byte[] b2) {
				if (b1.Length != b2.Length) return false;
				for (int i = 0; i < b1.Length; i++)
					if (b1[i] != b2[i]) return false;
				return true;
			}

			return Equals(val1, val2);
		}
	}
}