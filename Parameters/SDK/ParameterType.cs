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
	}
}