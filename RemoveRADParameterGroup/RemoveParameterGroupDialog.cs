using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Utils.InteractiveAutomationScript;
using System;
using System.Configuration;

namespace RemoveRADParameterGroup
{
    public class RemoveParameterGroupDialog : Dialog
    {
        public string GroupName { get; private set; }
        public int DataMinerID { get; private set; }

        public event EventHandler Accepted;
        public event EventHandler Cancelled;

        public RemoveParameterGroupDialog(IEngine engine, string groupName, int dataMinerID) : base(engine)
        {
            GroupName = groupName;
            DataMinerID = dataMinerID;

            var label = new Label($"Are you sure you want to the parameter group '{groupName}' from Relational Anomaly Detection?");
            
            var noButton = new Button("No");
            noButton.Pressed += (sender, args) => Cancelled?.Invoke(this, EventArgs.Empty);

            var yesButton = new Button("Yes");
            yesButton.Pressed += (sender, args) => Accepted?.Invoke(this, EventArgs.Empty);

            AddWidget(label, 0, 0, 1, 2);
            AddWidget(noButton, 1, 0);
            AddWidget(yesButton, 1, 1);
        }
    }
}
