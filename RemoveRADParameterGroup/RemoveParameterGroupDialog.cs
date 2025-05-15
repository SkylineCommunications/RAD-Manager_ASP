namespace RemoveRADParameterGroup
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadWidgets;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class RemoveParameterGroupDialog : Dialog
    {
		public RemoveParameterGroupDialog(IEngine engine, List<RadGroupID> groupIDs) : base(engine)
		{
			ShowScriptAbortPopup = false;
			GroupIDs = groupIDs;

			Label label;
			if (groupIDs.Count == 1)
			{
				Title = "Remove parameter group";
				label = new Label($"Are you sure you want to remove the parameter group '{groupIDs[0].GroupName}' from Relational Anomaly Detection?");
			}
			else
			{
				Title = "Remove parameter groups";
				var groupNamesStr = groupIDs.Select(g => $"'{g.GroupName}'").HumanReadableJoin();
				label = new Label($"Are you sure you want to remove the parameter groups {groupNamesStr} from Relational Anomaly Detection?");
			}

			label.MaxWidth = 900;

			var noButton = new Button("No");
			noButton.Pressed += (sender, args) => Cancelled?.Invoke(this, EventArgs.Empty);

			var yesButton = new Button("Yes")
			{
				Style = ButtonStyle.CallToAction,
			};
			yesButton.Pressed += (sender, args) => Accepted?.Invoke(this, EventArgs.Empty);

			AddWidget(label, 0, 0, 1, 2);
			AddWidget(yesButton, 1, 0);
			AddWidget(noButton, 1, 1);
		}

		public event EventHandler Accepted;

		public event EventHandler Cancelled;

		public List<RadGroupID> GroupIDs { get; private set; }
    }
}
