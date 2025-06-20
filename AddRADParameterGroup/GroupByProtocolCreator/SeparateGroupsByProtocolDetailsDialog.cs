namespace AddRadParameterGroup.GroupByProtocolCreator
{
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Automation;

	public enum SeparateGroupsByProtocolStatus
	{
		ValidGroup,
		MoreThanMaxInstances,
		LessThanMinInstances,
		GroupNameExists,
	}

	public class RadSeparateGroupByProtocolDetailsItem : RadGroupByProtocolDetailsItem
	{
		public RadSeparateGroupByProtocolDetailsItem(GroupByProtocolInfo groupByProtocolInfo) : base(groupByProtocolInfo)
		{
		}

		public override string GetDisplayValue()
		{
			switch (GroupByProtocolInfo.SeparateGroupStatus)
			{
				case SeparateGroupsByProtocolStatus.ValidGroup:
					return GroupByProtocolInfo.ElementName;
				case SeparateGroupsByProtocolStatus.GroupNameExists:
					return $"{GroupByProtocolInfo.ElementName} (group name exists)";
				case SeparateGroupsByProtocolStatus.LessThanMinInstances:
					return $"{GroupByProtocolInfo.ElementName} (too few instances)";
				case SeparateGroupsByProtocolStatus.MoreThanMaxInstances:
					return $"{GroupByProtocolInfo.ElementName} (too many instances)";
				default:
					return $"{GroupByProtocolInfo.ElementName} (unknown failure)";
			}
		}

		public override string GetFailureText()
		{
			switch (GroupByProtocolInfo.SeparateGroupStatus)
			{
				case SeparateGroupsByProtocolStatus.ValidGroup:
					return string.Empty;
				case SeparateGroupsByProtocolStatus.GroupNameExists:
					return $"No group can be created for this element, since a group with the name '{GroupByProtocolInfo.GroupName}' already exists.";
				case SeparateGroupsByProtocolStatus.MoreThanMaxInstances:
					return "No group can be created for this element, since too many instances have been selected.";
				case SeparateGroupsByProtocolStatus.LessThanMinInstances:
					return "No group can be created for this element, since too few instances have been selected.";
				default:
					return "No group can be created for this element for unknown reasons.";
			}
		}
	}

	public class SeparateGroupsByProtocolDetailsDialog : GroupsByProtocolDetailsDialog<RadSeparateGroupByProtocolDetailsItem>
	{
		public SeparateGroupsByProtocolDetailsDialog(IEngine engine, List<GroupByProtocolInfo> groups) : base(engine, groups)
		{
		}

		protected override List<RadSeparateGroupByProtocolDetailsItem> GetValidItems(List<GroupByProtocolInfo> groups)
		{
			return groups.Where(g => g.SeparateGroupStatus == SeparateGroupsByProtocolStatus.ValidGroup)
				.OrderBy(g => g.ElementName)
				.Select(g => new RadSeparateGroupByProtocolDetailsItem(g)).ToList();
		}

		protected override List<RadSeparateGroupByProtocolDetailsItem> GetInvalidItems(List<GroupByProtocolInfo> groups)
		{
			return groups.Where(g => g.SeparateGroupStatus != SeparateGroupsByProtocolStatus.ValidGroup)
				.OrderBy(g => g.ElementName)
				.Select(g => new RadSeparateGroupByProtocolDetailsItem(g)).ToList();
		}
	}
}
