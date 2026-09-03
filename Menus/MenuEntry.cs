using System;
using Nox.Avatars.Parameters;
using UnityEngine;

namespace Nox.CCK.Avatars.Menus {
	/// <summary>
	/// Represents an entry in the avatar menu. 
    /// An entry can be a trigger, a parameter, or a submenu.
	/// </summary>
	[Serializable]
	public class MenuEntry {
        /// <summary>
        /// Display label of the entry.
        /// </summary>
		public string label;

        /// <summary>
        /// Icon of the entry, can be null.
        /// </summary>
		public Sprite icon;

        /// <summary>
        /// Type of the entry, determines its behavior when clicked (or manipulated).
        /// </summary>
		public EntryType type = EntryType.Trigger;

        /// <summary>
        /// Name of the parameter associated with the entry, if any.
        /// </summary>
		public string parameter;

        /// <summary>
        /// Type of the associated parameter. Used by the editor to read/write
        /// <see cref="values"/> with the right encoding, and by the runtime
        /// to interpret the values.
        /// </summary>
        public ParameterType parameterType = ParameterType.Float;

        /// <summary>
        /// Values associated with the entry, if any.
        /// Each value is stored as a base64-encoded string to support various data
        /// types (e.g., int, float, string) while remaining Unity-serializable.
        /// </summary>
        public string[] values;

        /// <summary>
        /// Submenu Used when the entry is a submenu (type = Menu).
        /// </summary>
		public AvatarMenu menu;
	}
}