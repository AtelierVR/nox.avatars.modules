using UnityEngine;

namespace Nox.Avatars.Menus
{
    /// <summary>
    /// Interface for an entry in the avatar menu.
    /// </summary>
    public interface IEntry
    {
        /// <summary>
        /// Unique identifier of the entry, 
        /// used to identify the entry in the menu.
        /// </summary>
		int Id { get; }

        /// <summary>
        /// First element is the key label of the menu, 
        /// then each element the arguments of the label.
        /// </summary>
        string Label { get; }

        /// <summary>
        /// Icon of the entry, can be null.
        /// </summary>
        Sprite Icon { get; }
    }
}
