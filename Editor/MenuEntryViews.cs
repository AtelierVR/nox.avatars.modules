using System;
using System.Collections.Generic;
using System.Linq;
using Nox.Avatars.Parameters;
using Nox.CCK.Avatars.Menus;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
using UnityEngine.UIElements;

namespace Nox.CCK.Avatars.Modules.Editor
{
	/// <summary>
	/// Construit les vues d'édition typées d'un <see cref="MenuEntry"/> selon son
	/// <see cref="EntryType"/>. Les valeurs restent stockées en <c>byte[]</c>
	/// (<see cref="MenuEntry.values"/>) : les champs typés les encodent/décodent via
	/// <see cref="Converter"/>, avec Undo sur l'asset.
	/// </summary>
	public static class MenuEntryViews
	{
		// ------------------------------------------------------------------ types

		private static IReadOnlyList<ParameterType> AllowedTypes(EntryType type)
			=> ParameterTypeCompatibility.GetCompatibleTypes(type);

		private static ParameterType DefaultType(EntryType type)
		{
			var allowed = AllowedTypes(type);
			return allowed.Count > 0 ? allowed[0] : ParameterType.Float;
		}

		// ----------------------------------------------------------- accès objet

		private static MenuEntry Resolve(SerializedProperty p)
		{
			var so = p?.serializedObject;
			if (so == null || so.targetObject is not AvatarMenu menu || menu.entries == null)
				return null;

			const string token = "Array.data[";
			var open = p.propertyPath.IndexOf(token, StringComparison.Ordinal);
			if (open < 0)
				return null;
			var close = p.propertyPath.IndexOf(']', open);
			if (close < 0)
				return null;
			var start = open + token.Length;
			if (!int.TryParse(p.propertyPath[start..close], out var idx))
				return null;

			return idx >= 0 && idx < menu.entries.Length ? menu.entries[idx] : null;
		}

		private static AvatarMenu AssetOf(SerializedProperty p)
			=> p?.serializedObject?.targetObject as AvatarMenu;

		private static void Record(SerializedProperty p, string label)
		{
			var asset = AssetOf(p);
			if (asset == null)
				return;
			Undo.RecordObject(asset, label);
			EditorUtility.SetDirty(asset);
		}

		private static byte[] GetBytes(SerializedProperty p, int slot)
			=> Converter.FromBase64(Resolve(p)?.values is { } values && slot >= 0 && slot < values.Length ? values[slot] : null);

		private static void EnsureSlots(MenuEntry e, int count)
		{
			if (e == null)
				return;
			if (e.values == null)
			{
				e.values = new string[count];
				return;
			}
			if (e.values.Length < count)
			{
				var bigger = new string[count];
				Array.Copy(e.values, bigger, e.values.Length);
				e.values = bigger;
			}
		}

		private static void WriteBytes(SerializedProperty p, int slot, byte[] bytes)
		{
			var e = Resolve(p);
			if (e == null)
				return;
			Record(p, "Edit entry value");
			EnsureSlots(e, slot + 1);
			e.values[slot] = Converter.ToBase64(bytes);
			p.serializedObject.Update();
		}

		private static void WriteParameterType(SerializedProperty p, ParameterType type)
		{
			var e = Resolve(p);
			if (e == null || e.parameterType == type)
				return;
			Record(p, "Change entry parameter type");
			e.parameterType = type;
			p.serializedObject.Update();
		}

		private static void WriteParameter(SerializedProperty p, string parameter)
		{
			var e = Resolve(p);
			if (e == null || e.parameter == parameter)
				return;
			Record(p, "Change entry parameter");
			e.parameter = parameter;
			p.serializedObject.Update();
		}

		// ------------------------------------------------------- encodage/décodage

