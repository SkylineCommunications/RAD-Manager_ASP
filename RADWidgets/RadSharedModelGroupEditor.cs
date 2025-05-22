namespace RadWidgets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadUtils;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	//TODO: make task for duplication of subgroups and groups if Dennis didn't make one already
	public class RadSharedModelGroupEditor : VisibilitySection
	{
		private const string _invalidParameterLabelsText = "Either all parameters should have a label, or none of them should.";
		private readonly IEngine _engine;
		private readonly GroupNameSection _groupNameSection;
		private readonly RadGroupOptionsEditor _optionsEditor;
		private readonly Label _parametersCountLabel;
		private readonly Numeric _parametersCountNumeric;
		private readonly Button _parameterLabelsEditorButton;
		private readonly RadSubgroupSelector _subgroupSelector;
		private readonly Label _detailsLabel;
		private List<string> _parameterLabels;
		private List<string> _oldParameterLabels;
		private bool _parameterLabelsValid;

		public RadSharedModelGroupEditor(IEngine engine, List<string> existingGroupNames, RadSharedModelGroupSettings settings = null)
		{
			_engine = engine;//TODO: also select the correct subgroup when editting an existing subgroup of a shared model group
			_groupNameSection = new GroupNameSection(settings?.GroupName, existingGroupNames, 2);
			_groupNameSection.ValidationChanged += (sender, args) => OnGroupNameSectionValidationChanged();

			const string parametersPerSubgroupTooltip = "For each subgroup you will be able to add this many subgroups";
			_parametersCountLabel = new Label("Number of parameters per subgroup")
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
			_oldParameterLabels = new List<string>();

			_parameterLabelsEditorButton = new Button("Edit labels...")
			{
				Tooltip = "Edit the labels of the parameters in the subgroups. These labels are used to identify the parameters in the subgroups.",
			};
			_parameterLabelsEditorButton.Pressed += (sender, args) => OnEditLabelsButtonPressed();

			_optionsEditor = new RadGroupOptionsEditor(3, settings?.Options);
			_optionsEditor.Changed += (sender, args) => _subgroupSelector.UpdateParentOptions(_optionsEditor.Options);

			_subgroupSelector = new RadSubgroupSelector(engine, _optionsEditor.Options, _parameterLabels, settings?.Subgroups);
			_subgroupSelector.ValidationChanged += (sender, args) => OnSubgroupSelectorValidationChanged();

			_detailsLabel = new Label();

			UpdateParameterLabelsValid();
			UpdateDetailsLabel();
			UpdateIsValid();

			int row = 0;
			AddSection(_groupNameSection, row, 0);
			row += _groupNameSection.RowCount;

			AddWidget(_parametersCountLabel, row, 0);
			AddWidget(_parametersCountNumeric, row, 1);
			AddWidget(_parameterLabelsEditorButton, row, 2);
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

		/// <inheritdoc />
		public override bool IsVisible
		{
			get => IsSectionVisible;
			set
			{
				if (IsSectionVisible == value)
					return;

				IsSectionVisible = value;

				_groupNameSection.IsVisible = value;
				_optionsEditor.IsVisible = value;
				_parametersCountLabel.IsVisible = value;
				_parametersCountNumeric.IsVisible = value;
				_parameterLabelsEditorButton.IsVisible = value;
				_subgroupSelector.IsVisible = value;
				UpdateDetailsLabelVisibility();
			}
		}

		public bool IsValid { get; private set; }

		public string ValidationText { get; private set; }

		private void UpdateParameterLabelsValid()
		{
			_parameterLabelsValid = _parameterLabels.All(s => !string.IsNullOrEmpty(s)) || _parameterLabels.All(s => string.IsNullOrEmpty(s));
		}

		private void UpdateDetailsLabelVisibility()
		{
			_detailsLabel.IsVisible = IsSectionVisible && _groupNameSection.IsValid && (!_subgroupSelector.IsValid || !_parameterLabelsValid);
		}

		private void UpdateValidationText()
		{
			if (!_groupNameSection.IsValid)
			{
				ValidationText = "Provide a valid subgroup name";
			}
			else if (!_subgroupSelector.IsValid)
			{
				ValidationText = "Make sure all subgroup configurations are valid";
			}
			else if (!_parameterLabelsValid)
			{
				ValidationText = _invalidParameterLabelsText;
			}
			else
			{
				ValidationText = string.Empty;
			}
		}

		private void UpdateDetailsLabel()
		{
			UpdateDetailsLabelVisibility();

			if (!_subgroupSelector.IsValid)
				_detailsLabel.Text = _subgroupSelector.ValidationText;
			else if (!_parameterLabelsValid)
				_detailsLabel.Text = _invalidParameterLabelsText;
			else
				_detailsLabel.Text = string.Empty;
		}

		private void UpdateIsValid()
		{
			UpdateValidationText();
			IsValid = string.IsNullOrEmpty(ValidationText);
			ValidationChanged?.Invoke(this, EventArgs.Empty);
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

				_parameterLabels = d.Labels;
				_oldParameterLabels = new List<string>();
				_subgroupSelector.UpdateParameterLabels(_parameterLabels);

				UpdateParameterLabelsValid();
				UpdateDetailsLabel();
				UpdateIsValid();
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
			{
				int nrFromOldLabels = Math.Min(_oldParameterLabels.Count, newCount - _parameterLabels.Count);
				_parameterLabels.AddRange(_oldParameterLabels.Take(nrFromOldLabels));
				_oldParameterLabels.RemoveRange(0, nrFromOldLabels);
				if (newCount > _parameterLabels.Count)
					_parameterLabels.AddRange(Enumerable.Range(0, newCount - _parameterLabels.Count).Select(i => string.Empty));
			}
			else
			{
				_oldParameterLabels.InsertRange(0, _parameterLabels.Skip(newCount));
				_parameterLabels = _parameterLabels.Take(newCount).ToList();
			}

			_subgroupSelector.UpdateParameterLabels(_parameterLabels);

			UpdateParameterLabelsValid();
			UpdateDetailsLabel();
			UpdateIsValid();
		}

		private void OnGroupNameSectionValidationChanged()
		{
			UpdateDetailsLabelVisibility();
			UpdateIsValid();
		}

		private void OnSubgroupSelectorValidationChanged()
		{
			UpdateDetailsLabel();
			UpdateIsValid();
		}
		//TODO: also add shared group edit method
		//TODO: remove group: either remove whole group, or only specific subgroup
		//TODO: Retraining: exclude elements or subgroups for retraining
	}
}
