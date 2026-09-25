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
	}
}