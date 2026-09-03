namespace Nox.Avatars.Menus
{
    /// <summary>
    /// Interface for a trigger entry in the avatar menu.
    /// Triggers an action with a parameter value when clicked.
    /// </summary>
	public interface ITriggerEntry : IEntry
    {
        /// <summary>
        /// Name of the parameter associated with the trigger entry.
        /// </summary>
        public string Parameter { get; }

        /// <summary>
        /// Value sent when the entry is triggered.
        /// </summary>
        public byte[] Value { get; }
    }
}