		private static object Decode(byte[] bytes, ParameterType pt)
		{
			if (bytes == null)
				return pt switch
				{
					ParameterType.Bool => false,
					ParameterType.Byte => (byte)0,
					ParameterType.Short => (short)0,
					ParameterType.UShort => (ushort)0,
					ParameterType.Int => 0,
					ParameterType.UInt => (uint)0,
					ParameterType.Long => 0L,
					ParameterType.ULong => (ulong)0,
					ParameterType.Float => 0f,
					ParameterType.Double => 0d,
					ParameterType.String => string.Empty,
					_ => null
				};
			return pt switch
			{
				ParameterType.Bool => bytes.ToBool(),
				ParameterType.Byte => bytes.ToByte(),
				ParameterType.Short => bytes.ToShort(),
				ParameterType.UShort => bytes.ToUShort(),
				ParameterType.Int => bytes.ToInt(),
				ParameterType.UInt => bytes.ToUInt(),
				ParameterType.Long => bytes.ToLong(),
				ParameterType.ULong => bytes.ToULong(),
				ParameterType.Float => bytes.ToFloat(),
				ParameterType.Double => bytes.ToDouble(),
				ParameterType.String => Converter.ToString(bytes),
				_ => null
			};
		}
		private static byte[] Encode(object value, ParameterType pt)
			=> value?.ToBytes() ?? Array.Empty<byte>();

		// ------------------------------------------------------------ point d'entrée

		public static VisualElement CreateTypeView(EntryType type, SerializedProperty property)
			=> type switch
			{
				EntryType.Menu => CreateMenuView(property),
				EntryType.ColorPicker => CreateParameterOnlyView(property, type),
				_ => CreateTypedView(property, type)
			};

		// --------------------------------------------------------------- Menu (sous-menu)

		private static VisualElement CreateMenuView(SerializedProperty property)
		{
			var container = new VisualElement();
			var menuProp = property.FindPropertyRelative(nameof(MenuEntry.menu));
			var menuField = new PropertyField(menuProp, "Submenu");
			menuField.BindProperty(menuProp);
			container.Add(menuField);
			return container;
		}

		// ------------------------------------------------- entrées à paramètre seul

		private static VisualElement CreateParameterOnlyView(SerializedProperty property, EntryType type)
		{
			var container = new VisualElement();
			container.Add(CreateParameterRow(property, type, includeType: false));
			return container;
		}

		// -------------------------------------------------- rangée Paramètre + Type

		private static VisualElement CreateParameterRow(SerializedProperty property, EntryType type, bool includeType)
		{
			var e = Resolve(property);
			var row = new VisualElement();
			row.AddToClassList("menu-entry-parameter-row");

			var field = new TextField("Parameter") { value = e?.parameter ?? string.Empty };
			field.AddToClassList("menu-entry-parameter-input");
			field.RegisterValueChangedCallback(evt => WriteParameter(property, evt.newValue));
			row.Add(field);

			if (!includeType)
				return row;

			var allowed = AllowedTypes(type).ToArray();
			if (allowed.Length == 0)
				return row;

			var current = e != null ? e.parameterType : DefaultType(type);
			if (Array.IndexOf(allowed, current) < 0)
				current = allowed[0];

			var dropdown = new DropdownField();
			dropdown.choices = new List<string>(Array.ConvertAll(allowed, t => ParameterTypeCompatibility.GetDisplayName(t)));
			dropdown.index = Array.IndexOf(allowed, current);
			dropdown.AddToClassList("menu-entry-type-dropdown");
			// Map the selected index back to the ParameterType instead of parsing the
			// display label, because display names may differ from enum names (e.g.
			// "Boolean" for ParameterType.Bool).
			dropdown.RegisterValueChangedCallback(_ =>
			{
				var idx = dropdown.index;
				if (idx >= 0 && idx < allowed.Length)
					WriteParameterType(property, allowed[idx]);
			});
			row.Add(dropdown);

			return row;
		}

		// --------------------------------------------- entrées typées (values bytes)

		private static VisualElement CreateTypedView(SerializedProperty property, EntryType type)
		{
			var root = new VisualElement();
			root.Add(CreateParameterRow(property, type, includeType: true));

			var valuesContainer = new VisualElement();
			valuesContainer.AddToClassList("menu-entry-values");
			root.Add(valuesContainer);

			Rebuild();

			// Reconstruit l'interprétation quand le type du paramètre change.
			var dropdown = root.Q<DropdownField>();
			dropdown?.RegisterValueChangedCallback(_ => Rebuild());

			// Track changes to the parameterType property itself
			var parameterTypeProp = property.FindPropertyRelative(nameof(MenuEntry.parameterType));
			if (parameterTypeProp != null)
				root.TrackPropertyValue(parameterTypeProp, _ => Rebuild());

			return root;

			void Rebuild()
			{
				valuesContainer.Clear();
				FillValues(valuesContainer, property, type);
			}
		}

