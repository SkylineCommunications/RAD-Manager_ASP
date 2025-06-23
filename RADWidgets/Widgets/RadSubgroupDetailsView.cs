namespace RadWidgets.Widgets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadWidgets.Widgets.Generic;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;
	using Skyline.DataMiner.Utils.RadToolkit;

	public class RadSubgroupDetailsView : DetailsView<RadSubgroupSelectorItem>
	{
		private readonly Label _groupNameLabel;
		private readonly Label _invalidSelectionLabel;
		private readonly Label _detailsLabel;
		private List<string> _parameterLabels;
		private RadGroupOptions _parentOptions;
		private RadSubgroupSelectorItem _item;

		public RadSubgroupDetailsView(int columnSpan, List<string> parameterLabels, RadGroupOptions parentOptions)
		{
			_parameterLabels = parameterLabels ?? new List<string>();
			_parentOptions = parentOptions ?? throw new ArgumentNullException(nameof(parentOptions));

			_groupNameLabel = new Label()
			{
				Tooltip = "The name of the selected subgroup.",
				Style = TextStyle.Heading,
			};

			_invalidSelectionLabel = new Label()
			{
				Tooltip = "Invalid selection.",
				MinWidth = 400,
			};

			_detailsLabel = new Label()
			{
				Tooltip = "The parameters and options of the selected subgroup.",
				MinWidth = 400,
			};

			AddWidget(_groupNameLabel, 0, 0, 1, columnSpan, verticalAlignment: VerticalAlignment.Top);
			AddWidget(_invalidSelectionLabel, 1, 0, 1, columnSpan, verticalAlignment: VerticalAlignment.Top);
			AddWidget(_detailsLabel, 2, 0, 2, columnSpan, verticalAlignment: VerticalAlignment.Top);
		}

		public void SetParameterLabels(List<string> parameterLabels)
		{
			_parameterLabels = parameterLabels ?? new List<string>();
			UpdateDetails();
		}

		public void SetParentOptions(RadGroupOptions parentOptions)
		{
			_parentOptions = parentOptions ?? throw new ArgumentNullException(nameof(parentOptions));
			UpdateDetails();
		}

		public override void ShowDetails(RadSubgroupSelectorItem selectedItem, List<RadSubgroupSelectorItem> allItems)
		{
			if (allItems == null || !allItems.Any())
			{
				ShowError("Add a subgroup by selecting 'Add subgroup...' on the right");
				return;
			}

			_item = selectedItem;
			if (selectedItem == null)
			{
				ShowError("No subgroup selected");
				return;
			}

			UpdateDetails();
		}

		private void UpdateDetails()
		{
			if (_item == null)
				return;

			_groupNameLabel.Text = _item.DisplayName;
			_invalidSelectionLabel.Text = string.Empty;

			List<string> parameterTexts = new List<string>(_parameterLabels.Count);
			for (int i = 0; i < _parameterLabels.Count; ++i)
			{
				string label = string.IsNullOrWhiteSpace(_parameterLabels[i]) ? $"Parameter {i + 1}" : _parameterLabels[i];
				if (i < _item.Parameters.Count && _item.Parameters[i] != null)
					parameterTexts.Add($"\t{label}: {_item.Parameters[i].ToString()}");
				else
					parameterTexts.Add($"\t{label}: Not set");
			}

			var parameterText = string.Join("\n", parameterTexts);
			double anomalyThreshold = _item.Options.GetAnomalyThresholdOrDefault(_parentOptions.AnomalyThreshold);
			string anomalyThresholdText = _item.Options.AnomalyThreshold.HasValue ? anomalyThreshold.ToString() : $"{anomalyThreshold} (same as parent group)";
			int minimalDuration = _item.Options.GetMinimalDurationOrDefault(_parentOptions.MinimalDuration);
			string minimalDurationText = _item.Options.MinimalDuration.HasValue ? $"{minimalDuration} minutes" : $"{minimalDuration} minutes (same as parent group)";
			_detailsLabel.Text = $"Parameters:\n{parameterText}\n\n" +
				$"Options:\n" +
				$"\tAnomaly threshold: {anomalyThresholdText}\n" +
				$"\tMinimal anomaly duration: {minimalDurationText}";

			UpdateVisibility();
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

		private void UpdateVisibility()
		{
			bool groupDetailsVisible = GetGroupDetailsVisible();
			_detailsLabel.IsVisible = IsSectionVisible && groupDetailsVisible;
			_groupNameLabel.IsVisible = IsSectionVisible && groupDetailsVisible;
			_invalidSelectionLabel.IsVisible = IsSectionVisible && !groupDetailsVisible;
		}
	}
}
