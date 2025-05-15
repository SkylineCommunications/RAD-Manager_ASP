namespace RadWidgets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadUtils;
	using Skyline.DataMiner.Analytics.Rad;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	//TODO: probably prevent adding groups with same parameters in other subgroups
	//TODO: make task for duplication of subgroups and groups if Dennis didn't make one already
	public class RadSharedModelGroupEditor : VisibilitySection
	{
		private readonly IEngine _engine;
		private readonly GroupNameSection _groupNameSection;
		private readonly RadGroupOptionsEditor _optionsEditor;
		private readonly Numeric _parametersCountNumeric;
		private readonly List<RadSubgroupView> _subgroupViews;
		private readonly Label _detailsLabel;
		private readonly List<string> _existingGroupNames;//TODO: still use this one for validation
		private List<string> _parameterLabels;

		public RadSharedModelGroupEditor(IEngine engine, List<string> existingGroupNames, RadSharedModelGroupSettings settings = null)
		{
			_engine = engine;
			_groupNameSection = new GroupNameSection(settings?.GroupName, existingGroupNames, 2);
			_groupNameSection.ValidationChanged += (sender, args) => OnGroupNameSectionValidationChanged();

			OnGroupNameSectionValidationChanged();

			// Add the "Number of parameters per subgroup" label and numeric input
			// Numeric input for number of parameters per subgroup
			const string parametersPerSubgroupTooltip = "For each subgroup you will be able to add this many subgroups";
			var parametersCountLabel = new Label("Number of parameters per subgroup")
			{
				Tooltip = parametersPerSubgroupTooltip,
			};
			var firstSubgroup = settings?.Subgroups?.FirstOrDefault();
			_parametersCountNumeric = new Numeric
			{
				Tooltip = parametersPerSubgroupTooltip,
				Minimum = RadGroupEditor.MIN_PARAMETERS,
				Maximum = RadGroupEditor.MAX_PARAMETERS,
				StepSize = 1,
			};
			_parametersCountNumeric.Changed += (sender, args) => OnParametersCountNumericChanged();

			if (firstSubgroup != null)
			{
				_parameterLabels = firstSubgroup.Parameters.Select(p => p.Label).ToList();
				_parametersCountNumeric.Value = firstSubgroup.Parameters.Count;
			}
			else
			{
				_parameterLabels = Enumerable.Range(0, RadGroupEditor.MIN_PARAMETERS).Select(i => string.Empty).ToList();
				_parametersCountNumeric.Value = RadGroupEditor.MIN_PARAMETERS;
			}

			var editLabelsButton = new Button("Edit labels...")
			{
				Tooltip = "Edit the labels of the parameters in the subgroups. These labels are used to identify the parameters in the subgroups.",
			};
			editLabelsButton.Pressed += (sender, args) => OnEditLabelsButtonPressed();

			_subgroupViews = new List<RadSubgroupView>();
			if (settings?.Subgroups != null)
				_subgroupViews.AddRange(settings.Subgroups.Select(s => new RadSubgroupView(s)));
			//TODO: this is just a preview
			_subgroupViews.Add(new RadSubgroupView(new RadSubgroupSettings()
			{
				Name = "First subgroup",
				Parameters = new List<RADParameter>()
				{
					new RADParameter(new Skyline.DataMiner.Analytics.DataTypes.ParameterKey(1, 2, 3, "instance"), "Input power"),
					new RADParameter(new Skyline.DataMiner.Analytics.DataTypes.ParameterKey(1, 2, 3, "instance2"), "Output power"),
				},
				Options = new RadSubgroupOptions()
				{
					AnomalyThreshold = 0.5,
					MinimalDuration = 10,
				},
			}));
			_subgroupViews.Add(new RadSubgroupView(new RadSubgroupSettings()
			{
				Name = "Second subgroup",
				Parameters = new List<RADParameter>()
				{
					new RADParameter(new Skyline.DataMiner.Analytics.DataTypes.ParameterKey(2, 2, 3, "instance"), "Input power"),
					new RADParameter(new Skyline.DataMiner.Analytics.DataTypes.ParameterKey(2, 2, 3, "instance2"), "Output power"),
				},
				Options = new RadSubgroupOptions(),
			}));

			var addSubgroupButton = new Button("Add subgroup...")
			{
				Tooltip = "Add a new subgroup.",
			};
			addSubgroupButton.Pressed += (sender, args) => OnAddSubgroupButtonPressed();

			_optionsEditor = new RadGroupOptionsEditor(3, settings?.Options);

			_detailsLabel = new Label();

			int row = 0;
			AddSection(_groupNameSection, row, 0);
			row += _groupNameSection.RowCount;

			AddWidget(parametersCountLabel, row, 0);
			AddWidget(_parametersCountNumeric, row, 1, 1, 2);
			row++;

			AddWidget(editLabelsButton, row, 2);
			row++;

			foreach (var subgroupView in _subgroupViews)
			{
				AddSection(subgroupView, row, 0);
				row += subgroupView.RowCount;
			}

			AddWidget(addSubgroupButton, row, 2);
			row++;

			AddSection(_optionsEditor, row, 0);
			row += _optionsEditor.RowCount;

			AddWidget(_detailsLabel, row, 0, 1, 3);
		}

		private void UpdateLabels(List<string> parameterLabels)
		{
			_parameterLabels = parameterLabels;
			foreach (var subgroupView in _subgroupViews)
				subgroupView.UpdateLabels(parameterLabels);
		}

		private void AddSubgroup(RadSubgroupSettings settings)
		{
			var subgroupView = new RadSubgroupView(settings);
			_subgroupViews.Add(subgroupView);

			//TODO: add the subgroup to the editor, remove the widgets below it and readd them, then propagate upwards
		}

		private void OnAddSubgroupButtonPressed()
		{
			InteractiveController app = new InteractiveController(_engine);
			//TODO: fill in existing subgroup names
			AddSubgroupDialog dialog = new AddSubgroupDialog(_engine, new List<string>(), _parameterLabels, _optionsEditor.Options.AnomalyThreshold,
				_optionsEditor.Options.MinimalDuration);
			dialog.Accepted += (sender, args) =>
			{
				var d = sender as AddSubgroupDialog;
				if (d == null)
					return;

				app.Stop();

				AddSubgroup(d.Settings);
			};
			dialog.Cancelled += (sender, args) => app.Stop();

			app.ShowDialog(dialog);
		}

		private void OnEditLabelsButtonPressed()
		{
			InteractiveController app = new InteractiveController(_engine);
			RadLabelEditorDialog dialog = new RadLabelEditorDialog(_engine, _parameterLabels);
			dialog.Accepted += (sender, args) =>
			{
				var d = sender as RadLabelEditorDialog;
				if (d == null)
					return;

				app.Stop();

				UpdateLabels(d.Labels);
			};
			dialog.Cancelled += (sender, args) => app.Stop();

			app.ShowDialog(dialog);
		}

		private void OnParametersCountNumericChanged()
		{
			int newCount = (int)_parametersCountNumeric.Value;
			if (newCount == _parameterLabels.Count)
				return;

			List<string> newParameterLabels;
			if (newCount > _parameterLabels.Count)
				newParameterLabels = _parameterLabels.Concat(Enumerable.Range(0, newCount - _parameterLabels.Count).Select(i => string.Empty)).ToList();
			else
				newParameterLabels = _parameterLabels.Take(newCount).ToList();

			UpdateLabels(newParameterLabels);
			//TODO: remember the old labels

			//TODO: update the text about the subgroups and the validation state
		}

		private void OnGroupNameSectionValidationChanged()
		{
			//TODO
		}
	}
}
