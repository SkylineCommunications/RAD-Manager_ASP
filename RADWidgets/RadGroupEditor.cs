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
		private readonly Label groupNameLabel_;
		private readonly TextBox groupNameTextBox_;
		private readonly MultiParameterSelector parameterSelector_;
		private readonly RadGroupOptionsEditor optionsEditor_;
		private readonly List<string> existingGroupNames_;
		private bool moreThanMinParametersSelected_ = false;
		private bool lessThanMaxParametersSelected_ = false;
		private bool isVisible_ = true;

		public RadGroupEditor(IEngine engine, List<string> existingGroupNames, RadGroupSettings settings = null)
		{
			existingGroupNames_ = existingGroupNames;
			if (settings != null) // The current group name should be accepted as valid
				existingGroupNames_.Remove(settings.GroupName);

			var groupNameTooltip = "Provide the name of the group. This name will be used when creating suggestion events for anomalies detected on this group.";
			groupNameLabel_ = new Label("Group name")
			{
				Tooltip = groupNameTooltip,
			};
			groupNameTextBox_ = new TextBox()
			{
				Text = settings?.GroupName ?? string.Empty,
				MinWidth = 600,
				Tooltip = groupNameTooltip,
			};
			groupNameTextBox_.Changed += (sender, args) => OnGroupNameTextBoxChanged();

			parameterSelector_ = new MultiParameterSelector(engine, settings?.Parameters);
			parameterSelector_.Changed += (sender, args) => OnParameterSelectorChanged();

			optionsEditor_ = new RadGroupOptionsEditor(parameterSelector_.ColumnCount, settings?.Options);

			OnGroupNameTextBoxChanged();
			OnParameterSelectorChanged();

			int row = 0;
			AddWidget(groupNameLabel_, row, 0);
			AddWidget(groupNameTextBox_, row, 1, 1, parameterSelector_.ColumnCount - 1);
			++row;

			AddSection(parameterSelector_, row, 0);
			row += parameterSelector_.RowCount;

			AddSection(optionsEditor_, row, 0);
		}

		public event EventHandler<EventArgs> ValidationChanged;

		public RadGroupSettings Settings
		{
			get
			{
				return new RadGroupSettings
				{
					GroupName = groupNameTextBox_.Text,
					Parameters = parameterSelector_.GetSelectedParameters(),
					Options = optionsEditor_.Options,
				};
			}
		}

		/// <inheritdoc />
		public override bool IsVisible
		{
			// Note: we had to override this, since otherwise isVisible of the underlying widgets is called instead of on the sections
			get => isVisible_;
			set
			{
				if (isVisible_ == value)
					return;

				isVisible_ = value;

				groupNameLabel_.IsVisible = value;
				groupNameTextBox_.IsVisible = value;
				parameterSelector_.IsVisible = value;
				optionsEditor_.IsVisible = value;
			}
		}

		public bool IsValid { get; private set; }

		public string ValidationText { get; private set; }

		private void UpdateIsValid()
		{
			IsValid = groupNameTextBox_.ValidationState == UIValidationState.Valid && moreThanMinParametersSelected_ && lessThanMaxParametersSelected_;

			List<string> validationTexts = new List<string>(2);
			if (groupNameTextBox_.ValidationState == UIValidationState.Invalid && !moreThanMinParametersSelected_)
				validationTexts.Add("provide a valid group name");
			if (!moreThanMinParametersSelected_)
				validationTexts.Add("select at least two instances");
			else if (!lessThanMaxParametersSelected_)
				validationTexts.Add($"select at most {MAX_PARAMETERS} instances");

			ValidationText = validationTexts.HumanReadableJoin().Capitalize();
			ValidationChanged?.Invoke(this, EventArgs.Empty);
		}

		private void OnParameterSelectorChanged()
		{
			var count = parameterSelector_.GetSelectedParameters().Count();
			bool newMinParametersState = count >= MIN_PARAMETERS;
			bool newMaxParametersState = count <= MAX_PARAMETERS;
			if (newMinParametersState != moreThanMinParametersSelected_ || newMaxParametersState != lessThanMaxParametersSelected_)
			{
				moreThanMinParametersSelected_ = newMinParametersState;
				lessThanMaxParametersSelected_ = newMaxParametersState;
				UpdateIsValid();
			}
		}

		private void OnGroupNameTextBoxChanged()
		{
			if (string.IsNullOrEmpty(groupNameTextBox_.Text))
			{
				groupNameTextBox_.ValidationState = UIValidationState.Invalid;
				groupNameTextBox_.ValidationText = "Provide a group name";
			}
			else if (existingGroupNames_.Contains(groupNameTextBox_.Text))
			{
				groupNameTextBox_.ValidationState = UIValidationState.Invalid;
				groupNameTextBox_.ValidationText = "Group name already exists";
			}
			else
			{
				groupNameTextBox_.ValidationState = UIValidationState.Valid;
				groupNameTextBox_.ValidationText = string.Empty;
			}

			UpdateIsValid();
		}
	}
}
