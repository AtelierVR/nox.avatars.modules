using Nox.Avatars.Parameters;
using Nox.CCK.Network;

namespace Nox.CCK.Avatars.Scale {
	/// <summary>
	/// Effective (world) scale of the avatar (the anchor's <see cref="UnityEngine.Transform.lossyScale"/>),
	/// stable and independent of the current animation pose.
	/// This is the value synced to viewers: setting it rescales the avatar so its effective scale matches.
	/// </summary>
	public class ScaleParameter : IParameter {
		private readonly ScaleAvatarModule _module;

		public ScaleParameter(ScaleAvatarModule module)
			=> _module = module;

		public string Name
			=> "Scale";

		public int Key
			=> Name.Hash();

		public ParameterType ValueType
			=> ParameterType.Float;

		public ParameterFlags Flags
			=> ParameterFlags.OwnerEditable
				| ParameterFlags.OwnerSyncsToViewers;

		public object Value {
			get => _module.Scale;
			set => _module.Scale = value.ToFloat();
		}
	}
}