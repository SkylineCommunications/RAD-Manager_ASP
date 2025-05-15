namespace RadWidgets
{
	using System.Collections.Generic;
	using System.Linq;
	using RadUtils;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Analytics.Rad;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class RadSubgroupView : VisibilitySection
	{
		private readonly Label _subgroupNameLabel;
		private readonly Label _subgroupDetailsLabel;
		private RadSubgroupSettings _settings;

		public RadSubgroupView(RadSubgroupSettings settings = null)
		{
			_settings = settings ?? new RadSubgroupSettings()
			{
				Name = string.Empty,
				Parameters = new List<RADParameter>(),
				Options = new RadSubgroupOptions(),
			};//TODO: probably I can remove this one and just assume that settings is not null

			_subgroupNameLabel = new Label()
			{
				Tooltip = "The name of this subgroup",
			};

			_subgroupDetailsLabel = new Label();

			var editSubgroupButton = new Button("Edit...")
			{
				Tooltip = "Edit the parameters and options of this subgroup.",
			};
			editSubgroupButton.Pressed += (sender, args) => OnEditSubgroupButtonPressed();

			var removeSubgroupButton = new Button("Remove")
			{
				Tooltip = "Remove this subgroup.",
			};
			removeSubgroupButton.Pressed += (sender, args) => OnRemoveSubgroupButtonPressed();

			UpdateText();

			AddWidget(_subgroupNameLabel, 0, 0);
			AddWidget(_subgroupDetailsLabel, 0, 1, 2, 1);
			AddWidget(editSubgroupButton, 0, 2);
			AddWidget(removeSubgroupButton, 1, 2);
		}

		public void UpdateLabels(List<string> labels)
		{
			var newParameters = new List<RADParameter>();
			for (int i = 0; i < labels.Count; ++i)
			{
				ParameterKey key = null;
				if (i < _settings.Parameters.Count)
					key = _settings.Parameters[i].Key;
				//TODO: probably I want to remember the old key as well for the case they reduce and then increase the number again
				newParameters.Add(new RADParameter(key, labels[i]));
			}

			_settings.Parameters = newParameters;
			UpdateText();
		}

		public void UpdateSettings(RadSubgroupSettings settings)
		{
			_settings = settings;
			UpdateText();
		}

		private void OnEditSubgroupButtonPressed()
		{
			//TODO
		}

		private void OnRemoveSubgroupButtonPressed()
		{
			//TODO
		}

		private void UpdateText()
		{
			_subgroupNameLabel.Text = _settings.Name ?? "Unnamed subgroup";

			var parameterText = string.Join("\n", _settings.Parameters.Select(p => $"  {p.Label}: {p.Key?.ToString() ?? "Not set"}"));
			var optionsText = GetOptionsText();
			//TODO: display the element and parameter name just as in the parameter selector
			_subgroupDetailsLabel.Text = $"Parameters:\n{parameterText}";
			if (!string.IsNullOrEmpty(optionsText))
				_subgroupDetailsLabel.Text += $"\n\nOptions:\n{optionsText}";
		}

		private string GetOptionsText(string indent = "  ")
		{
			var optionsTexts = new List<string>();
			if (_settings.Options.AnomalyThreshold.HasValue)
				optionsTexts.Add($"Anomaly threshold: {_settings.Options.AnomalyThreshold}");
			if (_settings.Options.MinimalDuration.HasValue)
				optionsTexts.Add($"Minimal duration: {_settings.Options.MinimalDuration}");

			return string.Join("\n", optionsTexts.Select(t => $"{indent}{t}"));
		}
	}
}
