namespace EditRADParameterGroup
{
	using System;
	using RadWidgets;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class EditParameterGroupDialog : Dialog
	{
		private readonly RadGroupEditor groupEditor_;
		private readonly Button okButton_;

		public EditParameterGroupDialog(IEngine engine, RadGroupSettings groupSettings, int dataMinerID) : base(engine)
		{
			ShowScriptAbortPopup = false;
			DataMinerID = dataMinerID;
			OriginalGroupName = groupSettings.GroupName;
			Title = $"Edit group '{groupSettings.GroupName}'";

			groupEditor_ = new RadGroupEditor(engine, Utils.FetchRadGroupNames(engine), groupSettings);
			groupEditor_.ValidationChanged += (sender, args) => OnGroupEditorValidationChanged();

			okButton_ = new Button("Apply")
			{
				Style = ButtonStyle.CallToAction,
			};
			okButton_.Pressed += (sender, args) => Accepted?.Invoke(this, EventArgs.Empty);

			var cancelButton = new Button("Cancel");
			cancelButton.Pressed += (sender, args) => Cancelled?.Invoke(this, EventArgs.Empty);

			OnGroupEditorValidationChanged();

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

		public RadGroupSettings GroupSettings => groupEditor_.Settings;

		private void OnGroupEditorValidationChanged()
		{
			if (groupEditor_.IsValid)
			{
				okButton_.IsEnabled = true;
				okButton_.Tooltip = "Edit the selected parameter group as specified above";
			}
			else
			{
				okButton_.IsEnabled = false;
				okButton_.Tooltip = groupEditor_.ValidationText;
			}
		}
	}
}
