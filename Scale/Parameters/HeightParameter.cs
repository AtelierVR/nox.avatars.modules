using Nox.Avatars.Parameters;
using Nox.CCK.Network;
using Nox.CCK.Avatars.Parameters;

namespace Nox.CCK.Avatars.Scale {
	/// <summary>
	/// Full avatar height (feet to the top of the skull), in metres, at the current scale
	/// (<see cref="ScaleAvatarModule.Height"/> = base height × scale).
	/// <para>
	/// It describes the <b>model</b>, not the current pose: it is measured in the rest pose at load
	/// (see <see cref="ScaleAvatarModule.RealHeight"/> for the live measurement), so it does not move with
	/// the animation or the trackers. It is <b>local only</b> —
	/// <see cref="ScaleParameter"/> carries the value synced to viewers.
	/// </para>
	/// Setting it rescales the avatar so its height matches the requested value.
	/// </summary>
	public class HeightParameter : IParameter {
		/// <summary>Published by its module: this parameter does not announce a write.</summary>
		public UnityEngine.Events.UnityEvent<IParameter> OnValueChanged => null;
		private readonly ScaleAvatarModule _module;

		public HeightParameter(ScaleAvatarModule module)
			=> _module = module;

		public string Name
			=> "Height";

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
			get => _module.Height;
			set => _module.Height = value.ToFloat();
		}
	}
}
