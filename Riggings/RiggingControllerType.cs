namespace Nox.CCK.Avatars.Rigging {
	/// <summary>
	/// Well-known controller type identifiers used by <see cref="RiggingBackendRegistry"/>.
	/// This is an open string type: any mod can define its own controller type string
	/// and register preferences for it without modifying core code.
	/// </summary>
	public static class RiggingControllerType {
		/// <summary>Wildcard — matches any controller type in preference lookups.</summary>
		public const string Any = "any";

		/// <summary>Local player on desktop (no headset/XR).</summary>
		public const string Desktop = "desktop";

		/// <summary>Local player in VR/XR with a headset.</summary>
		public const string XR = "xr";

		/// <summary>Remote/networked player viewed by another client.</summary>
		public const string Remote = "remote";
	}
}
