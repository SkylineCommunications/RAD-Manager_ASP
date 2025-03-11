using System;
using System.Collections.Generic;
using System.Linq;
using Skyline.DataMiner.Net.AutomationUI.Objects;
using Skyline.DataMiner.Utils.InteractiveAutomationScript;

namespace RADWidgets
{
	public abstract class MultiSelectorItem : Section
	{
		public abstract string GetKey();
		public abstract string GetDisplayValue();
	}

	/// <summary>
	/// Selector widget to select a single item in a MultiSelector widget.
	/// </summary>
	public abstract class MultiSelectorItemSelector<T> : Section where T: MultiSelectorItem
	{
		public abstract T SelectedItem { get; }
	}

	/// <summary>
	/// Widget allowing to select multiple items of a certain type. The number of columns and rows the widget takes up is determined by the itemSelector: it will take (itemSelector.RowCount + 2) rows and (itemSelector.ColumnCount + 1) columns.
	/// </summary>
	public abstract class MultiSelector<T> : Section where T: MultiSelectorItem
	{
		protected MultiSelectorItemSelector<T> itemSelector_;
		private Dictionary<string, T> selectedItems_ = new Dictionary<string, T>();
		private TreeView selectedItemsView_;

		public event EventHandler Changed;

		public List<T> SelectedItems => selectedItems_.Values.ToList();

		private void AddButton_Pressed(object sender, EventArgs e)
		{
			var selectedItem = itemSelector_.SelectedItem;
			if (selectedItem == null)
				return;

			string key = selectedItem.GetKey();
			if (selectedItems_.ContainsKey(key))
				return;

			selectedItems_.Add(key, selectedItem);
			var item = new TreeViewItem(selectedItem.GetDisplayValue(), key);
			selectedItemsView_.Items = selectedItemsView_.Items.Concat(new List<TreeViewItem>() { item }).ToList();
			Changed?.Invoke(this, EventArgs.Empty);
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

		public void ClearSelection()
		{
			selectedItems_.Clear();
			selectedItemsView_.Items = new List<TreeViewItem>();
		}

		protected MultiSelector(MultiSelectorItemSelector<T> itemSelector) : base()
		{
			itemSelector_ = itemSelector;

			selectedItemsView_ = new TreeView(new List<TreeViewItem>())
			{
				IsReadOnly = false,
				MinHeight = 100
			};

			var addButton = new Button("Add");
			addButton.Pressed += AddButton_Pressed;

			var removeButton = new Button("Remove");
			removeButton.Pressed += RemoveButton_Pressed;

			int row = 0;
			AddSection(itemSelector_, row, 0);
			AddWidget(addButton, row + itemSelector_.RowCount - 1, itemSelector_.ColumnCount);
			row += itemSelector_.RowCount;

			AddWidget(selectedItemsView_, row, 0, 2, itemSelector_.ColumnCount);
			AddWidget(removeButton, row, itemSelector_.ColumnCount, verticalAlignment: VerticalAlignment.Top);
		}
	}
}