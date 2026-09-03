using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using Nox.Avatars.Parameters;
using Nox.CCK.Avatars.Parameters;

namespace Nox.CCK.Avatars.Modules.Editor {
	[CustomEditor(typeof(AvatarParameters))]
	public class AvatarParametersEditor : UnityEditor.Editor {
		private AvatarParameters Target
			=> (AvatarParameters)target;

		private MultiColumnListView  _listView;
		private List<ParameterEntry> _parametersList;

		public override VisualElement CreateInspectorGUI() {
			var root = new VisualElement();
			root.styleSheets.Add(Resources.Load<StyleSheet>("AvatarParametersEditor"));

			// Initialize parameters list
			_parametersList = new List<ParameterEntry>();
			if (Target.parameters != null)
				_parametersList.AddRange(Target.parameters);

			// Create MultiColumnListView
			CreateMultiColumnListView();
			root.Add(_listView);

			return root;
		}

		private void CreateMultiColumnListView() {
			// Create columns collection
			var columns = new Columns {
				// Name column — largest, stretches to fill remaining space
				new Column {
					name        = "name",
					title       = "Name",
					stretchable = true,
					minWidth    = 160,
					sortable    = true,
					makeCell    = () => MakeContainer(new TextField()),
					bindCell = (element, index) => {
						var textField = element.Q<TextField>();
						if (textField == null || index < 0 || index >= _parametersList.Count || _parametersList[index] == null) return;
						textField.value = _parametersList[index].name ?? "";
						textField.UnregisterValueChangedCallback(OnNameChanged);
						textField.userData = index;
						textField.RegisterValueChangedCallback(OnNameChanged);
					}
				},
				// Type column
				new Column {
					name     = "type",
					title    = "Type",
					width    = 100,
					minWidth = 80,
					sortable = true,
					makeCell = () => MakeContainer(new EnumField()),
					bindCell = (element, index) => {
						var enumField = element.Q<EnumField>();
						if (enumField == null || index < 0 || index >= _parametersList.Count || _parametersList[index] == null) return;
						enumField.Init(ParameterType.Bool);
						enumField.value = _parametersList[index].type;
						enumField.UnregisterValueChangedCallback(OnTypeChanged);
						enumField.userData = index;
						enumField.RegisterValueChangedCallback(OnTypeChanged);
					}
				},
				// Default Value column
				new Column {
					name        = "defaultValue",
					title       = "Default",
					width       = 110,
					minWidth    = 80,
					stretchable = true,
					makeCell = () => MakeContainer(CreateDefaultValueControl()),
					bindCell = (element, index) => {
						if (index < 0 || index >= _parametersList.Count || _parametersList[index] == null) return;
						BindDefaultValueControl(element, index);
					}
				},
				// Flags column — replaces the simple Synced bool with full ParameterFlags
				new Column {
					name     = "flags",
					title    = "Flags",
					width    = 170,
					minWidth = 130,
					sortable = true,
					makeCell = () => MakeContainer(new EnumFlagsField(ParameterFlags.None)),
					bindCell = (element, index) => {
						var enumFlags = element.Q<EnumFlagsField>();
						if (enumFlags == null || index < 0 || index >= _parametersList.Count || _parametersList[index] == null) return;
						enumFlags.value = _parametersList[index].flags;
						enumFlags.UnregisterValueChangedCallback(OnFlagsChanged);
						enumFlags.userData = index;
						enumFlags.RegisterValueChangedCallback(OnFlagsChanged);
					}
				}
			};

			_listView = new MultiColumnListView(columns) {
				fixedItemHeight               = 25,
				itemsSource                   = _parametersList,
				showAlternatingRowBackgrounds = AlternatingRowBackground.All,
				showBorder                    = true,
				showFoldoutHeader             = true,
				showAddRemoveFooter           = true,
				headerTitle                   = "Parameters",
				sortingMode                   = ColumnSortingMode.Default,
				reorderable                   = true,
				reorderMode                   = ListViewReorderMode.Animated,
				style = {
					flexGrow  = 1,
					marginTop = 5
				},
				onAdd    = _ => OnAddParameter(),
				onRemove = OnRemoveParameter
			};
			
			// Register callback for item reordering
			_listView.itemsSourceChanged += SaveChangesInternal;
		}

