using System;
using System.Collections.Generic;
using System.Linq;
using Skyline.DataMiner.Net.AutomationUI.Objects;
using Skyline.DataMiner.Utils.InteractiveAutomationScript;

namespace RadWidgets.Widgets.Generic
{
	public abstract class DetailsView<T> : VisibilitySection where T : SelectorItem
	{
		public abstract void ShowDetails(List<T> selection);
	}

	public class SelectionChangedEventArgs<T> : EventArgs where T : SelectorItem
	{
		public SelectionChangedEventArgs(List<T> selection)
		{
			Selection = selection;
		}

		public List<T> Selection { get; }
	}

	/// <summary>
	/// Widget consisting of a list on the left (represented by a tree view) and a section viewing the details on the right.
	/// </summary>
	public class DetailsViewer<T> : VisibilitySection where T : SelectorItem
	{
		private readonly TreeView _treeView;
		private readonly DetailsView<T> _detailsView;
		private List<T> _items;

		public DetailsViewer(DetailsView<T> detailsView, List<T> items = null)
		{
			_treeView = new TreeView(new List<TreeViewItem>())
			{
				IsReadOnly = false,
				MinWidth = 300,
			};
			_treeView.Changed += (sender, args) => OnTreeViewChanged();

			_detailsView = detailsView ?? throw new ArgumentNullException(nameof(detailsView));

			SetItems(items);

			AddWidget(_treeView, 0, 0, _detailsView.RowCount, 1, verticalAlignment: VerticalAlignment.Top);
			AddSection(_detailsView, 0, 1);
		}

		public event EventHandler<SelectionChangedEventArgs<T>> SelectionChanged;

		public List<T> Items
		{
			get => _items;
			set => SetItems(value);
		}

		public void SetItems(List<T> items, params string[] selectedKeys)
		{
			_items = items ?? new List<T>();

			var treeViewItems = new List<TreeViewItem>(_items.Count);
			List<T> selectedItems = new List<T>(selectedKeys.Length);
			foreach (var item in _items)
			{
				var key = item.GetKey();
				var selected = selectedKeys.Contains(key);
				var treeViewItem = new TreeViewItem(item.GetDisplayValue(), key)
				{
					IsChecked = selected,
				};
				treeViewItems.Add(treeViewItem);
				if (selected)
					selectedItems.Add(item);
			}

			_treeView.Items = treeViewItems;
			_detailsView.ShowDetails(selectedItems);
			SelectionChanged?.Invoke(this, new SelectionChangedEventArgs<T>(selectedItems));
		}

		public List<T> GetSelected()
		{
			var selectedKeys = _treeView.Items.Where(i => i.IsChecked).Select(i => i.KeyValue).ToHashSet();
			return _items.Where(i => selectedKeys.Contains(i.GetKey())).ToList();
		}

		private void OnTreeViewChanged()
		{
			var selectedItems = GetSelected();
			_detailsView.ShowDetails(selectedItems);
			SelectionChanged?.Invoke(this, new SelectionChangedEventArgs<T>(selectedItems));
		}
	}
}
