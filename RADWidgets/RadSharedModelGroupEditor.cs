namespace RadWidgets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadUtils;
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
		private readonly RadSubgroupSelector _subgroupSelector;
		private readonly Label _detailsLabel;
		private readonly List<string> _existingGroupNames;//TODO: still use this one for validation
		private List<string> _parameterLabels;

		public RadSharedModelGroupEditor(IEngine engine, List<string> existingGroupNames, RadSharedModelGroupSettings settings = null)
		{
			_engine = engine;//TODO: also select the correct subgroup when editting an existing subgroup of a shared model group
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

			_subgroupSelector = new RadSubgroupSelector(engine, settings?.Options, _parameterLabels, settings?.Subgroups);

			_optionsEditor = new RadGroupOptionsEditor(3, settings?.Options);
			_optionsEditor.Changed += (sender, args) => _subgroupSelector.UpdateParentOptions(_optionsEditor.Options);

			_detailsLabel = new Label();

			int row = 0;
			AddSection(_groupNameSection, row, 0);
			row += _groupNameSection.RowCount;

			AddWidget(parametersCountLabel, row, 0);
			AddWidget(_parametersCountNumeric, row, 1);
			AddWidget(editLabelsButton, row, 2);
			row++;

			AddSection(_subgroupSelector, row, 0);
			row += _subgroupSelector.RowCount;

			AddSection(_optionsEditor, row, 0);
			row += _optionsEditor.RowCount;

			AddWidget(_detailsLabel, row, 0, 1, 3);
		}

		public event EventHandler ValidationChanged;

		public RadSharedModelGroupSettings Settings
		{
			get
			{
				return new RadSharedModelGroupSettings
				{
					GroupName = _groupNameSection.GroupName,
					Subgroups = _subgroupSelector.Subgroups,
					Options = _optionsEditor.Options,
				};
			}
		}

		public bool IsValid { get; private set; }

		public string ValidationText { get; private set; }

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

				_subgroupSelector.UpdateParameterLabels(d.Labels);
			};
			dialog.Cancelled += (sender, args) => app.Stop();

			app.ShowDialog(dialog);
		}

		private void OnParametersCountNumericChanged()
		{
			int newCount = (int)_parametersCountNumeric.Value;
			if (newCount == _parameterLabels.Count)
				return;

			if (newCount > _parameterLabels.Count)
				_parameterLabels = _parameterLabels.Concat(Enumerable.Range(0, newCount - _parameterLabels.Count).Select(i => string.Empty)).ToList();
			else
				_parameterLabels = _parameterLabels.Take(newCount).ToList();

			_subgroupSelector.UpdateParameterLabels(_parameterLabels);
			//TODO: remember the old labels

			//TODO: update the text about the subgroups and the validation state
		}

		private void OnGroupNameSectionValidationChanged()
		{
			//TODO
		}
		//TODO: also add shared group edit method
		//TODO: remove group: either remove whole group, or only specific subgroup
		//TODO: Retraining: exclude elements or subgroups for retraining
	}
}
