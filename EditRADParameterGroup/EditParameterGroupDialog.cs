namespace EditRADParameterGroup
{
	using System;
	using System.Linq;
	using RADWidgets;
	using Skyline.DataMiner.Analytics.Mad;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class EditParameterGroupDialog : Dialog
	{
		private RADGroupEditor groupEditor_;
		private Button okButton_;

		public EditParameterGroupDialog(IEngine engine, RADGroupSettings groupSettings, int dataMinerID) : base(engine)
		{
			DataMinerID = dataMinerID;
			OriginalGroupName = groupSettings.GroupName;
			Title = $"Edit group '{groupSettings.GroupName}'";

			groupEditor_ = new RADGroupEditor(engine, groupSettings);
			groupEditor_.IsValidChanged += (sender, args) => UpdateEditGroupIsEnabled();

			okButton_ = new Button("Apply");
			okButton_.Pressed += (sender, args) => Accepted?.Invoke(this, EventArgs.Empty);

			var cancelButton = new Button("Cancel");
			cancelButton.Pressed += (sender, args) => Cancelled?.Invoke(this, EventArgs.Empty);

			UpdateEditGroupIsEnabled();

			int row = 0;
			AddSection(groupEditor_, row, 0);
			row += groupEditor_.RowCount;

			AddWidget(cancelButton, row, 0, 1, 1);
			AddWidget(okButton_, row, 1, 1, 3);
		}

		public event EventHandler Accepted;

		public event EventHandler Cancelled;

		public int DataMinerID { get; private set; }

		public string OriginalGroupName { get; private set; }

		public RADGroupSettings GroupSettings => groupEditor_.Settings;

		private void UpdateEditGroupIsEnabled()
		{
			okButton_.IsEnabled = groupEditor_.IsValid;
		}
	}
}
