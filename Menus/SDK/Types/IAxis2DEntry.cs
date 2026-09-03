namespace Nox.Avatars.Menus
{
    /// <summary>
    /// Interface for a 2D axis entry in the avatar menu.
    /// </summary>
	public interface IAxis2DEntry : IEntry
    {
        /// <summary>
        /// Name of the parameter associated with the 2D axis.
        /// </summary>
        public string Parameter { get; }

        public byte[] MinX { get; }
        public byte[] MaxX { get; }
        public byte[] StepX { get; }
        public byte[] MinY { get; }
        public byte[] MaxY { get; }
        public byte[] StepY { get; }
    }
}
