namespace AddRadParameterGroup.GroupByProtocolCreator
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadWidgets.Widgets.Generic;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public abstract class RadGroupByProtocolDetailsItem : SelectorItem
	{
		protected RadGroupByProtocolDetailsItem(GroupByProtocolInfo groupByProtocolInfo)
		{
			GroupByProtocolInfo = groupByProtocolInfo;
		}

		public GroupByProtocolInfo GroupByProtocolInfo { get; set; }

		public override string GetKey()
		{
			return GroupByProtocolInfo.ElementName;
		}

		public abstract string GetFailureText();
	}

	public abstract class GroupsByProtocolDetailsDialog : Dialog
	{
		protected GroupsByProtocolDetailsDialog(IEngine engine) : base(engine)
		{
		}

		public abstract event EventHandler Closed;
	}

	public abstract class GroupsByProtocolDetailsDialog<T> : GroupsByProtocolDetailsDialog where T : RadGroupByProtocolDetailsItem
	{
		protected GroupsByProtocolDetailsDialog(IEngine engine, List<GroupByProtocolInfo> groups) : base(engine)
		{
			Title = "Overview";

			var validGroups = GetValidItems(groups);
			var invalidGroups = GetInvalidItems(groups);

			string labelText;
			if (validGroups.Count > 0 && invalidGroups.Count > 0)
			{
				labelText = $"Relational anomaly groups will be created for {validGroups.Count} elements, but no group will be created for {invalidGroups.Count}. Select an element " +
					$"below to see more details.";
			}
			else if (validGroups.Count > 0)
			{
				labelText = $"Relational anomaly groups will be created for {validGroups.Count} elements. Select an element below to see more details.";
			}
			else if (invalidGroups.Count > 0)
			{
				labelText = $"Relational anomaly groups can not be created for {invalidGroups.Count} elements. Select an element below to see more details.";
			}
			else
			{
				labelText = "No elements for the selected protocol found.";
			}

			var label = new WrappingLabel(labelText, 120);

			var groupsViewer = new DetailsViewer<T>(new GroupsByProtocolDetailsView<T>(), "Element", validGroups.Concat(invalidGroups).ToList());

			var closeButton = new Button("Close")
			{
				Style = ButtonStyle.CallToAction,
			};
			closeButton.Pressed += (sender, args) => Closed?.Invoke(this, EventArgs.Empty);

			int row = 0;
			AddWidget(label, 0, 0, 1, groupsViewer.ColumnCount);
			row++;

			AddSection(groupsViewer, 1, 0);
			row += groupsViewer.RowCount;

			AddWidget(closeButton, row, 0, 1, groupsViewer.ColumnCount);
		}

		public override event EventHandler Closed;

		protected abstract List<T> GetValidItems(List<GroupByProtocolInfo> groups);

		protected abstract List<T> GetInvalidItems(List<GroupByProtocolInfo> groups);
	}
}