		private static void FillValues(VisualElement container, SerializedProperty property, EntryType type)
		{
			var e = Resolve(property);
			var allowed = AllowedTypes(type);
			if (allowed.Count == 0)
			{
				Debug.LogWarning($"No compatible parameter types for entry type {type}. Using default.");
				return;
			}
			var pt = e?.parameterType ?? allowed[0];
			if (!allowed.Contains(pt))
				pt = allowed[0];

			// Déterminer le nombre de slots nécessaires et les allouer.
			int requiredSlots = type switch
			{
				EntryType.Toggle => 2,
				EntryType.Trigger => 1,
				EntryType.Axis1D => 3,
				EntryType.Axis2D => 6,
				EntryType.Text => 1,
				EntryType.Choice => e?.values?.Length ?? 0, // Choice gère ses slots dynamiquement
				_ => 0
			};
			if (requiredSlots > 0)
				EnsureSlots(e, requiredSlots);

			switch (type)
			{
				case EntryType.Toggle:
					container.Add(CreateValueField(property, 0, "Value ON", pt));
					container.Add(CreateValueField(property, 1, "Value OFF", pt));
					break;

				case EntryType.Trigger:
					container.Add(CreateValueField(property, 0, "Trigger Value", pt));
					break;

				case EntryType.Axis1D:
					container.Add(CreateValueField(property, 0, "Min", pt));
					container.Add(CreateValueField(property, 1, "Max", pt));
					container.Add(CreateValueField(property, 2, "Step", pt));
					break;

				case EntryType.Axis2D:
					container.Add(CreateValueField(property, 0, "Min X", pt));
					container.Add(CreateValueField(property, 1, "Max X", pt));
					container.Add(CreateValueField(property, 2, "Step X", pt));
					container.Add(CreateValueField(property, 3, "Min Y", pt));
					container.Add(CreateValueField(property, 4, "Max Y", pt));
					container.Add(CreateValueField(property, 5, "Step Y", pt));
					break;

				case EntryType.Text:
					container.Add(CreateValueField(property, 0, "Placeholder", pt));
					break;

				case EntryType.Choice:
					CreateChoiceListView(container, property, pt);
					break;
			}
		}

