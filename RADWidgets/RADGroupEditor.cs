namespace RADWidgets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class RADGroupSettings
	{
		public string GroupName { get; set; }

		public IEnumerable<ParameterKey> Parameters { get; set; }

		public RADGroupOptions Options { get; set; }
	}

	public class RADGroupEditor : Section
	{
		private readonly TextBox groupNameTextBox_;
		private readonly MultiParameterSelector parameterSelector_;
		private readonly RADGroupOptionsEditor optionsEditor_;
		private bool parameterSelectorValid_ = false;

		public RADGroupEditor(IEngine engine, RADGroupSettings settings = null)
		{
			var groupNameLabel = new Label("Group name");
			groupNameTextBox_ = new TextBox()
			{
				Text = settings?.GroupName ?? string.Empty,
				MinWidth = 600,
			};
			groupNameTextBox_.Changed += (sender, args) => OnGroupNameTextBoxChanged();
			groupNameTextBox_.ValidationText = "Provide a group name";

			parameterSelector_ = new MultiParameterSelector(engine, settings?.Parameters);
			parameterSelector_.Changed += (sender, args) => OnParameterSelectorChanged();

			optionsEditor_ = new RADGroupOptionsEditor(parameterSelector_.ColumnCount, settings?.Options);

			OnGroupNameTextBoxChanged();
			OnParameterSelectorChanged();

			int row = 0;
			AddWidget(groupNameLabel, row, 0);
			AddWidget(groupNameTextBox_, row, 1, 1, parameterSelector_.ColumnCount - 1);
			++row;

			AddSection(parameterSelector_, row, 0);
			row += parameterSelector_.RowCount;

			AddSection(optionsEditor_, row, 0);
		}

		public event EventHandler<EventArgs> ValidationChanged;

		public RADGroupSettings Settings
		{
			get
			{
				return new RADGroupSettings
				{
					GroupName = groupNameTextBox_.Text,
					Parameters = parameterSelector_.GetSelectedParameters(),
					Options = optionsEditor_.Options,
				};
			}
		}

		public bool IsValid { get; private set; }

		public string ValidationText { get; private set; }

		private void UpdateIsValid()
		{
			IsValid = groupNameTextBox_.ValidationState == UIValidationState.Valid && parameterSelectorValid_;
			if (groupNameTextBox_.ValidationState == UIValidationState.Invalid && !parameterSelectorValid_)
				ValidationText = "Provide a group name and select at least two instances";
			else if (groupNameTextBox_.ValidationState == UIValidationState.Invalid)
				ValidationText = "Provide a group name";
			else if (!parameterSelectorValid_)
				ValidationText = "Select at least two instances";
			else
				ValidationText = string.Empty;

			ValidationChanged?.Invoke(this, EventArgs.Empty);
		}

		private void OnParameterSelectorChanged()
		{
			bool newState = parameterSelector_.GetSelectedParameters().Count() >= 2;
			if (newState != parameterSelectorValid_)
			{
				parameterSelectorValid_ = newState;
				UpdateIsValid();
			}
		}

		private void OnGroupNameTextBoxChanged()
		{
			UIValidationState newState = string.IsNullOrEmpty(groupNameTextBox_.Text) ? UIValidationState.Invalid : UIValidationState.Valid;
			if (newState != groupNameTextBox_.ValidationState)
			{
				groupNameTextBox_.ValidationState = newState;
				UpdateIsValid();
			}
		}
	}
}
