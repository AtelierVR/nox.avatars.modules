namespace Nox.Avatars.Menus
{
    /// <summary>
    /// Avatar module that exposes the avatar radial menu.
    /// </summary>
    public interface IMenuModule : IAvatarModule
    {
        /// <summary>
        /// Asset of the root menu of the avatar.
        /// The root menu is the first menu displayed
        /// when the radial menu is opened in the avatar page.
        /// </summary>
        IMenuEntry Menu { get; }
    }
}
