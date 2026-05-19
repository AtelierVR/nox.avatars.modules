using System;

namespace Nox.Avatars.Rigging {
	/// <summary>
	/// Represents a pluggable rigging backend that can be registered in <see cref="IRiggingBackendRegistry"/>.
	/// Implement this interface to add a new IK solver (e.g. FinalIK, RigBuilder, custom) as a separate mod.
	/// </summary>
	public interface IRiggingBackend : IDisposable {
		/// <summary>Unique identifier for this backend (e.g. "rigbuilder", "finalik").</summary>
		string Id { get; }

		/// <summary>
		/// Returns the priority this backend claims for the given avatar runtime arguments,
		/// or -1 if it cannot handle them at all.
		/// Higher positive values win over lower ones during resolution.
		/// </summary>
		int CanHandle(IRuntimeAvatar runtime);

		/// <summary>Creates or retrieves the module component on the avatar anchor.</summary>
		IRiggingModule Instantiate(IRuntimeAvatar runtime);
	}
}