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
		private readonly TextBox groupNameTextBox_;
		private readonly MultiParameterSelector parameterSelector_;
		private readonly RadGroupOptionsEditor optionsEditor_;
		private readonly List<string> existingGroupNames_;
		private bool parameterSelectorValid_ = false;

		public RadGroupEditor(IEngine engine, List<string> existingGroupNames, RadGroupSettings settings = null)
		{
			existingGroupNames_ = existingGroupNames;
			if (settings != null) // The current group name should be accepted as valid
				existingGroupNames_.Remove(settings.GroupName);

			var groupNameLabel = new Label("Group name");
			groupNameTextBox_ = new TextBox()
			{
				Text = settings?.GroupName ?? string.Empty,
				MinWidth = 600,
			};
			groupNameTextBox_.Changed += (sender, args) => OnGroupNameTextBoxChanged();

			parameterSelector_ = new MultiParameterSelector(engine, settings?.Parameters);
			parameterSelector_.Changed += (sender, args) => OnParameterSelectorChanged();

			optionsEditor_ = new RadGroupOptionsEditor(parameterSelector_.ColumnCount, settings?.Options);

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

		public bool IsValid { get; private set; }

		public string ValidationText { get; private set; }

		private void UpdateIsValid()
		{
			IsValid = groupNameTextBox_.ValidationState == UIValidationState.Valid && parameterSelectorValid_;
			if (groupNameTextBox_.ValidationState == UIValidationState.Invalid && !parameterSelectorValid_)
				ValidationText = "Provide a valid group name and select at least two instances";
			else if (groupNameTextBox_.ValidationState == UIValidationState.Invalid)
				ValidationText = "Provide a valid group name";
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
