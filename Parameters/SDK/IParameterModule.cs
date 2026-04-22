namespace Nox.Avatars.Parameters {
	public interface IParameterModule : IAvatarModule, IParameterGroup {
		public void RegisterParameter(IParameter parameter);
		public void UnregisterParameter(IParameter parameter);
	}
}