using System;
using System.Linq;
using Nox.Assets.Editor;
using Nox.Avatars.Editor;
using Nox.CCK.Avatars;
using UnityEngine;

namespace Nox.CCK.Avatars.Modules.Editor {
	/// <summary>
	/// Plumbing shared by the avatar module notifications (one file per check, see the others), the avatar
	/// counterpart of the world modules: the writing itself is <see cref="AssetModuleNotification"/>, this
	/// class only says which scope the avatars use and what a module of an avatar is.
	/// </summary>
	internal static class AvatarModuleNotification {
		/// <summary>Panel id of the avatar publisher/builder, plus the keys of the generic sentences.</summary>
		public static readonly AssetModuleNotificationScope Scope = new("avatar", "avatars.modules.notification");

		/// <summary>
		/// Modules of the avatar. <see cref="AvatarDescriptor.Modules"/> is only filled by a compile, so the
		/// anchor is rescanned here: <see cref="AvatarDescriptor.FindModules(IAvatarDescriptor)"/> is the
		/// one place that knows what a module of an avatar is.
		/// </summary>
		public static T[] Modules<T>(AvatarDescriptor avatar) where T : class
			=> avatar
				? AvatarDescriptor.FindModules(avatar).OfType<T>().ToArray()
				: Array.Empty<T>();

		/// <summary>
		/// Standard state of a module the avatar requires: one is quiet, none is a warning with an add
		/// action, several is an error. Returns the modules found, so a check can go on with its own
		/// content.
		/// </summary>
		public static T[] Required<T>(
			AvatarDescriptor avatar,
			string uid,
			string module,
			string messageKey,
			Action refresh,
			params object[] args
		) where T : Component
			=> AssetModuleNotification.Required(
				Scope,
				avatar ? avatar.Anchor : null,
				Modules<T>(avatar),
				uid,
				module,
				messageKey,
				refresh,
				args
			);

		/// <summary>Standard state of a module the avatar accepts zero or one of: several is an error.</summary>
		public static T[] AtMostOne<T>(AvatarDescriptor avatar, string uid, string module) where T : Component
			=> AssetModuleNotification.AtMostOne(Scope, uid, Modules<T>(avatar), module);

		/// <summary>Writes (or replaces) the entry of <paramref name="uid"/>.</summary>
		public static void Show(
			string uid,
			AssetNotificationType type,
			string key,
			object[] args = null,
			params AssetNotificationAction[] actions
		)
			=> AssetModuleNotification.Show(Scope, uid, type, key, args, actions);

		/// <summary>Removes the entry of <paramref name="uid"/> (nothing to report any more).</summary>
		public static void Hide(string uid)
			=> AssetModuleNotification.Hide(uid);

		/// <summary>Action that selects <paramref name="module"/>, so the author finds what is reported.</summary>
		public static AssetNotificationAction Select(Component module)
			=> AssetModuleNotification.Select(Scope, module);

		/// <summary>
		/// Re-runs <paramref name="update"/> when the current avatar changes, when the scene hierarchy
		/// changes, and — with <paramref name="watchProperties"/> — when a serialized field the check reads
		/// is edited.
		/// </summary>
		public static void Watch(Action<AvatarDescriptor> update, bool watchProperties = false)
			=> _ = new AssetNotificationWatch<AvatarDescriptor>(
				AvatarDescriptorHelper.OnAvatarSelected,
				() => AvatarDescriptorHelper.CurrentAvatar,
				update,
				watchProperties
			);
	}
}
