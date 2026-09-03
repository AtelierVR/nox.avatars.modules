namespace Nox.CCK.Avatars.Menus {
	/// <summary>
	/// Entry type of an avatar menu: corresponds to a radial menu element.
	/// </summary>
	public enum EntryType {
		/// <summary>
		/// Menu entry that opens a submenu (via <see cref="MenuEntry.menu"/>).
		/// </summary>
		Menu = 0, 

		/// <summary>
		/// Menu entry that controls a parameter (via <see cref="MenuEntry.parameter"/>).
		/// Switches between Onv (values[0]) and Off (values[1]) when toggled.
		/// </summary>
		Toggle = 1,

		/// <summary>
		/// Menu entry that triggers an action (via <see cref="MenuEntry.parameter"/>).
		/// The action is triggered when the entry is clicked, 
		/// and the parameter value (values[0]) is sent.
		/// </summary>
		Trigger = 2,

		/// <summary>
		/// Menu entry that sets a value within a range (via <see cref="MenuEntry.parameter"/>).
		/// The parameter is controlled by a 1D axis (values[0] = Start included, values[1] = End included, values[2] = Step).
		/// </summary>
		Axis1D = 3,

		/// <summary>
		/// Menu entry that sets a value within a 2D range (via <see cref="MenuEntry.parameter"/>).
		/// The parameter is controlled by a 2D axis 
		/// (values[0] = StartX included, values[1] = EndX included, values[2] = StepX)
		/// (values[3] = StartY included, values[4] = EndY included, values[5] = StepY).
		/// </summary>
		Axis2D = 4,

		/// <summary>
		/// Menu entry that sets a value within a list of choices (via <see cref="MenuEntry.parameter"/>).
		/// The parameter is controlled by a list of key/value pairs
		/// (values[0] = Choice1 key, values[1] = Choice1 label, values[2] = Choice2 key, values[3] = Choice2 label, …).
		/// </summary>
		Choice = 5,

		/// <summary>
		/// Menu entry that sets a text value.
		/// The parameter is controlled by a text input (values[0] = default text).
		/// Placeholder text can be set via a second value (values[1] = placeholder text).
		/// </summary>
		Text = 6,

		/// <summary>
		/// Menu entry opening a color picker.
		/// </summary>
		ColorPicker = 7
	}
}
