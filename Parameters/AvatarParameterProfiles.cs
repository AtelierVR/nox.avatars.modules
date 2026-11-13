using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.Parameters {
	/// <summary>
	/// Snapshot of the <b>savable</b> parameters of an avatar at a given time (see
	/// <see cref="ParameterFlags.Persistent"/>): the encoded values, the avatar it was captured on and the
	/// name the user gave it.
	/// <para>
	/// A profile belongs to the local user of an avatar, a template is shared by every user of an avatar.
	/// Both live in the data container of the avatar (see <see cref="IRuntimeAvatar.Data"/>), so the same
	/// code path persists them for everybody: a local avatar saves them locally (config file) or on the
	/// server (table of the account), a remote avatar saves nothing.
	/// </para>
	/// </summary>
	[Serializable]
	public sealed class AvatarParameterProfile {
		/// <summary>Avatar the profile was captured on.</summary>
		public string avatar;

		/// <summary>Name the user gave to the profile (the key it is stored under).</summary>
		public string name;

		/// <summary>Capture time, ISO 8601 UTC.</summary>
		public string savedAt;

		/// <summary>
		/// Encoded values, by parameter name (see <see cref="ParameterValue"/>), so a value keeps its type
		/// and stays readable without the parameter it came from.
		/// </summary>
		public Dictionary<string, ParameterValue> parameters = new();

		/// <summary>Number of parameters the profile carries.</summary>
		public int Count
			=> parameters?.Count ?? 0;

		public override string ToString()
			=> $"{name ?? "(unnamed)"} @ {avatar} ({Count})";
	}

	/// <summary>
	/// Persists the <b>savable</b> parameters of an avatar as named <i>profiles</i> (per local user and per
	/// avatar) and <i>templates</i> (per avatar, shared by every user). A profile is captured from the
	/// parameters of a module at a moment in time (<see cref="Capture"/>, <see cref="Save"/>) and applied
	/// back later (<see cref="Apply"/>), which is what ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œsave a profile and set itÃƒÂ¢Ã¢â€šÂ¬Ã‚Â means.
	/// <para>
	/// Only the parameters flagged <see cref="ParameterFlags.Persistent"/> are captured and
	/// restored: the others (live poses, backend state...) are left untouched.
	/// </para>
	/// <para>
	/// The service works on an <see cref="IRuntimeAvatar"/>: it reads and writes the data container of the
	/// avatar (<see cref="IRuntimeAvatar.Data"/>) and lets the avatar persist it (<see
	/// cref="ISavableContainer.Save"/>). A local avatar therefore saves the profiles on the server, or
	/// locally when it is not connected (or configured to), and a remote avatar saves nothing.
	/// </para>
	/// </summary>
	public static class AvatarParameterProfiles {
		/// <summary>Segment of the profiles (per local user, in the data of the avatar).</summary>
		public const string ProfilesRoot = "profiles";

		/// <summary>Segment of the templates (per avatar, shared by every user of the machine).</summary>
		public const string TemplatesRoot = "templates";

		/// <summary>Segment holding the names of a set, so an empty set is still enumerable.</summary>
		public const string NamesKey = "names";

		//  Paths (the container of the avatar already scopes them by owner and avatar, see
		//  AvatarDataContainer)

		private static string[] NamesPath(string root)
			=> new[] { root, NamesKey };

		private static string[] EntryPath(string root, string name)
			=> new[] { root, name };

		/// <summary>
		/// Whether the profiles of <paramref name="avatar"/> can be written: only a local avatar saves
		/// (writing in the data of a remote avatar would overwrite someone else's values).
		/// </summary>
		private static bool CanWrite(IRuntimeAvatar avatar, string name) {
			if (avatar == null || !avatar.IsLocal)
				return false;

			if (string.IsNullOrEmpty(name)) {
				Logger.LogWarning("A parameter profile needs a name.", tag: nameof(AvatarParameterProfiles));
				return false;
			}

			return true;
		}

		//  Capture / restore

		/// <summary>
		/// Captures the savable parameters of <paramref name="module"/> into a new profile (not stored yet,
		/// see <see cref="Save"/>). The name and the avatar are left to the caller.
		/// </summary>
		public static AvatarParameterProfile Capture(IParameterModule module, string name = null) {
			var profile = new AvatarParameterProfile {
				name      = name,
				savedAt   = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
				parameters = new Dictionary<string, ParameterValue>()
			};

			if (module == null)
				return profile;

			foreach (var parameter in module.GetParameters() ?? Array.Empty<IParameter>()) {
				if (!IsSavable(parameter))
					continue;

				try {
					var encoded = ParameterValue.From(parameter.Value, parameter.ValueType);
					if (encoded != null)
						profile.parameters[parameter.Name] = encoded;
				} catch (Exception exception) {
					Logger.LogDebug($"Parameter '{parameter.Name}' cannot be captured: {exception.Message}", tag: nameof(AvatarParameterProfiles));
				}
			}

			return profile;
		}

		/// <summary>
		/// Writes the values of <paramref name="profile"/> into the parameters of <paramref name="module"/>.
		/// A parameter the avatar no longer declares (or that is not savable) is skipped. Returns the number
		/// of parameters that were applied.
		/// </summary>
		public static int Apply(AvatarParameterProfile profile, IParameterModule module) {
			if (profile?.parameters == null || module == null)
				return 0;

			var applied = 0;

			foreach (var (name, encoded) in profile.parameters) {
				var parameter = module.GetParameter(name);
				if (!IsSavable(parameter))
					continue;

				var value = encoded?.ToObject();
				if (value == null)
					continue;

				try {
					parameter.Value = value;
					applied++;
				} catch (Exception exception) {
					Logger.LogDebug($"Parameter '{name}' cannot be restored: {exception.Message}", tag: nameof(AvatarParameterProfiles));
				}
			}

			return applied;
		}

		//  Profiles (per local user, in the data of the avatar)

		/// <summary>Names of the profiles the local user saved for <paramref name="avatar"/>.</summary>
		public static string[] GetProfiles(IRuntimeAvatar avatar)
			=> Names(avatar, ProfilesRoot);

		/// <summary>Profile <paramref name="name"/> of the local user for <paramref name="avatar"/>, or null.</summary>
		public static AvatarParameterProfile Load(IRuntimeAvatar avatar, string name)
			=> LoadAt(avatar, ProfilesRoot, name);

		/// <summary>Captures the savable parameters of <paramref name="module"/> as a named profile of the local user.</summary>
		public static bool Save(IRuntimeAvatar avatar, string name, IParameterModule module) {
			if (!CanWrite(avatar, name))
				return false;

			var profile = Capture(module, name);
			profile.avatar = avatar.Identifier.ToString();
			Write(avatar, ProfilesRoot, name, profile);

			return true;
		}

		/// <summary>
		/// Applies the profile <paramref name="name"/> of the local user, falling back to the template of the
		/// same name.
		/// </summary>
		public static int Apply(IRuntimeAvatar avatar, string name, IParameterModule module)
			=> Apply(Load(avatar, name) ?? LoadTemplate(avatar, name), module);

		/// <summary>Deletes a profile of the local user. Returns whether it existed.</summary>
		public static bool Delete(IRuntimeAvatar avatar, string name) {
			if (!Remove(avatar, ProfilesRoot, name))
				return false;

			avatar.Data.Save().Forget();
			return true;
		}

		//  Templates (per avatar, shared by every user of the machine)

		/// <summary>Names of the templates of <paramref name="avatar"/>.</summary>
		public static string[] GetTemplates(IRuntimeAvatar avatar)
			=> Names(avatar, TemplatesRoot);

		/// <summary>Template <paramref name="name"/> of <paramref name="avatar"/>, or null.</summary>
		public static AvatarParameterProfile LoadTemplate(IRuntimeAvatar avatar, string name)
			=> LoadAt(avatar, TemplatesRoot, name);

		/// <summary>Captures the savable parameters of <paramref name="module"/> as a named template of the avatar.</summary>
		public static bool SaveTemplate(IRuntimeAvatar avatar, string name, IParameterModule module) {
			if (!CanWrite(avatar, name))
				return false;

			var profile = Capture(module, name);
			profile.avatar = avatar.Identifier.ToString();
			Write(avatar, TemplatesRoot, name, profile);

			return true;
		}

		/// <summary>Applies a template of the avatar.</summary>
		public static int ApplyTemplate(IRuntimeAvatar avatar, string name, IParameterModule module)
			=> Apply(LoadTemplate(avatar, name), module);

		/// <summary>Deletes a template. Returns whether it existed.</summary>
		public static bool DeleteTemplate(IRuntimeAvatar avatar, string name) {
			if (!Remove(avatar, TemplatesRoot, name))
				return false;

			avatar.Data.Save().Forget();
			return true;
		}

		//  Storage (the container of the avatar persists them, see ISavableContainer.Save)

		private static string[] Names(IRuntimeAvatar avatar, string root)
			=> avatar?.Data?.Get(NamesPath(root), Array.Empty<string>())
			   ?? Array.Empty<string>();

		private static AvatarParameterProfile LoadAt(IRuntimeAvatar avatar, string root, string name) {
			if (avatar == null || string.IsNullOrEmpty(name))
				return null;

			var profile = avatar.Data.Get<AvatarParameterProfile>(EntryPath(root, name));
			if (profile == null)
				return null;

			profile.parameters ??= new Dictionary<string, ParameterValue>();
			return profile;
		}

		private static void Write(IRuntimeAvatar avatar, string root, string name, AvatarParameterProfile profile) {
			avatar.Data.Set(EntryPath(root, name), profile);
			avatar.Data.Set(
				NamesPath(root),
				Names(avatar, root)
					.Where(existing => existing != name)
					.Append(name)
					.OrderBy(existing => existing, StringComparer.OrdinalIgnoreCase)
					.ToArray()
			);

			avatar.Data.Save().Forget();
		}

		private static bool Remove(IRuntimeAvatar avatar, string root, string name) {
			if (LoadAt(avatar, root, name) == null)
				return false;

			avatar.Data.Delete(EntryPath(root, name));
			avatar.Data.Set(
				NamesPath(root),
				Names(avatar, root).Where(existing => existing != name).ToArray()
			);

			return true;
		}

		//  Values

		/// <summary>
		/// A parameter is savable when its entry sets <see cref="ParameterFlags.Persistent"/> and the Animator
		/// does not declare it as a trigger (a trigger has no value to restore).
		/// </summary>
		private static bool IsSavable(IParameter parameter)
			=> parameter.Flags.HasFlag(ParameterFlags.Persistent)
				&& parameter is not BaseParameter { Parameter: { type: AnimatorControllerParameterType.Trigger } };
	}
}
