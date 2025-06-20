namespace RadWidgets.Widgets.Generic
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public abstract class DetailsView<T> : VisibilitySection where T : SelectorItem
	{
		public abstract void ShowDetails(T selection);
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
		private readonly RadioButtonList<T> _radioButtonsList;
		private readonly DetailsView<T> _detailsView;

		public DetailsViewer(DetailsView<T> detailsView, List<T> items = null)
		{
			_radioButtonsList = new RadioButtonList<T>()
			{
				MinWidth = 300,
			};
			_radioButtonsList.Changed += (sender, args) => OnRadioButtonsListChanged(args.Selected);

			_detailsView = detailsView ?? throw new ArgumentNullException(nameof(detailsView));

			SetItems(items);

			AddWidget(_radioButtonsList, 0, 0, _detailsView.RowCount, 1, verticalAlignment: VerticalAlignment.Top);
			AddSection(_detailsView, 0, 1);
		}

		public event EventHandler<SelectionChangedEventArgs<T>> SelectionChanged;

		public List<T> GetItems()
		{
			return _radioButtonsList.Options.Select(o => o.Value).ToList();
		}

		public void SetItems(List<T> items, string selectedKey = null)
		{
			if (items == null)
			{
				_radioButtonsList.Options = new List<Option<T>>();
				_detailsView.ShowDetails(null);
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
			_radioButtonsList.Options = options;
			if (selectedIndex.HasValue)
				_radioButtonsList.SelectedOption = options[selectedIndex.Value];

			var selectedItem = selectedIndex.HasValue ? items[selectedIndex.Value] : null;
			_detailsView.ShowDetails(selectedItem);
			SelectionChanged?.Invoke(this, new SelectionChangedEventArgs<T>(selectedItem));
		}

		public T GetSelected()
		{
			return _radioButtonsList.Selected;
		}

		private void OnRadioButtonsListChanged(T selectedItem)
		{
			_detailsView.ShowDetails(selectedItem);
			SelectionChanged?.Invoke(this, new SelectionChangedEventArgs<T>(selectedItem));
		}
	}
}
