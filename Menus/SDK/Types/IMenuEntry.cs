namespace Nox.Avatars.Menus
{
    /// <summary>
    /// Interface for a menu entry in the avatar menu.
    /// </summary>
	public interface IMenuEntry : IEntry
    {
        /// <summary>
        /// Entries of the menu, each entry is a radial element.
        /// </summary>
        IEntry[] Entries { get; }
    }
}
