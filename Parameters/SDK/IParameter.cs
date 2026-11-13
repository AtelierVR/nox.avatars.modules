using UnityEngine.Events;

namespace Nox.Avatars.Parameters {
	public interface IParameter {
		/// <summary>
		/// Gets the name of the parameter.
		/// </summary>
		/// <returns></returns>
		public string Name { get; }
		/// <summary>
		/// Gets the unique identifier of the parameter.
		/// </summary>
		/// <returns></returns>
		public int Key { get; }

		/// <summary>
		/// Gets the category of the parameter, in snake_case (e.g. <c>voice</c>, <c>movement</c>),
		/// or <c>null</c> when it declares none: a parameter without a category belongs to the
		/// « general » section of the editors.
		/// </summary>
		/// <returns></returns>
		public string Category { get; }

		/// <summary>
		/// Gets the flags associated with the parameter.
		/// </summary>
		/// <returns></returns>
		public ParameterFlags Flags { get; }

		/// <summary>
		/// Gets the type of the parameter value.
		/// </summary>
		/// <returns></returns>
		public ParameterType ValueType { get; }

		/// <summary>
		/// Gets the value of the parameter as an object.
		/// This method should be used with caution as it returns a generic object type.
		/// Sets the value of the parameter.
		/// Verify the type of the value before calling this method to ensure it matches the parameter's type.
		/// </summary>
		/// <param name="value"></param>
		public object Value { get; set; }

		/// <summary>
		/// Raised after <see cref="Value"/> was written, with the parameter that changed, so a consumer that
		/// must react (persistence, synchronization, editors…) follows the changes instead of polling.
		/// <para>
		/// <c>null</c> when the parameter does not announce its writes: a value a module publishes from its
		/// own state (rig weights, scale, jiggles) has nothing to report. Use <see cref="ParameterEvents"/>,
		/// which handles the null case.
		/// </para>
		/// </summary>
		public UnityEvent<IParameter> OnValueChanged { get; }
	}
}
