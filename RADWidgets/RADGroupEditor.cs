namespace RADWidgets
{
	using System;
	using System.Collections.Generic;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class RADGroupSettings
	{
		public string GroupName { get; set; }

		public List<ParameterSelectorInfo> Parameters { get; set; }

		public RADGroupOptions Options { get; set; }
	}

	public class RADGroupEditor : Section
	{
		private Label groupNameLabel_;
		private TextBox groupNameTextBox_;
		private MultiParameterSelector parameterSelector_;
		private RADGroupOptionsEditor optionsEditor_;
		private bool isValid_ = false;

		public RADGroupEditor(IEngine engine)
		{
			groupNameLabel_ = new Label("Group name");
			groupNameTextBox_ = new TextBox()
			{
				MinWidth = 600,
			};
			groupNameTextBox_.Changed += (sender, args) => OnGroupNameTextBoxChanged();

			parameterSelector_ = new MultiParameterSelector(engine);
			parameterSelector_.Changed += (sender, args) => UpdateIsValid();

			optionsEditor_ = new RADGroupOptionsEditor();

			OnGroupNameTextBoxChanged();

			int row = 0;
			AddWidget(groupNameLabel_, row, 0);
			AddWidget(groupNameTextBox_, row, 1, 1, parameterSelector_.ColumnCount - 1);
			++row;

			AddSection(parameterSelector_, row, 0);
			row += parameterSelector_.RowCount;

			AddSection(optionsEditor_, row, 0);
		}

		public event EventHandler<EventArgs> IsValidChanged;

		public RADGroupSettings Settings
		{
			get
			{
				return new RADGroupSettings
				{
					GroupName = groupNameTextBox_.Text,
					Parameters = parameterSelector_.SelectedItems,
					Options = optionsEditor_.Options,
				};
			}
		}

		public bool IsValid
		{
			get => isValid_;
			private set
			{
				if (isValid_ != value)
				{
					isValid_ = value;
					IsValidChanged?.Invoke(this, EventArgs.Empty);
				}
			}
		}

		private void UpdateIsValid()
		{
			IsValid = groupNameTextBox_.ValidationState == UIValidationState.Valid && parameterSelector_.SelectedItems.Count > 0;
		}

		private void OnGroupNameTextBoxChanged()
		{
			groupNameTextBox_.ValidationState = string.IsNullOrEmpty(groupNameTextBox_.Text) ? UIValidationState.Invalid : UIValidationState.Valid;
			UpdateIsValid();
		}
	}
}
