using System;
using Nox.Avatars.Parameters;
using Nox.CCK.Utils;

namespace Nox.CCK.Avatars.Rigging.Parameters {
	/// <summary>
	/// Exposes the active rigging backend as a synchronised parameter (<c>ik/type</c>).
	///
	/// The value is the stable CRC32 of the backend id (see <see cref="Hash.CRC32(string)"/>),
	/// so a remote viewer can detect which backend the owner uses and hot-swap its own rig to
	/// match — without reloading the avatar.
	///
	/// The mapping between the CRC32 value and an actual backend is the responsibility of the
	/// consumer (e.g. the <see cref="RigManagerModule"/>, via the backend registry). This
	/// parameter only transports the raw value — it does not hard-code the list of backends.
	///
	/// <list type="bullet">
	///   <item><see cref="Get"/> returns the CRC32 of the current backend.</item>
	///   <item><see cref="Set"/> forwards the raw CRC32 to <c>onChanged</c> so the owner can
	///   resolve and rebuild the rig.</item>
	/// </list>
	/// </summary>
	public class RiggingTypeParameter : IParameter {
		public const string ParameterName = "ik/type";

		private readonly Func<string> _getBackendId;
		private readonly Action<int>  _onChanged;

		/// <summary>
		/// </summary>
		/// <param name="getBackendId">Returns the currently active backend id (e.g. "rigbuilder").</param>
		/// <param name="onChanged">Invoked with the raw CRC32 of the requested backend when the parameter is set.</param>
		public RiggingTypeParameter(Func<string> getBackendId, Action<int> onChanged) {
			_getBackendId = getBackendId ?? throw new ArgumentNullException(nameof(getBackendId));
			_onChanged   = onChanged   ?? throw new ArgumentNullException(nameof(onChanged));
		}

		public string GetName()
			=> ParameterName;

		public bool IsValid()
			=> true;

		public int GetKey()
			=> ParameterName.GetHashCode();

		public ParameterType GetValueType()
			=> ParameterType.Int;

		/// <summary>
		/// Owner → viewers. The owner pushes its backend choice; viewers receive it and adapt.
		/// </summary>
		public ParameterFlags GetFlags()
			=> ParameterFlags.OwnerSyncsToViewers;

		public object Get()
			=> (object)Hash.CRC32(_getBackendId());

		public void Set(object value) {
			if (value == null)
				return;
			var crc = value.ToInt();
			_onChanged(crc);
		}
	}
}
