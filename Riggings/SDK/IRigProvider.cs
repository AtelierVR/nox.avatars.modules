namespace Nox.Avatars.Rigging {
	/// <summary>
	/// Exposes the currently active <see cref="IRigging"/> for an avatar.
	/// Implemented by a MonoBehaviour (the rig manager) so callers in other assemblies can
	/// resolve the live rig via <c>GetComponentInChildren&lt;IRigProvider&gt;()</c> without a
	/// direct reference to the concrete manager type.
	/// </summary>
	public interface IRigProvider {
		/// <summary>Returns the currently active rig, or <c>null</c> if none is set up.</summary>
		IRigging GetRig();
	}
}
