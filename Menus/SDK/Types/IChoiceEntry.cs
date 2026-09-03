namespace Nox.Avatars.Menus
{
    /// <summary>
    /// Represents an option in a choice menu entry.
    /// </summary>
    public interface IChoiceOption
    {
        public byte[] Value { get; }
        public string Label { get; }
    }

    /// <summary>
    /// Interface for a choice entry in the avatar menu.
    /// </summary>
	public interface IChoiceEntry : IEntry
    {
        /// <summary>
        /// Name of the parameter associated with the choice entry.
        /// </summary>
        public string Parameter { get; }

        /// <summary>
        /// Options available for selection.
        /// </summary>
        public IChoiceOption[] Options { get; }
    }
}