		/// <summary>Champ typé unique (bool/int/float/double/string) lié à un slot de bytes.</summary>
		private static VisualElement CreateValueField(SerializedProperty property, int slot, string label, ParameterType pt)
		{
			switch (pt)
			{
				case ParameterType.Bool:
					{
						var field = new Toggle(label)
						{
							value = (bool)Decode(GetBytes(property, slot), pt)
						};
						field.RegisterValueChangedCallback(evt => WriteBytes(property, slot, Encode(evt.newValue, pt)));
						return field;
					}
				case ParameterType.Int:
					{
						var field = new IntegerField(label)
						{
							value = (int)Decode(GetBytes(property, slot), pt)
						};
						field.RegisterValueChangedCallback(evt => WriteBytes(property, slot, Encode(evt.newValue, pt)));
						return field;
					}
				case ParameterType.Double:
					{
						var field = new DoubleField(label)
						{
							value = (double)Decode(GetBytes(property, slot), pt)
						};
						field.RegisterValueChangedCallback(evt => WriteBytes(property, slot, Encode(evt.newValue, pt)));
						return field;
					}
				case ParameterType.String:
					{
						var field = new TextField(label)
						{
							value = (string)Decode(GetBytes(property, slot), pt),
							tooltip = "Text value"
						};
						field.RegisterValueChangedCallback(evt => WriteBytes(property, slot, Encode(evt.newValue, pt)));
						return field;
					}
				case ParameterType.Byte:
					{
						var field = new IntegerField(label)
						{
							value = (byte)Decode(GetBytes(property, slot), pt)
						};
						field.RegisterValueChangedCallback(evt =>
						{
							var clamped = Mathf.Clamp(evt.newValue, byte.MinValue, byte.MaxValue);
							if (clamped != evt.newValue)
								field.SetValueWithoutNotify(clamped);
							WriteBytes(property, slot, Encode((byte)clamped, pt));
						});
						return field;
					}

				case ParameterType.Short:
					{
						var field = new IntegerField(label)
						{
							value = (short)Decode(GetBytes(property, slot), pt)
						};
						field.RegisterValueChangedCallback(evt =>
						{
							var clamped = Mathf.Clamp(evt.newValue, short.MinValue, short.MaxValue);
							if (clamped != evt.newValue)
								field.SetValueWithoutNotify(clamped);
							WriteBytes(property, slot, Encode((short)clamped, pt));
						});
						return field;
					}
				case ParameterType.UShort:
					{
						var field = new IntegerField(label)
						{
							value = (ushort)Decode(GetBytes(property, slot), pt)
						};
						field.RegisterValueChangedCallback(evt =>
						{
							var clamped = Mathf.Clamp(evt.newValue, ushort.MinValue, ushort.MaxValue);
							if (clamped != evt.newValue)
								field.SetValueWithoutNotify(clamped);
							WriteBytes(property, slot, Encode((ushort)clamped, pt));
						});
						return field;
					}
				case ParameterType.UInt:
					{
						var field = new LongField(label)
						{
							value = (uint)Decode(GetBytes(property, slot), pt)
						};
						field.RegisterValueChangedCallback(evt =>
						{
							var clamped = Math.Clamp(evt.newValue, uint.MinValue, uint.MaxValue);
							if (clamped != evt.newValue)
								field.SetValueWithoutNotify(clamped);
							WriteBytes(property, slot, Encode((uint)clamped, pt));
						});
						return field;
					}
				case ParameterType.Long:
					{
						var field = new LongField(label)
						{
							value = (long)Decode(GetBytes(property, slot), pt)
						};
						field.RegisterValueChangedCallback(evt => WriteBytes(property, slot, Encode(evt.newValue, pt)));
						return field;
					}
				case ParameterType.ULong:
					{
						var field = new UnsignedLongField(label)
						{
							value = (ulong)Decode(GetBytes(property, slot), pt)
						};
						field.RegisterValueChangedCallback(evt =>
						{
							var clamped = Math.Clamp(evt.newValue, ulong.MinValue, ulong.MaxValue);
							if (clamped != evt.newValue)
								field.SetValueWithoutNotify(clamped);
							WriteBytes(property, slot, Encode(clamped, pt));
						});
						return field;
					}
				case ParameterType.Float:
					{
						var field = new FloatField(label)
						{
							value = (float)Decode(GetBytes(property, slot), pt)
						};
						field.RegisterValueChangedCallback(evt => WriteBytes(property, slot, Encode(evt.newValue, pt)));
						return field;
					}
				case ParameterType.ByteArray:
					{
						var field = new TextField(label)
						{
							value = Converter.ToString(GetBytes(property, slot))
						};
						field.RegisterValueChangedCallback(evt => WriteBytes(property, slot, Converter.ToBytes(evt.newValue)));
						return field;
					}
				case ParameterType.Vector3:
					{
						var field = new Vector3Field(label)
						{
							value = (Vector3)Decode(GetBytes(property, slot), pt)
						};
						field.RegisterValueChangedCallback(evt => WriteBytes(property, slot, Encode(evt.newValue, pt)));
						return field;
					}
				case ParameterType.Quaternion:
					{
						var field = new Vector3Field(label)
						{
							value = ((Quaternion)Decode(GetBytes(property, slot), pt)).eulerAngles
						};
						field.RegisterValueChangedCallback(evt =>
						{
							var quat = Quaternion.Euler(evt.newValue);
							WriteBytes(property, slot, Encode(quat, pt));
						});
						return field;
					}
				default:
					{
						var field = new FloatField(label)
						{
							value = (float)Decode(GetBytes(property, slot), pt)
						};
						field.RegisterValueChangedCallback(evt => WriteBytes(property, slot, Encode(evt.newValue, pt)));
						return field;
					}
			}
		}

