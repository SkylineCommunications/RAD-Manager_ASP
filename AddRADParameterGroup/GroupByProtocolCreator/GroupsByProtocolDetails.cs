namespace AddRadParameterGroup.GroupByProtocolCreator
{
	using System.Collections.Generic;
	using System.Linq;
	using RadWidgets;
	using RadWidgets.Widgets.Generic;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

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

	public class GroupsByProtocolDetails : VisibilitySection
	{
		private const int WordWrapLength = 150;
		private readonly Label _detailsLabel;
		private readonly CollapseButton _moreDetailsButton;
		private readonly DetailsViewer<RadGroupByProtocolDetailsItem> _groupsViewer;

		public GroupsByProtocolDetails(int columnSpan) : base()
		{
			_detailsLabel = new Label();

			_groupsViewer = new DetailsViewer<RadGroupByProtocolDetailsItem>(new GroupsByProtocolDetailsView(columnSpan), "Element");

			_moreDetailsButton = new SectionCollapseButton(new List<Section>() { _groupsViewer }, true)
			{
				Tooltip = "View more details about the groups that will be created.",
				CollapseText = "Hide details",
				ExpandText = "View more details",
			};

			AddWidget(_detailsLabel, 0, 0, 1, columnSpan - 1);
			AddWidget(_moreDetailsButton, 0, columnSpan - 1, 1, 1, verticalAlignment: VerticalAlignment.Bottom);
			AddSection(_groupsViewer, 2, 0, () => !_moreDetailsButton.IsCollapsed);
		}

		public void Update(List<GroupByProtocolInfo> groups, bool sharedModelGroup)
		{
			if (groups.Count == 0)
			{
				_detailsLabel.Text = "No elements found on the selected protocol";
				_moreDetailsButton.IsEnabled = false;
				return;
			}

			List<string> lines;
			if (sharedModelGroup)
				lines = GetDetailsLabelTextSharedModelGroup(groups);
			else
				lines = GetDetailsLabelTextSeparateGroups(groups);

			_detailsLabel.Text = string.Join("\n", lines);
			_moreDetailsButton.IsEnabled = true;

			UpdateGroupsViewer(groups, sharedModelGroup);
		}

		private List<string> GetValidGroupsText(IEnumerable<GroupByProtocolInfo> groups, bool sharedModelGroup)
		{
			List<string> lines = new List<string>();

			int nrOfGroups = groups.Count();
			if (nrOfGroups <= 0)
				return lines;

			if (sharedModelGroup)
				lines.Add($"The following shared model group will be created with {nrOfGroups} subgroups:");
			else
				lines.Add($"The following groups will be created:");
			lines.AddRange(groups.OrderBy(g => g.GroupName)
				.Take(5)
				.SelectMany(g => $"'{g.GroupName}' with {g.ParameterKeys.Count} instances".WordWrap(WordWrapLength))
				.Select(s => $"\t{s}"));
			if (nrOfGroups > 6)
				lines.Add($"\t... and {nrOfGroups - 5} more");

			return lines;
		}

		private List<string> GetGroupsWithInvalidNamesText(IEnumerable<GroupByProtocolInfo> groups)
		{
			if (!groups.Any())
				return new List<string>();

			return $"Not overwriting existing groups with the same name for {groups.Select(s => $"'{s.ElementName}'").HumanReadableJoin()}".WordWrap(WordWrapLength);
		}

		private List<string> GetGroupsWithTooFewInstancesText(IEnumerable<GroupByProtocolInfo> groups)
		{
			if (!groups.Any())
				return new List<string>();

			return $"Too few instances have been selected, or instances are not trended for {groups.Select(s => $"'{s.ElementName}'").HumanReadableJoin()}".WordWrap(WordWrapLength);
		}

		private List<string> GetGroupsWithTooManyInstancesText(IEnumerable<GroupByProtocolInfo> groups)
		{
			if (!groups.Any())
				return new List<string>();

			return $"Too many instances have been selected for {groups.Select(s => $"'{s.ElementName}'").HumanReadableJoin()}".WordWrap(WordWrapLength);
		}

		private List<string> GetGroupsWithMultipleInstancesPerSelectorItemText(IEnumerable<GroupByProtocolInfo> groups)
		{
			if (!groups.Any())
				return new List<string>();

			return $"Some parameters selected above match multiple instances on {groups.Select(s => $"'{s.ElementName}'").HumanReadableJoin()}".WordWrap(WordWrapLength);
		}

		private List<string> GetGroupsWithNoInstancesPerSelectorItemText(IEnumerable<GroupByProtocolInfo> groups)
		{
			if (!groups.Any())
				return new List<string>();

			return $"Some parameters selected above match no instances on {groups.Select(s => $"'{s.ElementName}'").HumanReadableJoin()}".WordWrap(WordWrapLength);
		}

		private List<string> GetDetailsLabelTextSharedModelGroup(List<GroupByProtocolInfo> groups)
		{
			List<string> lines = new List<string>();
			lines.AddRange(GetValidGroupsText(groups.Where(g => g.SharedModelGroupStatus == SharedModelGroupByProtocolStatus.ValidSubgroup), false));
			lines.AddRange(GetGroupsWithTooFewInstancesText(groups.Where(g => g.SharedModelGroupStatus == SharedModelGroupByProtocolStatus.LessThanMinInstances)));
			lines.AddRange(GetGroupsWithTooManyInstancesText(groups.Where(g => g.SharedModelGroupStatus == SharedModelGroupByProtocolStatus.MoreThanMaxInstances)));
			lines.AddRange(GetGroupsWithMultipleInstancesPerSelectorItemText(groups.Where(g => g.SharedModelGroupStatus == SharedModelGroupByProtocolStatus.SelectorItemWithMultipleInstances)));
			lines.AddRange(GetGroupsWithNoInstancesPerSelectorItemText(groups.Where(g => g.SharedModelGroupStatus == SharedModelGroupByProtocolStatus.SelectorItemWithNoInstances)));
			lines.AddRange(GetGroupsWithInvalidNamesText(groups.Where(g => g.SharedModelGroupStatus == SharedModelGroupByProtocolStatus.SelectorItemWithDuplicateInstances)));

			return lines;
		}

		private List<string> GetDetailsLabelTextSeparateGroups(List<GroupByProtocolInfo> groups)
		{
			List<string> lines = new List<string>();
			lines.AddRange(GetValidGroupsText(groups.Where(g => g.SeparateGroupStatus == SeparateGroupsByProtocolStatus.ValidGroup), false));
			lines.AddRange(GetGroupsWithInvalidNamesText(groups.Where(g => g.SeparateGroupStatus == SeparateGroupsByProtocolStatus.GroupNameExists)));
			lines.AddRange(GetGroupsWithTooFewInstancesText(groups.Where(g => g.SeparateGroupStatus == SeparateGroupsByProtocolStatus.LessThanMinInstances)));
			lines.AddRange(GetGroupsWithTooManyInstancesText(groups.Where(g => g.SeparateGroupStatus == SeparateGroupsByProtocolStatus.MoreThanMaxInstances)));

			return lines;
		}

		private void UpdateGroupsViewer(List<GroupByProtocolInfo> groups, bool sharedModelGroups)
		{
			List<RadGroupByProtocolDetailsItem> validGroups;
			List<RadGroupByProtocolDetailsItem> invalidGroups;
			if (sharedModelGroups)
			{
				validGroups = GetValidSharedModelGroups(groups);
				invalidGroups = GetInvalidSharedModelGroups(groups);
			}
			else
			{
				validGroups = GetValidSeparateGroups(groups);
				invalidGroups = GetInvalidSeparateGroups(groups);
			}

			_groupsViewer.SetItems(validGroups.Concat(invalidGroups).ToList(), _groupsViewer.GetSelected()?.GetKey());
		}

		private List<RadGroupByProtocolDetailsItem> GetValidSharedModelGroups(List<GroupByProtocolInfo> groups)
		{
			return groups.Where(g => g.SharedModelGroupStatus == SharedModelGroupByProtocolStatus.ValidSubgroup)
				.OrderBy(g => g.ElementName)
				.Select(g => new RadSharedModelGroupByProtocolDetailsItem(g))
				.OfType<RadGroupByProtocolDetailsItem>().ToList();
		}

		private List<RadGroupByProtocolDetailsItem> GetInvalidSharedModelGroups(List<GroupByProtocolInfo> groups)
		{
			return groups.Where(g => g.SharedModelGroupStatus != SharedModelGroupByProtocolStatus.ValidSubgroup)
				.OrderBy(g => g.ElementName)
				.Select(g => new RadSharedModelGroupByProtocolDetailsItem(g))
				.OfType<RadGroupByProtocolDetailsItem>().ToList();
		}

		private List<RadGroupByProtocolDetailsItem> GetValidSeparateGroups(List<GroupByProtocolInfo> groups)
		{
			return groups.Where(g => g.SeparateGroupStatus == SeparateGroupsByProtocolStatus.ValidGroup)
				.OrderBy(g => g.ElementName)
				.Select(g => new RadSeparateGroupByProtocolDetailsItem(g))
				.OfType<RadGroupByProtocolDetailsItem>().ToList();
		}

		private List<RadGroupByProtocolDetailsItem> GetInvalidSeparateGroups(List<GroupByProtocolInfo> groups)
		{
			return groups.Where(g => g.SeparateGroupStatus != SeparateGroupsByProtocolStatus.ValidGroup)
				.OrderBy(g => g.ElementName)
				.Select(g => new RadSeparateGroupByProtocolDetailsItem(g))
				.OfType<RadGroupByProtocolDetailsItem>().ToList();
		}
	}
}
