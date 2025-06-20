namespace AddRadParameterGroup.GroupByProtocolCreator
{
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Automation;

	public enum SharedModelGroupByProtocolStatus
	{
		ValidSubgroup,
		MoreThanMaxInstances,
		LessThanMinInstances,

		/// <summary>
		/// There is at least one item in the parameter selector that matches multiple instances.
		/// </summary>
		SelectorItemWithMultipleInstances,

		/// <summary>
		/// There is at least one item in the parameter selector that matches no instances.
		/// </summary>
		SelectorItemWithNoInstances,

		/// <summary>
		/// There are two items in the parameter selector that match the same instance.
		/// </summary>
		SelectorItemWithDuplicateInstances,
	}

	public class RadSharedModelGroupByProtocolDetailsItem : RadGroupByProtocolDetailsItem
	{
		public RadSharedModelGroupByProtocolDetailsItem(GroupByProtocolInfo groupByProtocolInfo) : base(groupByProtocolInfo)
		{
		}

		public override string GetDisplayValue()
		{
			switch (GroupByProtocolInfo.SharedModelGroupStatus)
			{
				case SharedModelGroupByProtocolStatus.ValidSubgroup:
					return GroupByProtocolInfo.ElementName;
				case SharedModelGroupByProtocolStatus.LessThanMinInstances:
					return $"{GroupByProtocolInfo.ElementName} (too few instances)";
				case SharedModelGroupByProtocolStatus.MoreThanMaxInstances:
					return $"{GroupByProtocolInfo.ElementName} (too many instances)";
				case SharedModelGroupByProtocolStatus.SelectorItemWithMultipleInstances:
				case SharedModelGroupByProtocolStatus.SelectorItemWithNoInstances:
				case SharedModelGroupByProtocolStatus.SelectorItemWithDuplicateInstances:
					return $"{GroupByProtocolInfo.ElementName} (failed matching instances)";
				default:
					return $"{GroupByProtocolInfo.ElementName} (unknown failure)";
			}
		}

		public override string GetFailureText()
		{
			switch (GroupByProtocolInfo.SharedModelGroupStatus)
			{
				case SharedModelGroupByProtocolStatus.ValidSubgroup:
					return string.Empty;
				case SharedModelGroupByProtocolStatus.LessThanMinInstances:
					return "No group can be created for this element, since too few instances have been selected.";
				case SharedModelGroupByProtocolStatus.MoreThanMaxInstances:
					return "No group can be created for this element, since too many instances have been selected.";
				case SharedModelGroupByProtocolStatus.SelectorItemWithMultipleInstances:
					return "No group can be created for this element, since some of the selected parameters match multiple instances. " +
						"This is not allowed when creating a shared model group.";
				case SharedModelGroupByProtocolStatus.SelectorItemWithNoInstances:
					return "No group can be created for this element, since some of the selected parameters do not match any instances. " +
						"This is not allowed when creating a shared model group.";
				case SharedModelGroupByProtocolStatus.SelectorItemWithDuplicateInstances:
					return "No group can be created for this element, since some parameters are matched multiple times. " +
						"This is not allowed when creating a shared model group.";
				default:
					return "No group can be created for this element for unknown reasons.";
			}
		}
	}

	public class SharedModelGroupsByProtocolDetailsDialog : GroupsByProtocolDetailsDialog<RadSharedModelGroupByProtocolDetailsItem>
	{
		public SharedModelGroupsByProtocolDetailsDialog(IEngine engine, List<GroupByProtocolInfo> groups) : base(engine, groups)
		{
		}

		protected override List<RadSharedModelGroupByProtocolDetailsItem> GetValidItems(List<GroupByProtocolInfo> groups)
		{
			return groups.Where(g => g.SharedModelGroupStatus == SharedModelGroupByProtocolStatus.ValidSubgroup)
				.OrderBy(g => g.ElementName)
				.Select(g => new RadSharedModelGroupByProtocolDetailsItem(g)).ToList();
		}

		protected override List<RadSharedModelGroupByProtocolDetailsItem> GetInvalidItems(List<GroupByProtocolInfo> groups)
		{
			return groups.Where(g => g.SharedModelGroupStatus != SharedModelGroupByProtocolStatus.ValidSubgroup)
				.OrderBy(g => g.ElementName)
				.Select(g => new RadSharedModelGroupByProtocolDetailsItem(g)).ToList();
		}
	}
}
