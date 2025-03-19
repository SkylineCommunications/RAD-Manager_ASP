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
		private bool parameterSelectorValid_ = false;
		private RADGroupOptionsEditor optionsEditor_;

		public RADGroupByProtocolCreator(IEngine engine)
		{
			groupPrefixLabel_ = new Label("Group name prefix");

			groupPrefixTextBox_ = new TextBox()
			{
				MinWidth = 600,
			};
			groupPrefixTextBox_.Changed += (sender, args) => OnGroupPrefixTextBoxChanged();
			groupPrefixTextBox_.ValidationText = "Provide a prefix";

			parameterSelector_ = new MultiParameterPerProtocolSelector(engine)
			{
				IsVisible = false,
			};
			parameterSelector_.Changed += (sender, args) => OnParameterSelectorChanged();

			optionsEditor_ = new RADGroupOptionsEditor(parameterSelector_.ColumnCount);

			OnGroupPrefixTextBoxChanged();
			OnParameterSelectorChanged();

			int row = 0;
			AddWidget(groupPrefixLabel_, row, 0);
			AddWidget(groupPrefixTextBox_, row, 1, 1, parameterSelector_.ColumnCount - 1);
			++row;

			AddSection(parameterSelector_, row, 0);
			row += parameterSelector_.RowCount;

			AddSection(optionsEditor_, row, 0);
		}

		public event EventHandler<EventArgs> ValidationChanged;

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

		public bool IsValid { get; private set; }

		public string ValidationText { get; private set; }

		private void UpdateIsValid()
		{
			IsValid = groupPrefixTextBox_.ValidationState == UIValidationState.Valid && parameterSelectorValid_;
			if (groupPrefixTextBox_.ValidationState == UIValidationState.Invalid && !parameterSelectorValid_)
				ValidationText = "Provide a group name prefix and select at least two instances";
			else if (groupPrefixTextBox_.ValidationState == UIValidationState.Invalid)
				ValidationText = "Provide a group name prefix";
			else if (!parameterSelectorValid_)
				ValidationText = "Select at least two instances";
			else
				ValidationText = string.Empty;

			ValidationChanged?.Invoke(this, EventArgs.Empty);
		}

		private void OnParameterSelectorChanged()
		{
			bool newState = parameterSelector_.SelectedParameters.Count > 0;
			if (newState != parameterSelectorValid_)
			{
				parameterSelectorValid_ = newState;
				UpdateIsValid();
			}
		}

		private void OnGroupPrefixTextBoxChanged()
		{
			UIValidationState newState = string.IsNullOrEmpty(groupPrefixTextBox_.Text) ? UIValidationState.Invalid : UIValidationState.Valid;
			if (newState != groupPrefixTextBox_.ValidationState)
			{
				groupPrefixTextBox_.ValidationState = newState;
				UpdateIsValid();
			}
		}
	}
}
