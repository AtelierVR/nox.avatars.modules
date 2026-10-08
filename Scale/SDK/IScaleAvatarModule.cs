using Nox.Avatars.Parameters;

namespace Nox.Avatars.Scale {
	/// <summary>
	/// Avatar scale module: the single source of truth for the avatar's <b>size</b>.
	/// <para>
	/// Every measure comes in three flavours, so there is never any doubt which one is read:
	/// <list type="bullet">
	/// <item><c>Initial*</c> — the model, without its scale (what the avatar would measure with a scale of 1),
	/// measured once at load in the model's rest pose. A constant of the avatar.</item>
	/// <item>the plain name — the same measure at the avatar's <b>current</b> scale. Still a property of the
	/// model: it does not follow the animation or the trackers.</item>
	/// <item><c>Real*</c> — the <b>live</b> measurement of the current pose (animation, IK, trackers). This is
	/// the only one that moves.</item>
	/// </list>
	/// </para>
	/// <para>
	/// The avatar itself is the reference: <see cref="Scale"/> is never derived from a player or a rig, it is
	/// what the avatar is. Use <see cref="Height"/> to size something else against the avatar, not the other
	/// way around.
	/// </para>
	/// </summary>
	public interface IScaleAvatarModule : IAvatarModule, IParameterGroup {
		/// <summary>
		/// Effective (world) scale of the avatar, i.e. the anchor's lossy scale. Assigning it resizes the
		/// avatar so its effective scale matches; the parent scale is divided out because <c>localScale</c> is
		/// parent-relative.
		/// </summary>
		public float Scale { get; set; }

		/// <summary>
		/// Height of the model without its scale, in metres: what the avatar would measure with a scale of 1.
		/// Measured once at load, in the model's rest pose.
		/// </summary>
		public float InitialHeight { get; }

		/// <summary>Height of the head bone above the feet at a scale of 1, in metres.</summary>
		public float InitialHeadHeight { get; }

		/// <summary>Height of the eyes above the feet at a scale of 1, in metres.</summary>
		public float InitialEyeHeight { get; }

		/// <summary>
		/// Scale the avatar was loaded with, kept to tell whether its size has been changed since
		/// (<see cref="ScaleModified"/>). This is <b>not</b> a size — see <see cref="InitialHeight"/>.
		/// </summary>
		public float InitialScale { get; }

		/// <summary>
		/// Real (world) height of the avatar in metres, <c>InitialHeight × Scale</c>. Assigning it resizes the
		/// avatar so it measures that height.
		/// </summary>
		public float Height { get; set; }

		/// <summary>Height of the head bone above the feet, in metres, at the current scale.</summary>
		public float HeadHeight { get; }

		/// <summary>
		/// Height of the eyes above the feet, in metres, at the current scale. Assigning it resizes the avatar
		/// so its eyes measure that height.
		/// </summary>
		public float EyeHeight { get; set; }

		/// <summary>
		/// Live height of the avatar in metres: feet → head bone, plus twice the head bone → eye offset
		/// (mirrored past the eyes to reach the top of the skull). Follows the current pose.
		/// </summary>
		public float RealHeight { get; }

		/// <summary>Live height of the head bone above the feet, in metres. Follows the current pose.</summary>
		public float RealHeadHeight { get; }

		/// <summary>Live height of the eyes above the feet, in metres. Follows the current pose.</summary>
		public float RealEyeHeight { get; }

		/// <summary>True once the avatar's size differs from the one it was loaded with.</summary>
		public bool ScaleModified { set; get; }
	}
}