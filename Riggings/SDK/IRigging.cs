using System;
using Nox.Avatars.Parameters;
using UnityEngine;

namespace Nox.Avatars.Rigging {
	/// <summary>
	/// A live rigging instance (non-MonoBehaviour) produced by an <see cref="IRiggingBackend"/>.
	/// Backends implement this instead of creating a MonoBehaviour module, so rigs can be
	/// created, swapped and disposed at runtime without touching the avatar's component graph.
	///
	/// The owning <see cref="IRigProvider"/> (a MonoBehaviour) exposes the current rig to the
	/// rest of the system.
	/// </summary>
	public interface IRigging : IDisposable {
		/// <summary>The id of the backend that created this rig (e.g. "rigbuilder", "finalik").</summary>
		string Id { get; }

		/// <summary>Resolves a generated IK target by its <see cref="PlayerRig"/> index.</summary>
		bool TryGetPart(ushort id, out IRigPart part);

		/// <summary>Returns all generated IK targets.</summary>
		IRigPart[] GetParts();

		/// <summary>Resolves a humanoid bone transform on the avatar.</summary>
		Transform GetBone(HumanBodyBones bone);

		/// <summary>Returns the rig's exposed parameters (weights, active flags, targets).</summary>
		IParameter[] GetParameters();

		/// <summary>Whether the given bone is currently driven by the rig.</summary>
		bool IsActive(HumanBodyBones bone);

		/// <summary>Enables or disables rig control over the given bone.</summary>
		void SetActive(HumanBodyBones bone, bool active);
	}
}
