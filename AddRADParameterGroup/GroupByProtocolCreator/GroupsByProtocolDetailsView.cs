namespace AddRadParameterGroup.GroupByProtocolCreator
{
	using System.Collections.Generic;
	using System.Linq;
	using RadWidgets.Widgets.Generic;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class GroupsByProtocolDetailsView<T> : DetailsView<T> where T : RadGroupByProtocolDetailsItem
	{
		private readonly Label _groupNameLabel;
		private readonly Label _invalidSelectionLabel;
		private readonly Label _detailsLabel;
		private readonly WrappingLabel _errorLabel;
		private T _item;

		public GroupsByProtocolDetailsView()
		{
			_groupNameLabel = new Label()
			{
				Tooltip = "The name of the selected group.",
				Style = TextStyle.Heading,
			};

			_invalidSelectionLabel = new Label()
			{
				Tooltip = "Invalid selection.",
				MinWidth = 400,
			};

			_detailsLabel = new Label()
			{
				Tooltip = "The parameters and options of the selected group.",
				MinWidth = 400,
			};

			_errorLabel = new WrappingLabel()
			{
				Tooltip = "No group can be created for this element because of this error.",
				MaxTextWidth = 200,
			};

			AddWidget(_groupNameLabel, 0, 2, GetGroupDetailsVisible, verticalAlignment: VerticalAlignment.Top);
			AddWidget(_invalidSelectionLabel, 1, 2, () => !GetGroupDetailsVisible(), verticalAlignment: VerticalAlignment.Top);
			AddWidget(_detailsLabel, 2, 2, GetGroupDetailsVisible, verticalAlignment: VerticalAlignment.Top);
			AddWidget(_errorLabel, 3, 2, GetGroupErrorVisible, verticalAlignment: VerticalAlignment.Top);
		}

		public override void ShowDetails(T selectedItem, List<T> allItems)
		{
			_item = selectedItem;
			if (selectedItem == null)
			{
				ShowError("No group selected");
				return;
			}

			_groupNameLabel.Text = _item.GroupByProtocolInfo.GroupName;
			_invalidSelectionLabel.Text = string.Empty;

			List<string> lines = new List<string>() { "Matching parameters:" };
			if (!_item.GroupByProtocolInfo.ParameterMatches.Any())
				lines.Add("\tNo matching parameters");

			foreach (var match in _item.GroupByProtocolInfo.ParameterMatches)
			{
				if (match.MatchingParameters.Count == 0)
					lines.Add($"\t{match.SelectorItem.GetDisplayValue()} (No matches)");
				else if (match.MatchingParameters.Count == 1)
					lines.Add($"\t{match.SelectorItem.GetDisplayValue()} (1 match)");
				else
					lines.Add($"\t{match.SelectorItem.GetDisplayValue()} ({match.MatchingParameters.Count} matches)");

				lines.AddRange(match.MatchingParameters.Select(p => $"\t\t{ParameterKeyToString(_item.GroupByProtocolInfo.ElementName, match.SelectorItem.ParameterName, p)}"));
			}

			_detailsLabel.Text = string.Join("\n", lines);

			_errorLabel.Text = _item.GetFailureText();

			UpdateVisibility();
		}

		private static string ParameterKeyToString(string elementName, string parameterName, ParameterKey key)
		{
			if (!string.IsNullOrEmpty(key.DisplayInstance))
				return $"{elementName}/{parameterName}/{key.DisplayInstance}";
			else if (!string.IsNullOrEmpty(key.Instance))
				return $"{elementName}/{parameterName}/{key.Instance}";
			else
				return $"{elementName}/{parameterName}";
		}

		private void ShowError(string text)
		{
			_groupNameLabel.Text = string.Empty;
			_invalidSelectionLabel.Text = text;
			_detailsLabel.Text = string.Empty;

			UpdateVisibility();
		}

		private bool GetGroupDetailsVisible()
		{
			return string.IsNullOrEmpty(_invalidSelectionLabel.Text);
		}

		private bool GetGroupErrorVisible()
		{
			return GetGroupDetailsVisible() && !string.IsNullOrEmpty(_errorLabel.Text);
		}

		private void UpdateVisibility()
		{
			bool groupDetailsVisible = GetGroupDetailsVisible();
			_detailsLabel.IsVisible = IsSectionVisible && groupDetailsVisible;
			_groupNameLabel.IsVisible = IsSectionVisible && groupDetailsVisible;
			_invalidSelectionLabel.IsVisible = IsSectionVisible && !groupDetailsVisible;

			bool groupErrorVisible = GetGroupErrorVisible();
			_errorLabel.IsVisible = IsSectionVisible && groupErrorVisible;
		}
	}
}
