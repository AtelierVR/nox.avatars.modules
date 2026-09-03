namespace Nox.Avatars.Menus
{
    /// <summary>
    /// Interface for a 1D axis (slider) entry in the avatar menu.
    /// </summary>
	public interface IAxis1DEntry : IEntry
    {
        /// <summary>
        /// Name of the parameter associated with the 1D axis.
        /// </summary>
        public string Parameter { get; }

        /// <summary>
        /// Minimum value (start included).
        /// </summary>
        public byte[] Min { get; }

        /// <summary>
        /// Maximum value (end included).
        /// </summary>
        public byte[] Max { get; }

        /// <summary>
        /// Step value.
        /// </summary>
        public byte[] Step { get; }
    }
}
