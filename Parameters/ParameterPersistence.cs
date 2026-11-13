using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.Parameters {
	/// <summary>
	/// Keeps the value of the parameters flagged <see cref="ParameterFlags.Persistent"/> between sessions.
	/// <para>
	/// The values live in the data container of the avatar (see <see cref="IRuntimeAvatar.Data"/>), under
	/// <c>parameters</c>, one <see cref="ParameterValue"/> per parameter name (the same format as the saved
	/// profiles, see <see cref="AvatarParameterProfiles"/>). They are read back when the avatar loads and
	/// written when they move, so a saved ears or tail toggle comes back without the user opening a profile.
	/// </para>
	/// <para>
	/// Only a local avatar persists anything: the data of a remote avatar belongs to its owner, and the
	/// relay synchronizes the values anyway.
	/// </para>
	/// </summary>
	public static class ParameterPersistence {
		/// <summary>Segment of the parameters in the data container, a <c>name</c> to value map.</summary>
		public const string Root = "parameters";

		/// <summary>Path of the persisted values in the data container of the avatar.</summary>
		public static string[] Path
			=> new[] { Root };

		/// <summary>
		/// Writes the persisted values stored in <paramref name="avatar"/> into the parameters of
		/// <paramref name="module"/>. Returns the number of parameters restored.
		/// </summary>
		public static int Restore(IParameterModule module, IRuntimeAvatar avatar) {
			if (module == null || avatar?.Data == null)
				return 0;

			var stored = avatar.Data.Get<Dictionary<string, ParameterValue>>(Path);
			if (stored == null || stored.Count == 0)
				return 0;

			var restored = 0;

			foreach (var parameter in module.GetParameters() ?? Array.Empty<IParameter>()) {
				if (!IsPersistent(parameter) || !stored.TryGetValue(parameter.Name, out var encoded))
					continue;

				var value = encoded?.ToObject();
				if (value == null)
					continue;

				try {
					parameter.Value = value;
					restored++;
				} catch (Exception exception) {
					Logger.LogDebug($"Parameter '{parameter.Name}' cannot be restored: {exception.Message}", tag: nameof(ParameterPersistence));
				}
			}

			// A stored value that matches no parameter of the avatar is worth a warning: the avatar changed,
			// or the entry it declares no longer says Persistent.
			if (restored == 0)
				Logger.LogWarning($"{stored.Count} saved parameter(s) found for '{avatar.Identifier}' but none could be applied.", tag: nameof(ParameterPersistence));

			return restored;
		}

		/// <summary>Snapshot of the persisted values of <paramref name="module"/>.</summary>
		public static Dictionary<string, ParameterValue> Capture(IParameterModule module) {
			var values = new Dictionary<string, ParameterValue>();

			if (module == null)
				return values;

			foreach (var parameter in module.GetParameters() ?? Array.Empty<IParameter>()) {
				if (!IsPersistent(parameter))
					continue;

				if (TryEncode(parameter, out var encoded))
					values[parameter.Name] = encoded;
			}

			return values;
		}

		/// <summary>
		/// Compares the persistent parameters of <paramref name="module"/> with <paramref name="snapshot"/>.
		/// Nothing is allocated while the values do not move; <paramref name="values"/> holds the new
		/// snapshot when one of them did (it stays <c>null</c> otherwise).
		/// </summary>
		public static bool TryCapture(IParameterModule module, Dictionary<string, ParameterValue> snapshot, out Dictionary<string, ParameterValue> values) {
			values = null;

			var parameters = module?.GetParameters();
			if (parameters == null || parameters.Length == 0)
				return false;

			var changed = snapshot == null;

			foreach (var parameter in parameters) {
				if (changed)
					break;

				if (!IsPersistent(parameter) || !TryEncode(parameter, out var encoded))
					continue;

				if (!snapshot.TryGetValue(parameter.Name, out var previous) || !Equals(previous, encoded))
					changed = true;
			}

			if (!changed)
				return false;

			values = Capture(module);
			return true;
		}

		/// <summary>
		/// Writes <paramref name="values"/> in the data of <paramref name="avatar"/> and persists it (see
		/// <see cref="ISavableContainer.Save"/>). Nothing happens for a remote avatar, or when the stored
		/// values are already the same.
		/// </summary>
		public static bool Store(IRuntimeAvatar avatar, Dictionary<string, ParameterValue> values) {
			var stored = Stash(avatar, values);
			if (stored)
				avatar.Data.Save().Forget();

			return stored;
		}

		/// <summary>
		/// Writes <paramref name="values"/> in the data of <paramref name="avatar"/> without touching its
		/// storage: the values travel with the avatar as soon as they change, and the write that goes to
		/// the table happens later (see <see cref="Store"/> and the debounced save of the module), so the
		/// shutdown always finds them in the data. Nothing happens for a remote avatar, or when the values
		/// are already the stored ones.
		/// </summary>
		public static bool Stash(IRuntimeAvatar avatar, Dictionary<string, ParameterValue> values) {
			// A loading or error avatar has no identifier: it must never write an entry of its own.
			if (avatar == null || !avatar.IsLocal || !avatar.Identifier.IsValid() || values == null || avatar.Data == null)
				return false;

			if (Same(avatar.Data.Get<Dictionary<string, ParameterValue>>(Path), values))
				return false;

			avatar.Data.Set(Path, values);
			return true;
		}

		/// <summary>Whether two snapshots hold the same values.</summary>
		private static bool Same(Dictionary<string, ParameterValue> left, Dictionary<string, ParameterValue> right) {
			if (ReferenceEquals(left, right))
				return true;

			if (left == null || right == null || left.Count != right.Count)
				return false;

			foreach (var pair in left)
				if (!right.TryGetValue(pair.Key, out var other) || !Equals(other, pair.Value))
					return false;

			return true;
		}

		/// <summary>Encodes the value of a parameter, <c>false</c> when it cannot be read.</summary>
		private static bool TryEncode(IParameter parameter, out ParameterValue encoded) {
			encoded = ParameterValue.From(parameter.Value, parameter.ValueType);
			return encoded != null;
		}

		/// <summary>
		/// A parameter is persistent when its entry sets <see cref="ParameterFlags.Persistent"/> and the
		/// Animator does not declare it as a trigger (a trigger has no value to restore).
		/// </summary>
		private static bool IsPersistent(IParameter parameter)
			=> parameter != null
				&& parameter.Flags.HasFlag(ParameterFlags.Persistent)
				&& parameter is not BaseParameter { Parameter: { type: AnimatorControllerParameterType.Trigger } };
	}
}
