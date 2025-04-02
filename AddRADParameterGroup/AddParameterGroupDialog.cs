namespace AddParameterGroup
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using AddRadParameterGroup;
	using RadWidgets;
	using Skyline.DataMiner.Analytics.Mad;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public enum AddGroupType
	{
		[Description("Add single group")]
		Single,
		[Description("Add group for each element with given connector")]
		MultipleOnProtocol,
	}

	public class AddParameterGroupDialog : Dialog
	{
		private readonly EnumDropDown<AddGroupType> addTypeDropDown_;
		private readonly RadGroupEditor groupEditor_;
		private readonly RadGroupByProtocolCreator groupByProtocolCreator_;
		private readonly Button okButton_;

		public AddParameterGroupDialog(IEngine engine) : base(engine)
		{
			ShowScriptAbortPopup = false;
			Title = "Add Parameter Group";

			var addTypeLabel = new Label("What to add?")
			{
				Tooltip = "Choose whether to add a single group, or multiple groups at once using the specified method.",
			};
			addTypeDropDown_ = new EnumDropDown<AddGroupType>()
			{
				Selected = AddGroupType.Single,
			};
			addTypeDropDown_.Changed += (sender, args) => OnAddTypeChanged();

			var existingGroupNames = Utils.FetchRadGroupNames(engine);
			groupEditor_ = new RadGroupEditor(engine, existingGroupNames);
			groupEditor_.ValidationChanged += (sender, args) => OnEditorValidationChanged(groupEditor_.IsValid, groupEditor_.ValidationText);

			groupByProtocolCreator_ = new RadGroupByProtocolCreator(engine, existingGroupNames);
			groupByProtocolCreator_.ValidationChanged += (sender, args) => OnEditorValidationChanged(groupByProtocolCreator_.IsValid, groupByProtocolCreator_.ValidationText);

			okButton_ = new Button()
			{
				Style = ButtonStyle.CallToAction,
			};
			okButton_.Pressed += (sender, args) => Accepted?.Invoke(this, EventArgs.Empty);

			var cancelButton = new Button("Cancel");
			cancelButton.Pressed += (sender, args) => Cancelled?.Invoke(this, EventArgs.Empty);

			OnAddTypeChanged();

			int row = 0;
			AddWidget(addTypeLabel, row, 0);
			AddWidget(addTypeDropDown_, row, 1, 1, groupByProtocolCreator_.ColumnCount - 1);
			++row;

			AddSection(groupEditor_, row, 0);
			row += groupEditor_.RowCount;

			AddSection(groupByProtocolCreator_, row, 0);
			row += groupByProtocolCreator_.RowCount;

			AddWidget(cancelButton, row, 0, 1, 1);
			AddWidget(okButton_, row, 1, 1, 3);
		}

		public event EventHandler Accepted;

		public event EventHandler Cancelled;

		public List<MADGroupInfo> GetGroupsToAdd()
		{
			if (addTypeDropDown_.Selected == AddGroupType.Single)
			{
				var groupInfo = new MADGroupInfo(
					groupEditor_.Settings.GroupName,
					groupEditor_.Settings.Parameters.ToList(),
					groupEditor_.Settings.Options.UpdateModel,
					groupEditor_.Settings.Options.AnomalyThreshold,
					groupEditor_.Settings.Options.MinimalDuration);
				return new List<MADGroupInfo>() { groupInfo };
			}
			else
			{
				return groupByProtocolCreator_.GetGroupsToAdd();
			}
		}

		private void OnEditorValidationChanged(bool isValid, string validationText)
		{
			if (isValid)
			{
				okButton_.IsEnabled = true;
				okButton_.Tooltip = string.Empty;
			}
			else
			{
				okButton_.IsEnabled = false;
				okButton_.Tooltip = validationText;
			}
		}

		private void OnAddTypeChanged()
		{
			if (addTypeDropDown_.Selected == AddGroupType.Single)
			{
				groupEditor_.IsVisible = true;
				groupByProtocolCreator_.IsVisible = false;
				okButton_.Text = "Add group";
				addTypeDropDown_.Tooltip = "Add the parameter group specified below.";
				OnEditorValidationChanged(groupEditor_.IsValid, groupEditor_.ValidationText);
			}
			else
			{
				groupEditor_.IsVisible = false;
				groupByProtocolCreator_.IsVisible = true;
				okButton_.Text = "Add group(s)";
				addTypeDropDown_.Tooltip = "Add a parameter group with the instances and options specified below for each element using the given connection and connector version.";
				OnEditorValidationChanged(groupByProtocolCreator_.IsValid, groupByProtocolCreator_.ValidationText);
			}
		}
	}
}