		private static VisualElement MakeContainer(VisualElement element) {
			var container = new VisualElement {
				style = {
					flexDirection = FlexDirection.Row,
					alignItems    = Align.Center,
					marginRight   = 4,
					marginTop     = 2,
				}
			};
			element.style.flexGrow = 1;
			container.Add(element);
			return container;
		}

		// Separate callback methods to avoid closure issues
		private void OnNameChanged(ChangeEvent<string> evt) {
			if (evt.target is not TextField { userData: int index } || index >= _parametersList.Count) return;
			_parametersList[index].name = evt.newValue;
			SaveChangesInternal();
		}

		private void OnTypeChanged(ChangeEvent<Enum> evt) {
			if (evt.target is not EnumField { userData: int index } || index >= _parametersList.Count) return;
			var oldType = _parametersList[index].type;
			var newType = (ParameterType)evt.newValue;

			_parametersList[index].type = newType;

			if (oldType != newType) {
				_parametersList[index].defaultValue = string.Empty;
				_listView.RefreshItems();
			}

			SaveChangesInternal();
		}

		private void OnFlagsChanged(ChangeEvent<Enum> evt) {
			if (evt.target is not EnumFlagsField { userData: int index } || index >= _parametersList.Count) return;
			var flags = (ParameterFlags)evt.newValue;
			_parametersList[index].flags  = flags;
			_parametersList[index].synced = (flags & ParameterFlags.OwnerSyncsToViewers) != 0;
			SaveChangesInternal();
		}

		// Callbacks pour les différents types de contrôles
		private void OnBoolValueChanged(ChangeEvent<bool> evt) {
			if (evt.target is not VisualElement { userData: int index } || index >= _parametersList.Count) return;
			_parametersList[index].SetDefaultValue(evt.newValue);
			SaveChangesInternal();
		}

		private void OnStringValueChanged(ChangeEvent<string> evt) {
			if (evt.target is not VisualElement { userData: int index } || index >= _parametersList.Count) return;
			_parametersList[index].SetDefaultValue(evt.newValue ?? "");
			SaveChangesInternal();
		}

		// Callbacks pour ajouter et supprimer des paramètres
		private void OnAddParameter() {
			var newParameter = new ParameterEntry {
				name         = "New Parameter",
				type         = ParameterType.Bool,
				defaultValue = string.Empty,
				synced       = true,
				flags        = ParameterFlags.OwnerBroadcast
			};

			_parametersList.Add(newParameter);
			_listView.RefreshItems();
			SaveChangesInternal();
		}

		private void OnRemoveParameter(BaseListView listView) {
			var selectedIndices = listView.selectedIndices.ToList();
			if (selectedIndices.Count == 0) return;

			// Trier les indices en ordre décroissant pour éviter les problèmes d'index lors de la suppression
			selectedIndices.Sort((a, b) => b.CompareTo(a));

			foreach (var index in selectedIndices.Where(index => index >= 0 && index < _parametersList.Count)) 
				_parametersList.RemoveAt(index);

			_listView.RefreshItems();
			SaveChangesInternal();
		}

		private void SaveChangesInternal() {
			Target.parameters = _parametersList.ToArray();
			EditorUtility.SetDirty(Target);
		}

		private static VisualElement CreateDefaultValueControl()
			=> new() {
				style = {
					flexDirection = FlexDirection.Row,
					alignItems    = Align.Center
				}
			};


		private void BindDefaultValueControl(VisualElement element, int index) {
			var parameter = _parametersList[index];
			element.Clear();

			var control = ParameterFieldFactory.CreateFieldForParameterEntry(parameter, index, (Nox.CCK.Avatars.Parameters.ParameterEntry p, object val) => {
				p.SetDefaultValue(val);
				SaveChangesInternal();
			});

			var label = control.Q<VisualElement>(null, "unity-base-field__label");
			if (label != null) 
				label.style.display = DisplayStyle.None;
			var field = control.Q<VisualElement>(null, "unity-base-field");
			if (field != null) 
				field.style.flexGrow = 1;

			element.Add(control);
		}

		private void OnValueChanged(ChangeEvent<object> evt) {
			if (evt.target is not VisualElement { userData: int index } || index >= _parametersList.Count) return;
			var parameter = _parametersList[index];
			try {
				parameter.SetDefaultValue(evt.newValue);
				SaveChangesInternal();
			} catch (Exception ex) {
				Debug.LogWarning($"Failed to set value {evt.newValue} for type {parameter.type}: {ex.Message}");
			}
		}
	}
}
