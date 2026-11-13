using UnityEngine;

namespace Nox.Avatars.Parameters {
	public enum ParameterType : byte {
		[Compatible("numeric", "unsigned")]
		[InspectorName("Boolean")]
		Bool = 0,
		[Compatible("numeric", "integer", "unsigned")]
		Byte  = 1,
		[Compatible("numeric", "integer")]
		Short = 2,

		[Compatible("numeric", "integer", "unsigned")]
		[InspectorName("Unsigned Short")]
		UShort = 3,
		[Compatible("numeric", "integer")]
		Int = 4,

		[Compatible("numeric", "integer", "unsigned")]
		[InspectorName("Unsigned Int")]
		UInt = 5,
		[Compatible("numeric", "integer")]
		Long = 6,

		[Compatible("numeric", "integer", "unsigned")]
		[InspectorName("Unsigned Long")]
		ULong = 7,
		[Compatible("numeric", "floating")]
		Float  = 8,
		[Compatible("numeric", "floating")]
		Double = 9,

		[InspectorName("Byte Array")]
		ByteArray = 10,
		[Compatible("string")]
		String = 11,
		Vector3 = 12,
		Quaternion = 13,

		/// <summary>
		/// Colour parameter. The Animator has no colour parameter, so a colour is carried by one
		/// scalar parameter per channel (<c>Name.R</c>, <c>Name.G</c>, <c>Name.B</c>, <c>Name.A</c>)
		/// and composed into a single typed parameter (see <c>CompositeParameter</c>).
		/// </summary>
		[Compatible("color")]
		[InspectorName("Color")]
		Color = 14,
	}
}