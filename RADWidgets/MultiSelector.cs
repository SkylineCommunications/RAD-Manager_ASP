namespace RadWidgets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.AutomationUI.Objects;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	/// <summary>
	/// Abstract class for items that can be selected in a <see cref="MultiSelector{T}" /> widget.
	/// </summary>
	public abstract class MultiSelectorItem : Section
	{
		/// <summary>
		/// Gets the key of the item. This key is used to uniquely identify an item: no two items with the same key can be selected at the same time.
		/// </summary>
		/// <returns>The key.</returns>
		public abstract string GetKey();

		/// <summary>
		/// Gets the display value of the item. This value is shown in the widget.
		/// </summary>
		/// <returns>The display value.</returns>
		public abstract string GetDisplayValue();
	}

	/// <summary>
	/// Selector widget to select a single item in a MultiSelector widget.
	/// </summary>
	/// <typeparam name="T">The type of the items that can be selected.</typeparam>
	public abstract class MultiSelectorItemSelector<T> : Section where T : MultiSelectorItem
	{
		/// <summary>
		/// Gets the selected item in the widget.
		/// </summary>
		public abstract T SelectedItem { get; }
	}

	/// <summary>
	/// Widget allowing to select multiple items of a certain type. The number of columns and rows the widget takes up is determined by the itemSelector:
	/// it will take (itemSelector.RowCount + 2) rows and (itemSelector.ColumnCount + 1) columns.
	/// </summary>
	/// <typeparam name="T">The type of the items that can be selected.</typeparam>
	public abstract class MultiSelector<T> : Section where T : MultiSelectorItem
	{
		private readonly MultiSelectorItemSelector<T> itemSelector_;
		private readonly TreeView selectedItemsView_;
		private readonly Dictionary<string, T> selectedItems_ = new Dictionary<string, T>();
		private readonly Button addButton_;
		private readonly Button removeButton_;

		/// <summary>
		/// Initializes a new instance of the <see cref="MultiSelector{T}"/> class.
		/// </summary>
		/// <param name="itemSelector">The widget to select a single item.</param>
		/// <param name="selectedItems">The initially selected items, or null.</param>
		protected MultiSelector(MultiSelectorItemSelector<T> itemSelector, List<T> selectedItems = null) : base()
		{
			itemSelector_ = itemSelector;

			selectedItemsView_ = new TreeView(new List<TreeViewItem>())
			{
				IsReadOnly = false,
				MinHeight = 100,
			};
			SetSelected(selectedItems);

			addButton_ = new Button("Add");
			addButton_.Pressed += AddButton_Pressed;

			removeButton_ = new Button("Remove");
			removeButton_.Pressed += RemoveButton_Pressed;

			int row = 0;
			AddSection(itemSelector_, row, 0);
			AddWidget(addButton_, row + itemSelector_.RowCount - 1, itemSelector_.ColumnCount);
			row += itemSelector_.RowCount;

			AddWidget(selectedItemsView_, row, 0, 2, itemSelector_.ColumnCount);
			AddWidget(removeButton_, row, itemSelector_.ColumnCount, verticalAlignment: VerticalAlignment.Top);
		}

		/// <summary>
		/// Emitted when an item has been added or removed
		/// </summary>
		public event EventHandler Changed;

		/// <summary>
		/// Gets or sets the tooltip of the add button.
		/// </summary>
		public string AddButtonTooltip
		{
			get => addButton_.Tooltip;
			set => addButton_.Tooltip = value;
		}

		/// <summary>
		/// Gets or sets the tooltip of the remove button.
		/// </summary>
		public string RemoveButtonTooltip
		{
			get => removeButton_.Tooltip;
			set => removeButton_.Tooltip = value;
		}

		/// <summary>
		/// Gets or sets the tooltip of the tree view showing the selected items.
		/// </summary>
		public string SelectedItemsViewTooltip
		{
			get => selectedItemsView_.Tooltip;
			set => selectedItemsView_.Tooltip = value;
		}

		/// <summary>
		/// Gets the widget to select a single item.
		/// </summary>
		protected MultiSelectorItemSelector<T> ItemSelector => itemSelector_;

		/// <summary>
		/// Get the currently selected items.
		/// </summary>
		/// <returns>The currently selected items.</returns>
		public IEnumerable<T> GetSelected()
		{
			return selectedItems_.Values;
		}

		/// <summary>
		/// Sets the selected items.
		/// </summary>
		/// <param name="selected">The new selected items (empty or null if none are selected).</param>
		public void SetSelected(IEnumerable<T> selected)
		{
			selectedItems_.Clear();

			if (selected == null)
			{
				selectedItemsView_.Items = new List<TreeViewItem>();
				Changed?.Invoke(this, EventArgs.Empty);
				return;
			}

			foreach (var item in selected)
			{
				var key = item.GetKey();
				if (!selectedItems_.ContainsKey(key))
					selectedItems_.Add(key, item);
			}

			selectedItemsView_.Items = selectedItems_.Select(p => new TreeViewItem(p.Value.GetDisplayValue(), p.Key)).ToList();
			Changed?.Invoke(this, EventArgs.Empty);
		}

		protected virtual bool AddItem(T item)
		{
			string key = item.GetKey();
			if (selectedItems_.ContainsKey(key))
				return false;

			selectedItems_.Add(key, item);
			var treeViewItem = new TreeViewItem(item.GetDisplayValue(), key);
			selectedItemsView_.Items = selectedItemsView_.Items.Concat(new List<TreeViewItem>() { treeViewItem }).ToList();
			Changed?.Invoke(this, EventArgs.Empty);

			return true;
		}

		private void AddButton_Pressed(object sender, EventArgs e)
		{
			var selectedItem = itemSelector_.SelectedItem;
			if (selectedItem == null)
				return;

			AddItem(selectedItem);
		}

		private void RemoveButton_Pressed(object sender, EventArgs e)
		{
			var newItems = selectedItemsView_.Items.Where(i => !i.IsChecked).ToList();
			if (selectedItemsView_.Items.Count() != newItems.Count)
			{
				foreach (var item in selectedItemsView_.Items.Where(i => i.IsChecked))
					selectedItems_.Remove(item.KeyValue);

				selectedItemsView_.Items = newItems;
				Changed?.Invoke(this, EventArgs.Empty);
			}
		}
	}
}