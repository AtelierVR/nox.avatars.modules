namespace Nox.Avatars.Menus
{
    /// <summary>
    /// Interface for a toggle entry in the avatar menu.
    /// 
    /// The On value is sent when the entry is toggled on,
    /// and the Off value is sent when the entry is toggled off.
    /// 
    /// The toogle is considered on when the parameter 
    /// and the On value are equal, 
    /// else it is considered off.
    /// </summary>
	public interface IToggleEntry : IEntry
    {
        /// <summary>
        /// Name of the parameter associated with the toggle entry.
        /// </summary>
        public string Parameter { get; }

        /// <summary>
        /// Value sent when the entry is toggled on.
        /// </summary>
        public byte[] On { get; }

        /// <summary>
        /// Value sent when the entry is toggled off.
        /// </summary>
        public byte[] Off { get; }
    }
}
