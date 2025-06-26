namespace RadWidgets.Widgets.Generic
{
	/// <summary>
	/// Abstract class for items that can be selected. Used in a <see cref="MultiSelector{T}" /> and a <see cref="DetailsViewer{T}"/> widget.
	/// </summary>
	public abstract class SelectorItem
	{
		/// <summary>
		/// Gets the key of the item. This key is used to uniquely identify an item: no two items with the same key should be listed in the same widget.
		/// </summary>
		/// <returns>The key.</returns>
		public abstract string GetKey();

		/// <summary>
		/// Gets the display value of the item. This value is shown in the widget.
		/// </summary>
		/// <returns>The display value.</returns>
		public abstract string GetDisplayValue();
	}
}
