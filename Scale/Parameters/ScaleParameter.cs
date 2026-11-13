using Nox.Avatars.Parameters;
using Nox.CCK.Network;
using Nox.CCK.Avatars.Parameters;

namespace Nox.CCK.Avatars.Scale {
	/// <summary>
	/// Effective (world) scale of the avatar (the anchor's <see cref="UnityEngine.Transform.lossyScale"/>),
	/// stable and independent of the current animation pose.
	/// This is the value synced to viewers: setting it rescales the avatar so its effective scale matches.
	/// </summary>
	public class ScaleParameter : IParameter {
		/// <summary>Published by its module: this parameter does not announce a write.</summary>
		public UnityEngine.Events.UnityEvent<IParameter> OnValueChanged => null;
		private readonly ScaleAvatarModule _module;

		public ScaleParameter(ScaleAvatarModule module)
			=> _module = module;

		public string Name
			=> "Scale";

		/// <summary>Category shown by the parameter editors.</summary>
		public string Category
			=> ParameterCategory.Scale;

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