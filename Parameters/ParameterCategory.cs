namespace Nox.CCK.Avatars.Parameters {
	/// <summary>
	/// Categories a parameter can declare (see <c>IParameter.Category</c>): the editors group the
	/// parameters by category, then sort them by name inside each section.
	/// <para>
	/// The value is the <b>snake_case</b> key used by the language packs
	/// (<c>avatar.parameters.categories.&lt;category&gt;.label</c> / <c>.description</c>, see
	/// <see cref="ParameterCategoryLanguage"/>). A parameter without a category belongs to
	/// <see cref="General"/>.
	/// </para>
	/// <para>
	/// Adding a category is a one-line change here (plus its two language entries): any parameter —
	/// authored by an avatar, composed by a module, or declared in
	/// <see cref="AvatarParameterModule.DefaultParameters"/> — can then join it.
	/// </para>
	/// </summary>
	public static class ParameterCategory {
		/// <summary>Section of the parameters that declare no category.</summary>
		public const string General = "general";

		/// <summary>Locomotion of the avatar: <c>Velocity</c>, <c>Grounded</c>...</summary>
		public const string Movement = "movement";

		/// <summary>Global pose of the avatar: <c>Pose</c>...</summary>
		public const string Pose = "pose";

		/// <summary>Audio of the player: <c>Voice/Muted</c>, <c>Voice/Volume</c>, <c>Voice/Deafened</c>.</summary>
		public const string Audio = "audio";

		/// <summary>Team of the player: <c>Team/Assigned</c>, <c>Team/Color</c>, <c>Team/Relation</c>.</summary>
		public const string Team = "team";

		/// <summary>IK targets, layer weights and backend of the rig manager.</summary>
		public const string Rigging = "rigging";

		/// <summary>Height and scale of the avatar.</summary>
		public const string Scale = "scale";

		/// <summary>Look of the avatar: toggled body parts and accessories (<c>Ears</c>, <c>Tail</c>...).</summary>
		public const string Appearance = "appearance";
	}
}
