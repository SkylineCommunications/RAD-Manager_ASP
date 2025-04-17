namespace RadWidgets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class RadGroupSettings
	{
		public string GroupName { get; set; }

		public IEnumerable<ParameterKey> Parameters { get; set; }

		public RadGroupOptions Options { get; set; }
	}

	public class RadGroupEditor : Section
	{
		public const int MIN_PARAMETERS = 2;
		public const int MAX_PARAMETERS = 100;
		private readonly Label _groupNameLabel;
		private readonly TextBox _groupNameTextBox;
		private readonly MultiParameterSelector _parameterSelector;
		private readonly RadGroupOptionsEditor _optionsEditor;
		private readonly Label _detailsLabel;
		private readonly List<string> _existingGroupNames;
		private bool _moreThanMinParametersSelected = false;
		private bool _lessThanMaxParametersSelected = false;
		private bool _isVisible = true;

		public RadGroupEditor(IEngine engine, List<string> existingGroupNames, RadGroupSettings settings = null)
		{
			_existingGroupNames = existingGroupNames;
			if (settings != null) // The current group name should be accepted as valid
				_existingGroupNames.Remove(settings.GroupName);

			var groupNameTooltip = "Provide the name of the group. This name will be used when creating suggestion events for anomalies detected on this group.";
			_groupNameLabel = new Label("Group name")
			{
				Tooltip = groupNameTooltip,
			};
			_groupNameTextBox = new TextBox()
			{
				Text = settings?.GroupName ?? string.Empty,
				MinWidth = 600,
				Tooltip = groupNameTooltip,
			};
			_groupNameTextBox.Changed += (sender, args) => OnGroupNameTextBoxChanged();

			_parameterSelector = new MultiParameterSelector(engine, settings?.Parameters);
			_parameterSelector.Changed += (sender, args) => OnParameterSelectorChanged();

			_optionsEditor = new RadGroupOptionsEditor(_parameterSelector.ColumnCount, settings?.Options);

			_detailsLabel = new Label();

			OnGroupNameTextBoxChanged();
			OnParameterSelectorChanged();

			int row = 0;
			AddWidget(_groupNameLabel, row, 0);
			AddWidget(_groupNameTextBox, row, 1, 1, _parameterSelector.ColumnCount - 1);
			++row;

			AddSection(_parameterSelector, row, 0);
			row += _parameterSelector.RowCount;

			AddSection(_optionsEditor, row, 0);
			row += _optionsEditor.RowCount;

			AddWidget(_detailsLabel, row, 0, 1, _parameterSelector.ColumnCount);
		}

		public event EventHandler<EventArgs> ValidationChanged;

		public RadGroupSettings Settings
		{
			get
			{
				return new RadGroupSettings
				{
					GroupName = _groupNameTextBox.Text,
					Parameters = _parameterSelector.GetSelectedParameters(),
					Options = _optionsEditor.Options,
				};
			}
		}

		/// <inheritdoc />
		public override bool IsVisible
		{
			// Note: we had to override this, since otherwise isVisible of the underlying widgets is called instead of on the sections
			get => _isVisible;
			set
			{
				if (_isVisible == value)
					return;

				_isVisible = value;

				_groupNameLabel.IsVisible = value;
				_groupNameTextBox.IsVisible = value;
				_parameterSelector.IsVisible = value;
				_optionsEditor.IsVisible = value;
				UpdateDetailsLabelVisibility();
			}
		}

		public bool IsValid { get; private set; }

		public string ValidationText { get; private set; }

		private void UpdateIsValid()
		{
			IsValid = _groupNameTextBox.ValidationState == UIValidationState.Valid && _moreThanMinParametersSelected && _lessThanMaxParametersSelected;

			List<string> validationTexts = new List<string>(2);
			if (_groupNameTextBox.ValidationState == UIValidationState.Invalid && !_moreThanMinParametersSelected)
				validationTexts.Add("provide a valid group name");
			if (!_moreThanMinParametersSelected || !_lessThanMaxParametersSelected)
				validationTexts.Add("make a valid selection of instances");

			ValidationText = validationTexts.HumanReadableJoin().Capitalize();
			ValidationChanged?.Invoke(this, EventArgs.Empty);
		}

		private void UpdateDetailsLabelVisibility()
		{
			_detailsLabel.IsVisible = _isVisible && _groupNameTextBox.ValidationState == UIValidationState.Valid && (!_moreThanMinParametersSelected || !_lessThanMaxParametersSelected);
		}

		private void UpdateDetailsLabel()
		{
			UpdateDetailsLabelVisibility();

			if (!_moreThanMinParametersSelected)
				_detailsLabel.Text = "Select at least two instances.";
			else if (!_lessThanMaxParametersSelected)
				_detailsLabel.Text = $"Select at most {MAX_PARAMETERS} instances.";
			else
				_detailsLabel.Text = string.Empty;
		}

		private void OnParameterSelectorChanged()
		{
			var count = _parameterSelector.GetSelectedParameters().Count();
			bool newMinParametersState = count >= MIN_PARAMETERS;
			bool newMaxParametersState = count <= MAX_PARAMETERS;
			if (newMinParametersState != _moreThanMinParametersSelected || newMaxParametersState != _lessThanMaxParametersSelected)
			{
				_moreThanMinParametersSelected = newMinParametersState;
				_lessThanMaxParametersSelected = newMaxParametersState;
				UpdateDetailsLabel();
				UpdateIsValid();
			}
		}

		private void OnGroupNameTextBoxChanged()
		{
			if (string.IsNullOrEmpty(_groupNameTextBox.Text))
			{
				_groupNameTextBox.ValidationState = UIValidationState.Invalid;
				_groupNameTextBox.ValidationText = "Provide a group name";
			}
			else if (_existingGroupNames.Contains(_groupNameTextBox.Text))
			{
				_groupNameTextBox.ValidationState = UIValidationState.Invalid;
				_groupNameTextBox.ValidationText = "Group name already exists";
			}
			else
			{
				_groupNameTextBox.ValidationState = UIValidationState.Valid;
				_groupNameTextBox.ValidationText = string.Empty;
			}

			UpdateDetailsLabelVisibility();
			UpdateIsValid();
		}
	}
}