		/// <summary>
		/// Éditeur de liste de choix sous forme de <see cref="ListView"/> basique
		/// (une rangée = paire de slots value + label). Header et footer +/-
		/// natifs de <see cref="ListView"/> ; <c>onAdd</c>/<c>onRemove</c> sont
		/// redéfinis car les "items" sont des index synthétiques qui pointent
		/// vers des paires de slots dans <see cref="MenuEntry.values"/>, et non
		/// une collection C# qu'Unity pourrait manipuler seul.
		/// </summary>
		private static void CreateChoiceListView(VisualElement container, SerializedProperty property, ParameterType pt)
		{
			if (Resolve(property) == null)
				return;

			var listView = new ListView
			{
				virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
				selectionType = SelectionType.Single,
				showBorder = true,
				showFoldoutHeader = true,
				headerTitle = "Options",
				showAddRemoveFooter = true,
				makeItem = MakeChoiceRow,
				bindItem = (element, index) => BindChoiceRow(element, property, index, pt),
				reorderable = true,
				reorderMode = ListViewReorderMode.Animated
			};
			listView.AddToClassList("menu-entry-choice-list");
			listView.AddToClassList("choice-list-view");

			listView.onAdd = view =>
			{
				var resolved = Resolve(property);
				if (resolved == null) return;
				Record(property, "Add choice option");
				EnsureSlots(resolved, (resolved.values?.Length ?? 0) + 2);
				RefreshChoiceList(view, property);
			};

			listView.onRemove = view =>
			{
				var resolved = Resolve(property);
				if (resolved == null || resolved.values == null || resolved.values.Length < 2)
					return;
				int optionCount = resolved.values.Length / 2;
				int index = view.selectedIndex >= 0 ? view.selectedIndex : optionCount - 1;
				int slot = index * 2;
				if (slot < 0 || slot + 1 >= resolved.values.Length)
					return;
				Record(property, "Remove choice option");
				RemoveRange(resolved, slot, 2);
				RefreshChoiceList(view, property);
			};

			RefreshChoiceList(listView, property);
			container.Add(listView);
		}

		private static VisualElement MakeChoiceRow()
		{
			var row = new VisualElement();
			row.AddToClassList("choice-row");

			var valueSlot = new VisualElement();
			valueSlot.AddToClassList("choice-value-slot");
			row.Add(valueSlot);

			var labelSlot = new VisualElement();
			labelSlot.AddToClassList("choice-label-slot");
			row.Add(labelSlot);

			return row;
		}

		private static void BindChoiceRow(VisualElement row, SerializedProperty property, int index, ParameterType pt)
		{
			var valueSlot = row[0];
			valueSlot.Clear();
			int valueSlotIndex = index * 2;
			var valueField = CreateValueField(property, valueSlotIndex, "Value", pt);
			valueField.AddToClassList("choice-value-field");
			valueField.style.flexGrow = 1;
			valueSlot.Add(valueField);

			var labelSlot = row[1];
			labelSlot.Clear();
			int labelSlotIndex = index * 2 + 1;
			var labelField = new TextField("Label")
			{
				value = (string)Decode(GetBytes(property, labelSlotIndex), ParameterType.String)
			};
			labelField.AddToClassList("choice-label-field");
			labelField.style.flexGrow = 1;
			labelField.RegisterValueChangedCallback(evt => WriteBytes(property, labelSlotIndex, Encode(evt.newValue, ParameterType.String)));
			labelSlot.Add(labelField);
		}

		private static void RefreshChoiceList(BaseListView listView, SerializedProperty property)
		{
			var e = Resolve(property);
			if (e == null) return;
			int count = e.values?.Length / 2 ?? 0;
			var newSource = new List<int>();
			for (int i = 0; i < count; i++)
				newSource.Add(i);
			listView.itemsSource = newSource;
			listView.Rebuild();
		}

		private static void RemoveRange(MenuEntry e, int start, int count)
		{
			if (e.values == null || start < 0 || start >= e.values.Length)
				return;
			var remaining = Math.Max(0, e.values.Length - count);
			var result = new string[remaining];
			int write = 0;
			for (int i = 0; i < e.values.Length; i++)
			{
				if (i >= start && i < start + count)
					continue;
				result[write++] = e.values[i];
			}
			e.values = result;
		}
	}
}