namespace AddRadParameterGroup.GroupByProtocolCreator
{
	using System;
	using System.Collections.Generic;
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

			int row = 0;
			int columnCount = 1;
			if (validGroups.Count > 0)
			{
				var validLabel = new Label("The following relational anomaly groups will be created for the following elements:");
				var validGroupsViewer = new DetailsViewer<T>(new GroupsByProtocolDetailsView<T>(), items: validGroups);
				columnCount = validGroupsViewer.ColumnCount;

				AddWidget(validLabel, row, 0, 1, validGroupsViewer.ColumnCount);
				row++;

				AddSection(validGroupsViewer, row, 0);
				row += validGroupsViewer.RowCount;
			}

			if (validGroups.Count > 0 && invalidGroups.Count > 0)
			{
				var whitespace = new WhiteSpace()
				{
					MinHeight = 10,
				};

				AddWidget(whitespace, row, 0);
				row++;
			}

			if (invalidGroups.Count > 0)
			{
				var invalidLabel = new Label("Relational anomaly groups can not be created for the following elements:");
				var invalidGroupsViewer = new DetailsViewer<T>(new GroupsByProtocolDetailsView<T>(), items: invalidGroups);
				columnCount = invalidGroupsViewer.ColumnCount;

				AddWidget(invalidLabel, row, 0, 1, invalidGroupsViewer.ColumnCount);
				row++;

				AddSection(invalidGroupsViewer, row, 0);
				row += invalidGroupsViewer.RowCount;
			}

			var closeButton = new Button("Close")
			{
				Style = ButtonStyle.CallToAction,
			};
			closeButton.Pressed += (sender, args) => Closed?.Invoke(this, EventArgs.Empty);
			AddWidget(closeButton, row, 0, 1, columnCount);
		}

		public override event EventHandler Closed;

		protected abstract List<T> GetValidItems(List<GroupByProtocolInfo> groups);

		protected abstract List<T> GetInvalidItems(List<GroupByProtocolInfo> groups);
	}
}
