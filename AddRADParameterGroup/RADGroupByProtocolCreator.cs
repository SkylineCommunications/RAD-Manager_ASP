namespace AddRADParameterGroup
{
	using System;
	using System.Collections.Generic;
	using AddParameterGroup;
	using RADWidgets;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class RADGroupByProtocolSettings
	{
		public string GroupPrefix { get; set; }

		public string ProtocolName { get; set; }

		public string ProtocolVersion { get; set; }

		public List<ProtocolParameterSelectorInfo> Parameters { get; set; }

		public RADGroupOptions Options { get; set; }
	}

	public class RADGroupByProtocolCreator : Section
	{
		private Label groupPrefixLabel_;
		private TextBox groupPrefixTextBox_;
		private MultiParameterPerProtocolSelector parameterSelector_;
		private RADGroupOptionsEditor optionsEditor_;
		private bool isValid_ = false;

		public RADGroupByProtocolCreator(IEngine engine)
		{
			groupPrefixLabel_ = new Label("Group name prefix");

			groupPrefixTextBox_ = new TextBox()
			{
				MinWidth = 600,
			};
			groupPrefixTextBox_.Changed += (sender, args) => OnGroupPrefixTextBoxChanged();

			parameterSelector_ = new MultiParameterPerProtocolSelector(engine)
			{
				IsVisible = false,
			};
			parameterSelector_.Changed += (sender, args) => UpdateIsValid();

			optionsEditor_ = new RADGroupOptionsEditor(parameterSelector_.ColumnCount);

			OnGroupPrefixTextBoxChanged();

			int row = 0;
			AddWidget(groupPrefixLabel_, row, 0);
			AddWidget(groupPrefixTextBox_, row, 1, 1, parameterSelector_.ColumnCount - 1);
			++row;

			AddSection(parameterSelector_, row, 0);
			row += parameterSelector_.RowCount;

			AddSection(optionsEditor_, row, 0);
		}

		public event EventHandler<EventArgs> IsValidChanged;

		public RADGroupByProtocolSettings Settings
		{
			get
			{
				return new RADGroupByProtocolSettings
				{
					GroupPrefix = groupPrefixTextBox_.Text,
					ProtocolName = parameterSelector_.ProtocolName,
					ProtocolVersion = parameterSelector_.ProtocolVersion,
					Parameters = parameterSelector_.SelectedParameters,
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
			IsValid = groupPrefixTextBox_.ValidationState == UIValidationState.Valid && parameterSelector_.SelectedParameters.Count > 0;
		}

		private void OnGroupPrefixTextBoxChanged()
		{
			groupPrefixTextBox_.ValidationState = string.IsNullOrEmpty(groupPrefixTextBox_.Text) ? UIValidationState.Invalid : UIValidationState.Valid;
			UpdateIsValid();
		}
	}
}
