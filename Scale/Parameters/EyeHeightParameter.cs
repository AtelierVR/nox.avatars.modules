using Nox.Avatars.Parameters;
using Nox.CCK.Network;
using Nox.CCK.Avatars.Parameters;

namespace Nox.CCK.Avatars.Scale {
	/// <summary>
	/// Eye height: distance from the anchor (feet) to the camera point, in metres, at the current scale
	/// (see <see cref="ScaleAvatarModule.EyeHeight"/>).
	/// <para>
	/// A proportion of the <b>model</b> measured in its rest pose: it is the same value on every client and it
	/// does not follow the animation or the trackers — its live counterpart is the <c>RealEyeHeight</c> parameter.
	/// </para>
	/// Setting it rescales the avatar so its eyes match the requested height, like <c>Height</c> and <c>Scale</c>.
	/// It is <b>local only</b>: <see cref="ScaleParameter"/> carries the value synced to viewers.
	/// </summary>
	public class EyeHeightParameter : IParameter {
		/// <summary>Published by its module: this parameter does not announce a write.</summary>
		public UnityEngine.Events.UnityEvent<IParameter> OnValueChanged => null;
		private readonly ScaleAvatarModule _module;

		public EyeHeightParameter(ScaleAvatarModule module)
			=> _module = module;

		public string Name
			=> "Height/Eye";

		/// <summary>Category shown by the parameter editors.</summary>
		public string Category
			=> ParameterCategory.Scale;

		public int Key
			=> Name.Hash();

		public ParameterType ValueType
			=> ParameterType.Float;

		public ParameterFlags Flags
			=> ParameterFlags.OwnerEditable; // local only: Scale carries the synced value

		public object Value {
			get => _module.EyeHeight;
			set => _module.EyeHeight = value.ToFloat();
		}
	}
}
