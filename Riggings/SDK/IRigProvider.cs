using Nox.Avatars;
using Nox.CCK.Events;

namespace Nox.Avatars.Rigging {
	/// <summary>
	/// Exposes the currently active <see cref="IRigging"/> for an avatar.
	/// <para>
	/// It is a module of the avatar: it is resolved through the descriptor's module list
	/// (<c>GetModules&lt;IRigProvider&gt;()</c>), never through a search in the hierarchy, and it is
	/// announced by the descriptor's register/unregister events.
	/// </para>
	/// </summary>
	public interface IRigProvider : IAvatarModule {
		/// <summary>The currently active rig, or <c>null</c> if none is set up.</summary>
		IRigging Rig { get; }

		/// <summary>
		/// Raised when the active rig is replaced (backend hot-swap) or disposed.
		/// <para>
		/// A rig is the target of every tracked part: a consumer that writes the parts of a player onto
		/// the rig must know when those targets changed, instead of comparing the rig instance every
		/// frame.
		/// </para>
		/// </summary>
		NoxEvent<IRigging> OnRigChanged { get; }
	}
}
