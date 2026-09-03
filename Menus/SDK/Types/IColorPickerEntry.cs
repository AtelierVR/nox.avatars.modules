namespace Nox.Avatars.Menus
{
    /// <summary>
    /// Interface for a color picker entry in the avatar menu.
    /// </summary>
	public interface IColorPickerEntry : IEntry
    {
        /// <summary>
        /// Name of the parameter associated with the color picker.
        /// </summary>
        public string Parameter { get; }
    }
}
