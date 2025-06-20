namespace AddRadParameterGroup.GroupByProtocolCreator
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.Remoting.Channels;
	using RadUtils;
	using RadWidgets;
	using RadWidgets.Widgets;
	using RadWidgets.Widgets.Editors;
	using RadWidgets.Widgets.Generic;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;
	using Skyline.DataMiner.Utils.RadToolkit;

	public class ParameterSelectorItemMatchInfo
	{
		public ProtocolParameterSelectorInfo SelectorItem { get; set; }

		public List<ParameterKey> MatchingParameters { get; set; }
	}

	public class GroupByProtocolInfo
	{
		public GroupByProtocolInfo(string elementName, string groupName, List<ParameterSelectorItemMatchInfo> parameters, bool groupNameExists)
		{
			ElementName = elementName;
			GroupName = groupName;

			var comparer = new ParameterKeyEqualityComparer();
			ParameterMatches = parameters;
			ParameterKeys = parameters.SelectMany(p => p.MatchingParameters).Distinct(comparer).ToList();

			SetSeparateGroupStatus(groupNameExists, ParameterKeys.Count);
			SetSharedModelGroupStatus(ParameterKeys.Count, parameters);
		}

		public string ElementName { get; set; }

		public string GroupName { get; set; }

		public List<ParameterSelectorItemMatchInfo> ParameterMatches { get; private set; }

		public List<ParameterKey> ParameterKeys { get; private set; }

		public SeparateGroupsByProtocolStatus SeparateGroupStatus { get; private set; }

		public SharedModelGroupByProtocolStatus SharedModelGroupStatus { get; private set; }

		private void SetSeparateGroupStatus(bool groupNameExists, int parameterCount)
		{
			if (groupNameExists)
				SeparateGroupStatus = SeparateGroupsByProtocolStatus.GroupNameExists;
			else if (parameterCount > RadGroupEditor.MAX_PARAMETERS)
				SeparateGroupStatus = SeparateGroupsByProtocolStatus.MoreThanMaxInstances;
			else if (parameterCount < RadGroupEditor.MIN_PARAMETERS)
				SeparateGroupStatus = SeparateGroupsByProtocolStatus.LessThanMinInstances;
			else
				SeparateGroupStatus = SeparateGroupsByProtocolStatus.ValidGroup;
		}

		private void SetSharedModelGroupStatus(int parameterCount, List<ParameterSelectorItemMatchInfo> parameters)
		{
			if (parameterCount < RadGroupEditor.MIN_PARAMETERS)
				SharedModelGroupStatus = SharedModelGroupByProtocolStatus.LessThanMinInstances;
			else if (parameterCount > RadGroupEditor.MAX_PARAMETERS)
				SharedModelGroupStatus = SharedModelGroupByProtocolStatus.MoreThanMaxInstances;
			else if (parameters.Any(p => p.MatchingParameters.Count > 1))
				SharedModelGroupStatus = SharedModelGroupByProtocolStatus.SelectorItemWithMultipleInstances;
			else if (parameters.Any(p => p.MatchingParameters.Count == 0))
				SharedModelGroupStatus = SharedModelGroupByProtocolStatus.SelectorItemWithNoInstances;
			else if (parameters.Where(p => p.MatchingParameters.Count == 1).GroupBy(p => p.MatchingParameters?.First(), new ParameterKeyEqualityComparer()).Any(g => g.Count() > 1))
				SharedModelGroupStatus = SharedModelGroupByProtocolStatus.SelectorItemWithDuplicateInstances;
			else
				SharedModelGroupStatus = SharedModelGroupByProtocolStatus.ValidSubgroup;
		}
	}

	public class GroupByProtocolCreatorWidget : VisibilitySection
	{
		private const int WordWrapLength = 150;
		private readonly IEngine _engine;
		private readonly ParametersCache _parametersCache;
		private readonly List<string> _existingGroupNames;
		private readonly Label _groupPrefixLabel;
		private readonly TextBox _groupPrefixTextBox;
		private readonly MultiParameterPerProtocolSelector _parameterSelector;
		private readonly CheckBox _sharedModelCheckBox = null;
		private readonly RadGroupOptionsEditor _optionsEditor;
		private readonly WhiteSpace _whiteSpace;
		private readonly Label _detailsLabel;
		private readonly Button _moreDetailsButton;

		public GroupByProtocolCreatorWidget(IEngine engine, List<string> existingGroupNames, ParametersCache parametersCache)
		{
			_engine = engine;
			_parametersCache = parametersCache;
			_existingGroupNames = existingGroupNames;

			_groupPrefixLabel = new Label();
			_groupPrefixTextBox = new TextBox()
			{
				MinWidth = 600,
			};
			_groupPrefixTextBox.Changed += (sender, args) => OnGroupPrefixTextBoxChanged();

			_parameterSelector = new MultiParameterPerProtocolSelector(engine)
			{
				IsVisible = false,
			};
			_parameterSelector.Changed += (sender, args) => OnParameterSelectorChanged();

			var parameterProtocolWhiteSpace = new WhiteSpace()
			{
				MinWidth = 10,
			};

			if (engine.GetRadHelper().AllowSharedModelGroups)
			{
				_sharedModelCheckBox = new CheckBox("Share model between elements")
				{
					Tooltip = "If checked, one shared model group will be created with subgroups for each element. If unchecked, separate groups will be created for " +
					"each element.",
				};
				_sharedModelCheckBox.Changed += (sender, args) => OnSharedModelCheckBoxChanged();
			}

			_optionsEditor = new RadGroupOptionsEditor(_parameterSelector.ColumnCount);

			_whiteSpace = new WhiteSpace()
			{
				MinHeight = 10,
			};

			_detailsLabel = new Label();

			_moreDetailsButton = new Button("More details...");
			_moreDetailsButton.Pressed += (sender, args) => OnMoreDetailsButtonPressed();

			OnGroupPrefixTextBoxChanged();
			OnSharedModelCheckBoxChanged();

			int row = 0;
			AddWidget(_groupPrefixLabel, row, 0);
			AddWidget(_groupPrefixTextBox, row, 1, 1, _parameterSelector.ColumnCount - 1);
			++row;

			// Note: this whitespace is to avoid the second column from being very narrow, since there is no widget that only spans that column.
			AddWidget(parameterProtocolWhiteSpace, row, 1);
			++row;

			AddSection(_parameterSelector, row, 0);
			row += _parameterSelector.RowCount;

			if (_sharedModelCheckBox != null)
			{
				AddWidget(_sharedModelCheckBox, row, 0, 1, _parameterSelector.ColumnCount);
				row++;
			}

			AddSection(_optionsEditor, row, 0);
			row += _optionsEditor.RowCount;

			AddWidget(_whiteSpace, row, 0, 1, _parameterSelector.ColumnCount, GetDetailsLabelVisibility);
			row += 1;

			AddWidget(_detailsLabel, row, 0, 1, _parameterSelector.ColumnCount - 1, GetDetailsLabelVisibility);
			AddWidget(_moreDetailsButton, new WidgetLayout(row, _parameterSelector.ColumnCount - 1, verticalAlignment: VerticalAlignment.Bottom), GetDetailsLabelVisibility);
		}

		public event EventHandler<EventArgs> ValidationChanged;

		public bool IsValid { get; private set; }

		public string ValidationText { get; private set; }

		public List<RadGroupSettings> GetGroupsToAdd()
		{
			var groupInfos = GetSelectedGroupInfo();

			if (_sharedModelCheckBox?.IsChecked == true)
			{
				var subgroups = new List<RadSubgroupSettings>(groupInfos.Count);
				foreach (var g in groupInfos)
				{
					if (g.SeparateGroupStatus != SeparateGroupsByProtocolStatus.ValidGroup)
						continue;

					var subgroup = new RadSubgroupSettings(g.GroupName, Guid.NewGuid(),
						g.ParameterKeys.Select(p => new RadParameter(p, null)).ToList(), new RadSubgroupOptions());
					subgroups.Add(subgroup);
				}

				var group = new RadGroupSettings(_groupPrefixTextBox.Text, _optionsEditor.Options, subgroups);
				return new List<RadGroupSettings>() { group };
			}
			else
			{
				var groups = new List<RadGroupSettings>(groupInfos.Count);
				foreach (var g in groupInfos)
				{
					if (g.SharedModelGroupStatus != SharedModelGroupByProtocolStatus.ValidSubgroup)
						continue;

					var subgroup = new RadSubgroupSettings(g.GroupName, Guid.NewGuid(),
						g.ParameterKeys.Select(p => new RadParameter(p, null)).ToList(), new RadSubgroupOptions());
					var group = new RadGroupSettings(g.GroupName, _optionsEditor.Options, new List<RadSubgroupSettings> { subgroup });
					groups.Add(group);
				}

				return groups;
			}
		}

		private void UpdateIsValid()
		{
			if (_groupPrefixTextBox.ValidationState != UIValidationState.Valid)
			{
				IsValid = false;
				ValidationText = "Provide a valid group name prefix";
				ValidationChanged?.Invoke(this, EventArgs.Empty);
				return;
			}

			var groups = GetSelectedGroupInfo();
			UpdateDetailsLabel(groups);

			bool hasValidGroup;
			if (_sharedModelCheckBox?.IsChecked == true)
				hasValidGroup = groups.Any(g => g.SeparateGroupStatus == SeparateGroupsByProtocolStatus.ValidGroup);
			else
				hasValidGroup = groups.Any(g => g.SharedModelGroupStatus == SharedModelGroupByProtocolStatus.ValidSubgroup);
			IsValid = hasValidGroup;

			if (!IsValid)
				ValidationText = "Make sure at least one valid group will be added";
			else
				ValidationText = string.Empty;

			ValidationChanged?.Invoke(this, EventArgs.Empty);
		}

		private void UpdateGroupPrefixCheckboxValidity()
		{
			if (string.IsNullOrWhiteSpace(_groupPrefixTextBox.Text))
			{
				_groupPrefixTextBox.ValidationState = UIValidationState.Invalid;
				_groupPrefixTextBox.ValidationText = _sharedModelCheckBox?.IsChecked == true ? "Provide a group name" : "Provide a group name prefix";
			}
			else if (_sharedModelCheckBox?.IsChecked == true && _existingGroupNames.Any(s => string.Equals(s, _groupPrefixTextBox.Text, StringComparison.OrdinalIgnoreCase)))
			{
				_groupPrefixTextBox.ValidationState = UIValidationState.Invalid;
				_groupPrefixTextBox.ValidationText = "Group name already exists";
			}
			else
			{
				_groupPrefixTextBox.ValidationState = UIValidationState.Valid;
				_groupPrefixTextBox.ValidationText = string.Empty;
			}
		}

		private List<ParameterSelectorItemMatchInfo> GetSelectedParametersForElement(Element element)
		{
			if (!_parametersCache.TryGet(element.DmaId, element.ElementId, out var parametersOnElement))
			{
				_engine.Log($"Could not find parameters for element {element.ElementName} ({element.DmaId}/{element.ElementId})");
				return new List<ParameterSelectorItemMatchInfo>();
			}

			var items = new List<ParameterSelectorItemMatchInfo>();
			foreach (var parameter in _parameterSelector.GetSelectedParameters())
			{
				List<ParameterKey> matchingKeys;

				var paramInfo = parametersOnElement.FirstOrDefault(p => p.ID == parameter.ParameterID);
				if (paramInfo == null)
				{
					_engine.Log($"Could not find parameter {parameter.ParameterID} on element {element.ElementName} ({element.DmaId}/{element.ElementId})");
					matchingKeys = new List<ParameterKey>();
				}
				else if (!paramInfo.HasTrending())
				{
					matchingKeys = new List<ParameterKey>();
				}
				else if (parameter.ParentTableID == null)
				{
					matchingKeys = new List<ParameterKey>() { new ParameterKey(element.DmaId, element.ElementId, parameter.ParameterID) };
				}
				else
				{
					var matchingInstances = RadWidgets.Utils.FetchInstancesWithTrending(_engine, element.DmaId, element.ElementId, paramInfo, parameter.DisplayKeyFilter);
					matchingKeys = matchingInstances.Select(i => new ParameterKey(element.DmaId, element.ElementId, parameter.ParameterID, i.IndexValue, i.DisplayValue)).ToList();
				}

				var info = new ParameterSelectorItemMatchInfo()
				{
					SelectorItem = parameter,
					MatchingParameters = matchingKeys,
				};
				items.Add(info);
			}

			return items;
		}

		private List<GroupByProtocolInfo> GetSelectedGroupInfo()
		{
			if (_groupPrefixTextBox.ValidationState != UIValidationState.Valid)
				return new List<GroupByProtocolInfo>();

			var elements = _engine.FindElementsByProtocol(_parameterSelector.ProtocolName, _parameterSelector.ProtocolVersion);
			if (elements == null || elements.Length == 0)
				return new List<GroupByProtocolInfo>();

			var groups = new List<GroupByProtocolInfo>();
			foreach (var element in elements)
			{
				var parameters = GetSelectedParametersForElement(element);
				var groupName = $"{_groupPrefixTextBox.Text} ({element.ElementName})";
				groups.Add(new GroupByProtocolInfo(element.ElementName, groupName, parameters, _existingGroupNames.Contains(groupName)));
			}

			return groups;
		}

		private bool GetDetailsLabelVisibility()
		{
			return _groupPrefixTextBox.ValidationState == UIValidationState.Valid;
		}

		private void UpdateDetailsLabelVisibility()
		{
			var visibility = GetDetailsLabelVisibility();
			_whiteSpace.IsVisible = IsSectionVisible && visibility;
			_detailsLabel.IsVisible = IsSectionVisible && visibility;
			_moreDetailsButton.IsVisible = IsSectionVisible && visibility;
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

		private void UpdateDetailsLabel(List<GroupByProtocolInfo> groups)
		{
			UpdateDetailsLabelVisibility();
			if (!_detailsLabel.IsVisible)
				return;

			if (groups.Count == 0)
			{
				_detailsLabel.Text = "No elements found on the selected protocol";
				_moreDetailsButton.IsEnabled = false;
				return;
			}

			bool sharedModelGroup = _sharedModelCheckBox?.IsChecked ?? false;
			List<string> lines;
			if (sharedModelGroup)
				lines = GetDetailsLabelTextSharedModelGroup(groups);
			else
				lines = GetDetailsLabelTextSeparateGroups(groups);

			_detailsLabel.Text = string.Join("\n", lines);
			_moreDetailsButton.IsEnabled = true;
		}

		private void OnParameterSelectorChanged()
		{
			UpdateIsValid();
		}

		private void OnGroupPrefixTextBoxChanged()
		{
			UpdateGroupPrefixCheckboxValidity();
			UpdateDetailsLabelVisibility();
			UpdateIsValid();
		}

		private void OnSharedModelCheckBoxChanged()
		{
			if (_sharedModelCheckBox?.IsChecked == true)
			{
				_groupPrefixLabel.Text = "Group name";
				var tooltip = "The name of the shared model group. Each subgroup's name will be this name followed by the element name between brackets";
				_groupPrefixLabel.Tooltip = tooltip;
				_groupPrefixTextBox.Tooltip = tooltip;
			}
			else
			{
				_groupPrefixLabel.Text = "Group name prefix";
				var tooltip = "The prefix for the group names. The resulting group name will be the prefix followed by the element name between brackets.";
				_groupPrefixLabel.Tooltip = tooltip;
				_groupPrefixTextBox.Tooltip = tooltip;
			}

			UpdateGroupPrefixCheckboxValidity();
			UpdateDetailsLabelVisibility();
			UpdateIsValid();
		}

		private void OnMoreDetailsButtonPressed()
		{
			var groups = GetSelectedGroupInfo();
			if (groups.Count <= 0)
				return;

			GroupsByProtocolDetailsDialog dialog;
			if (_sharedModelCheckBox?.IsChecked ?? false)
				dialog = new SharedModelGroupsByProtocolDetailsDialog(_engine, groups);
			else
				dialog = new SeparateGroupsByProtocolDetailsDialog(_engine, groups);

			InteractiveController app = new InteractiveController(_engine);
			dialog.Closed += (sender, args) => app.Stop();
			app.ShowDialog(dialog);
		}
	}
}
