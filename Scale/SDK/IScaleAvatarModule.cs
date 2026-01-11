using Nox.Avatars.Parameters;

namespace Nox.Avatars.Scale {
	public interface IScaleAvatarModule : IAvatarModule, IParameterGroup {
		public float Scale { get; set; }

		public float Height { get; set; }

		public bool ScaleModified { set; get; }
	}
}