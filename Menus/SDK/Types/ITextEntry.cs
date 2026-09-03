namespace Nox.Avatars.Menus
{
    /// <summary>
    /// Interface for a text input entry in the avatar menu.
    /// </summary>
	public interface ITextEntry : IEntry
    {
        /// <summary>
        /// Name of the parameter associated with the text entry.
        /// </summary>
        public string Parameter { get; }

        /// <summary>
        /// Placeholder text.
        /// </summary>
        public string Placeholder { get; }
    }
}
