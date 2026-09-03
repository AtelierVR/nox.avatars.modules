using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Menus;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.Menus {
	/// <summary>
	/// Module du menu radial d'avatar : expose le menu racine de l'avatar via
	/// <see cref="IMenuModule.Menu"/> (un <see cref="IMenuEntry"/> construit depuis
	/// l'asset <see cref="AvatarMenu"/>). Les pages radiales sont générées côté
	/// client par AvatarRadialPage (converter vers nox.ui).
	/// </summary>
	public class AvatarMenuModule : MonoBehaviour, IMenuModule {
		public AvatarMenu menu;

		public int Priority
			=> int.MaxValue;

		public async UniTask<bool> Setup(IRuntimeAvatar runtime, AvatarModulePhase phase, CancellationToken token = default) {
			await UniTask.Yield(cancellationToken: token);
			if (phase != AvatarModulePhase.Init)
				return true;
			if (!menu)
				menu = ScriptableObject.CreateInstance<AvatarMenu>();
			return true;
		}

		IMenuEntry IMenuModule.Menu
			=> menu != null 
				? new MenuAdapter(menu) 
				: null;

		public static bool Check(IAvatarDescriptor descriptor) {
			var modules = descriptor.GetModules<AvatarMenuModule>();

			var module = modules.Length switch {
				1 => modules.FirstOrDefault(),
				0 => descriptor.Anchor.AddComponent<AvatarMenuModule>(),
				_ => null
			};

			if (!module) {
				Logger.LogError("Verify that the Avatar prefab has a valid AvatarMenuModule component.");
				return false;
			}

			return true;
		}

		/// <summary>Adapte un asset AvatarMenu (CCK) en IMenuEntry (SDK Avatars.Menus).</summary>
		private sealed class MenuAdapter : IMenuEntry {
			private readonly AvatarMenu _menu;

			public MenuAdapter(AvatarMenu menu)
				=> _menu = menu;

			public int Id
				=> _menu ? _menu.name.GetHashCode() : 0;

			public string Label
				=> _menu ? _menu.name : string.Empty;

			public Sprite Icon
				=> null;

			public IEntry[] Entries {
				get {
					if (_menu == null || _menu.entries == null)
						return Array.Empty<IEntry>();
					return _menu.entries
						.Where(entry => entry != null)
						.Select(Convert)
						.ToArray();
				}
			}

			private static IEntry Convert(MenuEntry entry) {
				if (entry == null)
					return null;

				// Sous-menu → sous-menu AvatarMenu (IMenuEntry).
				if (entry.type == EntryType.Menu || entry.menu != null)
					return new MenuAdapter(entry.menu);

				return entry.type switch {
					EntryType.Toggle      => new ToggleAdapter(entry),
					EntryType.Trigger     => new TriggerAdapter(entry),
					EntryType.Axis1D      => new Axis1DAdapter(entry),
					EntryType.Axis2D      => new Axis2DAdapter(entry),
					EntryType.Choice      => new ChoiceAdapter(entry),
					EntryType.Text        => new TextAdapter(entry),
					EntryType.ColorPicker => new ColorPickerAdapter(entry),
					_                     => new LeafAdapter(entry)
				};
			}
		}

		/// <summary>Adapte une entrée paramètre en IToggleEntry (On/Off depuis values ou 1/0 par défaut).</summary>
		private sealed class ToggleAdapter : IToggleEntry {
			private readonly MenuEntry _entry;

			public ToggleAdapter(MenuEntry entry) 
				=> _entry = entry;

			public int Id
				=> _entry != null ? _entry.label.GetHashCode() : 0;

			public string Label
				=> _entry?.label ?? string.Empty;

			public Sprite Icon
				=> _entry?.icon;

			public string Parameter
				=> _entry?.parameter ?? string.Empty;

			public byte[] On
				=> SafeVal(0) ?? new byte[] { 1 };

			public byte[] Off
				=> SafeVal(1) ?? new byte[] { 0 };

			private byte[] SafeVal(int idx)
				=> _entry?.values != null && idx < _entry.values.Length && !string.IsNullOrEmpty(_entry.values[idx])
					? Converter.FromBase64(_entry.values[idx])
					: null;
		}

		/// <summary>Adapte une entrée déclencheur en ITriggerEntry.</summary>
		private sealed class TriggerAdapter : ITriggerEntry {
			private readonly MenuEntry _entry;

			public TriggerAdapter(MenuEntry entry)
				=> _entry = entry;

			public int Id
				=> _entry != null ? _entry.label.GetHashCode() : 0;

			public string Label
				=> _entry?.label ?? string.Empty;

			public Sprite Icon
				=> _entry?.icon;

			public string Parameter
				=> _entry?.parameter ?? string.Empty;

			public byte[] Value
				=> SafeVal(0);

			private byte[] SafeVal(int idx)
				=> _entry?.values != null && idx < _entry.values.Length && !string.IsNullOrEmpty(_entry.values[idx])
					? Converter.FromBase64(_entry.values[idx])
					: Array.Empty<byte>();
		}

		/// <summary>Adapte une entrée axe 1D (slider) en IAxis1DEntry.</summary>
		private sealed class Axis1DAdapter : IAxis1DEntry {
			private readonly MenuEntry _entry;

			public Axis1DAdapter(MenuEntry entry)
				=> _entry = entry;

			public int Id
				=> _entry != null ? _entry.label.GetHashCode() : 0;

			public string Label
				=> _entry?.label ?? string.Empty;

			public Sprite Icon
				=> _entry?.icon;

			public string Parameter
				=> _entry?.parameter ?? string.Empty;

			public byte[] Min  => SafeVal(0);
			public byte[] Max  => SafeVal(1);
			public byte[] Step => SafeVal(2);

			private byte[] SafeVal(int idx)
				=> _entry?.values != null && idx < _entry.values.Length && !string.IsNullOrEmpty(_entry.values[idx])
					? Converter.FromBase64(_entry.values[idx])
					: Array.Empty<byte>();
		}

		/// <summary>Adapte une entrée axe 2D en IAxis2DEntry.</summary>
		private sealed class Axis2DAdapter : IAxis2DEntry {
			private readonly MenuEntry _entry;

			public Axis2DAdapter(MenuEntry entry)
				=> _entry = entry;

			public int Id
				=> _entry != null ? _entry.label.GetHashCode() : 0;

			public string Label
				=> _entry?.label ?? string.Empty;

			public Sprite Icon
				=> _entry?.icon;

			public string Parameter
				=> _entry?.parameter ?? string.Empty;

			public byte[] MinX  => GetVal(0);
			public byte[] MaxX  => GetVal(1);
			public byte[] StepX => GetVal(2);
			public byte[] MinY  => GetVal(3);
			public byte[] MaxY  => GetVal(4);
			public byte[] StepY => GetVal(5);

			private byte[] GetVal(int idx)
				=> _entry?.values != null && idx < _entry.values.Length && !string.IsNullOrEmpty(_entry.values[idx])
					? Converter.FromBase64(_entry.values[idx])
					: Array.Empty<byte>();
		}

		/// <summary>Adapte une entrée liste de choix en IChoiceEntry.</summary>
		private sealed class ChoiceAdapter : IChoiceEntry {
			private readonly MenuEntry _entry;

			public ChoiceAdapter(MenuEntry entry) 
				=> _entry = entry;

			public int Id
				=> _entry != null ? _entry.label.GetHashCode() : 0;

			public string Label
				=> _entry?.label ?? string.Empty;

			public Sprite Icon
				=> _entry?.icon;

			public string Parameter
				=> _entry?.parameter ?? string.Empty;

			public IChoiceOption[] Options {
				get {
					if (_entry?.values == null || _entry.values.Length < 2)
						return Array.Empty<IChoiceOption>();

					var count = _entry.values.Length / 2;
					var list  = new IChoiceOption[count];
					for (int i = 0; i < count; i++) {
					var valStr = _entry.values[i * 2];
					var lblStr = _entry.values[i * 2 + 1];
					var valBytes = Converter.FromBase64(valStr) 
						?? Array.Empty<byte>();
					var lblBytes = Converter.FromBase64(lblStr);
					var optLabel = Converter.ToString(lblBytes);
						list[i] = new ChoiceOption(valBytes, optLabel);
					}
					return list;
				}
			}

			private sealed class ChoiceOption : IChoiceOption {
				public ChoiceOption(byte[] value, string label) {
					Value = value;
					Label = label;
				}

				public byte[] Value { get; }
				public string Label { get; }
			}
		}

		/// <summary>Adapte une entrée saisie de texte en ITextEntry.</summary>
		private sealed class TextAdapter : ITextEntry {
			private readonly MenuEntry _entry;

			public TextAdapter(MenuEntry entry) 
				=> _entry = entry;

			public int Id
				=> _entry != null 
				? _entry.label.GetHashCode() 
				: 0;

			public string Label
				=> _entry?.label ?? string.Empty;

			public Sprite Icon
				=> _entry?.icon;

			public string Parameter
				=> _entry?.parameter ?? string.Empty;

			public string Placeholder
				=> GetString(0);

			private string GetString(int idx) {
				if (_entry?.values == null || _entry.values.Length <= idx)
					return string.Empty;
				var bytes = Converter.FromBase64(_entry.values[idx]);
				return bytes != null ? Converter.ToString(bytes) : string.Empty;
			}
		}

		/// <summary>Adapte une entrée sélecteur de couleur en IColorPickerEntry.</summary>
		private sealed class ColorPickerAdapter : IColorPickerEntry {
			private readonly MenuEntry _entry;

			public ColorPickerAdapter(MenuEntry entry) 
				=> _entry = entry;

			public int Id
				=> _entry != null 
				? _entry.label.GetHashCode() 
				: 0;

			public string Label
				=> _entry?.label ?? string.Empty;

			public Sprite Icon
				=> _entry?.icon;

			public string Parameter
				=> _entry?.parameter ?? string.Empty;
		}

		/// <summary>Adapte une entrée simple en IEntry.</summary>
		private sealed class LeafAdapter : IEntry {
			private readonly MenuEntry _entry;

			public LeafAdapter(MenuEntry entry) 
				=> _entry = entry;

			public int Id
				=> _entry != null 
					? _entry.label.GetHashCode() 
					: 0;

			public string Label
				=> _entry?.label ?? string.Empty;

			public Sprite Icon
				=> _entry?.icon;
		}
	}
}
