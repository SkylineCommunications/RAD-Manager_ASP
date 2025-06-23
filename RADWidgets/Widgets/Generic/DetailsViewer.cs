namespace RadWidgets.Widgets.Generic
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public abstract class DetailsView<T> : VisibilitySection where T : SelectorItem
	{
		public abstract void ShowDetails(T selection, List<T> allItems);
	}

	public class SelectionChangedEventArgs<T> : EventArgs where T : SelectorItem
	{
		public SelectionChangedEventArgs(T selection)
		{
			Selection = selection;
		}

		public T Selection { get; }
	}

	/// <summary>
	/// Widget consisting of a list on the left (represented by a tree view) and a section viewing the details on the right.
	/// </summary>
	public class DetailsViewer<T> : VisibilitySection where T : SelectorItem
	{
		private readonly DropDown<T> _dropDown;
		private readonly DetailsView<T> _detailsView;

		public DetailsViewer(DetailsView<T> detailsView, string labelText = null, List<T> items = null)
		{
			Label label = null;
			if (!string.IsNullOrEmpty(labelText))
				label = new Label(labelText);

			_dropDown = new DropDown<T>()
			{
				IsDisplayFilterShown = true,
			};
			_dropDown.Changed += (sender, args) => OnRadioButtonsListChanged(args.Selected);

			_detailsView = detailsView ?? throw new ArgumentNullException(nameof(detailsView));

			SetItems(items);

			if (label != null)
			{
				AddWidget(label, 0, 0);
				AddWidget(_dropDown, 0, 1, 1, _detailsView.ColumnCount - 1);
			}
			else
			{
				AddWidget(_dropDown, 0, 0, 1, _detailsView.ColumnCount);
			}

			AddSection(_detailsView, 1, 0);
		}

		public event EventHandler<SelectionChangedEventArgs<T>> SelectionChanged;

		public List<T> GetItems()
		{
			return _dropDown.Options.Select(o => o.Value).ToList();
		}

		public void SetItems(List<T> items, string selectedKey = null)
		{
			if (items == null)
			{
				_dropDown.Options = new List<Option<T>>();
				_detailsView.ShowDetails(null, new List<T>());
				SelectionChanged?.Invoke(this, new SelectionChangedEventArgs<T>(null));
				return;
			}

			int? selectedIndex = null;
			if (selectedKey != null)
			{
				for (int i = 0; i < items.Count; i++)
				{
					if (items[i].GetKey() == selectedKey)
					{
						selectedIndex = i;
						break;
					}
				}
			}

			var options = items.Select(i => new Option<T>(i.GetDisplayValue(), i)).ToList();
			_dropDown.Options = options;
			if (selectedIndex.HasValue)
				_dropDown.SelectedOption = options[selectedIndex.Value];

			var selectedItem = selectedIndex.HasValue ? items[selectedIndex.Value] : null;
			_detailsView.ShowDetails(selectedItem, items);
			SelectionChanged?.Invoke(this, new SelectionChangedEventArgs<T>(selectedItem));
		}

		public T GetSelected()
		{
			return _dropDown.Selected;
		}

		private void OnRadioButtonsListChanged(T selectedItem)
		{
			_detailsView.ShowDetails(selectedItem, GetItems());
			SelectionChanged?.Invoke(this, new SelectionChangedEventArgs<T>(selectedItem));
		}
	}
}
