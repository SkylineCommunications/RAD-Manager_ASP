namespace AddRadParameterGroup
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using AddParameterGroup;
	using RadWidgets;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Analytics.Mad;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class GroupByProtocolInfo
	{
		public string ElementName { get; set; }

		public string GroupName { get; set; }

		public List<ParameterKey> ParameterKeys { get; set; }

		public bool MoreThanMinInstances => ParameterKeys.Count >= RadGroupEditor.MIN_PARAMETERS;

		public bool LessThanMaxInstances => ParameterKeys.Count <= RadGroupEditor.MAX_PARAMETERS;

		public bool ValidGroupName { get; set; }

		public bool Valid => MoreThanMinInstances && LessThanMaxInstances && ValidGroupName;
	}

	public class RadGroupByProtocolCreator : Section
	{
		private readonly IEngine engine_;
		private readonly TextBox groupPrefixTextBox_;
		private readonly MultiParameterPerProtocolSelector parameterSelector_;
		private readonly RadGroupOptionsEditor optionsEditor_;
		private readonly Label detailsLabel_;
		private readonly List<string> existingGroupNames_;

		public RadGroupByProtocolCreator(IEngine engine, List<string> existingGroupNames)
		{
			engine_ = engine;
			existingGroupNames_ = existingGroupNames;

			string groupPrefixTooltip = "The prefix for the group names. The resulting group name will be the prefix followed by the element name between brackets.";
			var groupPrefixLabel = new Label("Group name prefix");
			groupPrefixTextBox_ = new TextBox()
			{
				MinWidth = 600,
				Tooltip = groupPrefixTooltip,
			};
			groupPrefixTextBox_.Changed += (sender, args) => OnGroupPrefixTextBoxChanged();
			groupPrefixTextBox_.ValidationText = "Provide a valid prefix";

			parameterSelector_ = new MultiParameterPerProtocolSelector(engine)
			{
				IsVisible = false,
			};
			parameterSelector_.Changed += (sender, args) => OnParameterSelectorChanged();

			optionsEditor_ = new RadGroupOptionsEditor(parameterSelector_.ColumnCount);

			detailsLabel_ = new Label()
			{
				MaxWidth = 900,
			};

			OnGroupPrefixTextBoxChanged();

			int row = 0;
			AddWidget(groupPrefixLabel, row, 0);
			AddWidget(groupPrefixTextBox_, row, 1, 1, parameterSelector_.ColumnCount - 1);
			++row;

			AddSection(parameterSelector_, row, 0);
			row += parameterSelector_.RowCount;

			AddSection(optionsEditor_, row, 0);
			row += optionsEditor_.RowCount;

			AddWidget(detailsLabel_, row, 0, 1, parameterSelector_.ColumnCount, HorizontalAlignment.Stretch);
		}

		public event EventHandler<EventArgs> ValidationChanged;

		public bool IsValid { get; private set; }

		public string ValidationText { get; private set; }

		public List<MADGroupInfo> GetGroupsToAdd()
		{
			var groupInfos = GetSelectedGroupInfo();
			var groups = new List<MADGroupInfo>(groupInfos.Count);
			foreach (var p in groupInfos)
			{
				if (!p.Valid)
					continue;

				groups.Add(new MADGroupInfo(
					p.GroupName,
					p.ParameterKeys,
					optionsEditor_.Options.UpdateModel,
					optionsEditor_.Options.AnomalyThreshold,
					optionsEditor_.Options.MinimalDuration));
			}

			return groups;
		}

		private void UpdateIsValid()
		{
			if (groupPrefixTextBox_.ValidationState != UIValidationState.Valid)
			{
				IsValid = false;
				ValidationText = "Provide a valid group name prefix";
				ValidationChanged?.Invoke(this, EventArgs.Empty);
				return;
			}

			var groups = GetSelectedGroupInfo();
			UpdateDetailsLabel(groups);

			var hasValidGroup = groups.Any(g => g.Valid);
			IsValid = hasValidGroup;

			if (!IsValid)
				ValidationText = "Make sure at least one valid group will be added";
			else
				ValidationText = string.Empty;

			ValidationChanged?.Invoke(this, EventArgs.Empty);
		}

		private List<GroupByProtocolInfo> GetSelectedGroupInfo()
		{
			if (groupPrefixTextBox_.ValidationState != UIValidationState.Valid)
				return new List<GroupByProtocolInfo>();

			var elements = engine_.FindElementsByProtocol(parameterSelector_.ProtocolName, parameterSelector_.ProtocolVersion);
			if (elements == null || elements.Length == 0)
				return new List<GroupByProtocolInfo>();

			var groups = new List<GroupByProtocolInfo>();
			foreach (var element in elements)
			{
				var pKeys = new List<ParameterKey>();
				foreach (var parameter in parameterSelector_.GetSelectedParameters())
				{
					if (parameter.ParentTableID == null)
					{
						pKeys.Add(new ParameterKey(element.DmaId, element.ElementId, parameter.ParameterID));
					}
					else
					{
						var matchingInstances = Utils.FetchMatchingInstances(engine_, element.DmaId, element.ElementId, parameter.ParentTableID.Value, parameter.DisplayKeyFilter);
						pKeys.AddRange(matchingInstances.Select(i => new ParameterKey(element.DmaId, element.ElementId, parameter.ParameterID, i)));
					}
				}

				var groupName = $"{groupPrefixTextBox_.Text} ({element.ElementName})";
				groups.Add(new GroupByProtocolInfo()
				{
					ElementName = element.ElementName,
					GroupName = groupName,
					ParameterKeys = pKeys,
					ValidGroupName = !existingGroupNames_.Contains(groupName),
				});
			}

			return groups;
		}

		private void UpdateDetailsLabel(List<GroupByProtocolInfo> groups)
		{
			detailsLabel_.IsVisible = groupPrefixTextBox_.ValidationState == UIValidationState.Valid;
			if (!detailsLabel_.IsVisible)
				return;

			if (groups.Count == 0)
			{
				detailsLabel_.Text = "No elements found on the selected protocol";
				return;
			}

			var validGroups = groups.Where(g => g.Valid).ToList();
			var groupsWithInvalidName = groups.Where(g => !g.ValidGroupName).ToList();
			var groupsWithTooFewInstances = groups.Where(g => g.ValidGroupName && !g.MoreThanMinInstances).ToList();
			var groupsWithTooManyInstances = groups.Where(g => g.ValidGroupName && !g.LessThanMaxInstances).ToList();

			List<string> lines = new List<string>();
			if (validGroups.Count > 0)
			{
				lines.Add("The following groups will be created:");
				lines.AddRange(validGroups.OrderBy(g => g.GroupName).Select(g => $"\t'{g.GroupName}' with {g.ParameterKeys.Count} instances").Take(5));
				if (validGroups.Count > 5)
					lines.Add($"\t... and {validGroups.Count - 5} more");
			}

			if (groupsWithInvalidName.Count > 0)
			{
				lines.Add($"Not overwriting existing groups with the same name for {groupsWithInvalidName.Select(s => $"'{s.ElementName}'").HumanReadableJoin()}");
			}

			if (groupsWithTooFewInstances.Count > 0)
			{
				lines.Add($"Too few instances have been selected for {groupsWithTooFewInstances.Select(s => $"'{s.ElementName}'").HumanReadableJoin()}");
			}

			if (groupsWithTooManyInstances.Count > 0)
			{
				lines.Add($"Too many instances have been selected for {groupsWithTooManyInstances.Select(s => $"'{s.ElementName}'").HumanReadableJoin()}");
			}

			detailsLabel_.Text = string.Join("\n", lines);
		}

		private void OnParameterSelectorChanged()
		{
			UpdateIsValid();
		}

		private void OnGroupPrefixTextBoxChanged()
		{
			UIValidationState newState = string.IsNullOrEmpty(groupPrefixTextBox_.Text) ? UIValidationState.Invalid : UIValidationState.Valid;
			if (newState != groupPrefixTextBox_.ValidationState)
				groupPrefixTextBox_.ValidationState = newState;

			UpdateIsValid();
		}
	}
}
