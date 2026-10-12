namespace Nox.Avatars.Scale {
	/// <summary>
	/// The <c>scale.change</c> bus event, sent by the scale module when the avatar's effective scale changes:
	/// the whole skeleton moved, so anything anchored to the bones (jiggles, …) must rebuild from the new
	/// pose.
	/// </summary>
	public static class ScaleChangedEvent {
		/// <summary>Name of the event (see <see cref="IAvatarModuleBus.Send"/>).</summary>
		public const string Name = "scale.change";

		/// <summary>Context key holding the new effective (world) scale, as a <see cref="float"/>.</summary>
		public const string Scale = "scale";
	}
}
