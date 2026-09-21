using Nox.Avatars.Rigging;
using UnityEngine;

namespace Nox.CCK.Avatars.Rigging {
	/// <summary>
	/// Single mapping from a tracked <c>PlayerRig</c> part to the avatar rig transform that carries it.
	///
	/// Both drivers go through here so a part always lands on the same rig object: the local one
	/// (<c>AvatarSyncConnector</c>) writes the live values every frame, the remote one
	/// (<c>RemotePhysical.Update</c>) writes the values interpolated from the network.
	/// </summary>
	public static class RigPartDriver {
		/// <summary>Resolves the rig transform carrying <paramref name="partId"/>.</summary>
		public static bool TryGetPart(IRigging rig, ushort partId, out Transform transform) {
			transform = null;
			if (rig == null || !rig.TryGetPart(partId, out var part))
				return false;
			transform = part.GetTransform();
			return transform;
		}

		/// <summary>
		/// Writes <paramref name="position"/> / <paramref name="rotation"/> on the rig part carrying
		/// <paramref name="partId"/>. Returns <c>false</c> when the rig has no such part (e.g. finger
		/// parts on a rig that only exposes humanoid bones).
		/// </summary>
		public static bool Write(IRigging rig, ushort partId, Vector3 position, Quaternion rotation) {
			if (!TryGetPart(rig, partId, out var transform))
				return false;

			transform.SetPositionAndRotation(position, rotation);
			return true;
		}
	}
}
