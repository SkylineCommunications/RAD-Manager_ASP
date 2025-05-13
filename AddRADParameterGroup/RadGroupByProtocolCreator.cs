namespace AddRadParameterGroup
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using AddParameterGroup;
	using RadUtils;
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

	public class RadGroupByProtocolCreator : VisibilitySection
	{
		private readonly IEngine _engine;
		private readonly ParametersCache _parametersCache;
		private readonly List<string> _existingGroupNames;
		private readonly Label _groupPrefixLabel;
		private readonly TextBox _groupPrefixTextBox;
		private readonly MultiParameterPerProtocolSelector _parameterSelector;
		private readonly RadGroupOptionsEditor _optionsEditor;
		private readonly Label _detailsLabel;

		public RadGroupByProtocolCreator(IEngine engine, List<string> existingGroupNames)
		{
			_engine = engine;
			_parametersCache = new ParametersCache(engine);
			_existingGroupNames = existingGroupNames;

			string groupPrefixTooltip = "The prefix for the group names. The resulting group name will be the prefix followed by the element name between brackets.";
			_groupPrefixLabel = new Label("Group name prefix");
			_groupPrefixTextBox = new TextBox()
			{
				MinWidth = 600,
				Tooltip = groupPrefixTooltip,
			};
			_groupPrefixTextBox.Changed += (sender, args) => OnGroupPrefixTextBoxChanged();
			_groupPrefixTextBox.ValidationText = "Provide a valid prefix";

			_parameterSelector = new MultiParameterPerProtocolSelector(engine)
			{
				IsVisible = false,
			};
			_parameterSelector.Changed += (sender, args) => OnParameterSelectorChanged();

			_optionsEditor = new RadGroupOptionsEditor(_parameterSelector.ColumnCount);

			_detailsLabel = new Label()
			{
				MaxWidth = 900,
			};

			OnGroupPrefixTextBoxChanged();

			int row = 0;
			AddWidget(_groupPrefixLabel, row, 0);
			AddWidget(_groupPrefixTextBox, row, 1, 1, _parameterSelector.ColumnCount - 1);
			++row;

			AddSection(_parameterSelector, row, 0);
			row += _parameterSelector.RowCount;

			AddSection(_optionsEditor, row, 0);
			row += _optionsEditor.RowCount;

			AddWidget(_detailsLabel, row, 0, 1, _parameterSelector.ColumnCount, HorizontalAlignment.Stretch);
		}

		public event EventHandler<EventArgs> ValidationChanged;

		public bool IsValid { get; private set; }

		public string ValidationText { get; private set; }

		/// <inheritdoc />
		public override bool IsVisible
		{
			// Note: we had to override this, since otherwise it will make the details label always visible
			get => IsSectionVisible;
			set
			{
				if (IsSectionVisible == value)
					return;

				IsSectionVisible = value;

				_groupPrefixLabel.IsVisible = value;
				_groupPrefixTextBox.IsVisible = value;
				_parameterSelector.IsVisible = value;
				_optionsEditor.IsVisible = value;
				UpdateDetailsLabelVisibility();
			}
		}

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
					_optionsEditor.Options.UpdateModel,
					_optionsEditor.Options.AnomalyThreshold,
					_optionsEditor.Options.MinimalDuration));
			}

			return groups;
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

			var hasValidGroup = groups.Any(g => g.Valid);
			IsValid = hasValidGroup;

			if (!IsValid)
				ValidationText = "Make sure at least one valid group will be added";
			else
				ValidationText = string.Empty;

			ValidationChanged?.Invoke(this, EventArgs.Empty);
		}

		private List<ParameterKey> GetSelectedParametersForElement(Element element)
		{
			if (!_parametersCache.TryGet(element.DmaId, element.ElementId, out var parametersOnElement))
			{
				_engine.Log($"Could not find parameters for element {element.ElementName} ({element.DmaId}/{element.ElementId})");
				return new List<ParameterKey>();
			}

			var pKeys = new List<ParameterKey>();
			foreach (var parameter in _parameterSelector.GetSelectedParameters())
			{
				var paramInfo = parametersOnElement.FirstOrDefault(p => p.ID == parameter.ParameterID);
				if (paramInfo == null)
				{
					_engine.Log($"Could not find parameter {parameter.ParameterID} on element {element.ElementName} ({element.DmaId}/{element.ElementId})");
					continue;
				}

				if (!paramInfo.HasTrending())
					continue;

				if (parameter.ParentTableID == null)
				{
					pKeys.Add(new ParameterKey(element.DmaId, element.ElementId, parameter.ParameterID));
				}
				else
				{
					var matchingInstances = RadWidgets.Utils.FetchMatchingInstancesWithTrending(_engine, element.DmaId, element.ElementId, paramInfo, parameter.DisplayKeyFilter);
					pKeys.AddRange(matchingInstances.Select(i => new ParameterKey(element.DmaId, element.ElementId, parameter.ParameterID, i.IndexValue)));
				}
			}

			return pKeys;
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
				var pKeys = GetSelectedParametersForElement(element);
				var groupName = $"{_groupPrefixTextBox.Text} ({element.ElementName})";
				groups.Add(new GroupByProtocolInfo()
				{
					ElementName = element.ElementName,
					GroupName = groupName,
					ParameterKeys = pKeys,
					ValidGroupName = !_existingGroupNames.Contains(groupName),
				});
			}

			return groups;
		}

		private void UpdateDetailsLabelVisibility()
		{
			_detailsLabel.IsVisible = IsSectionVisible && _groupPrefixTextBox.ValidationState == UIValidationState.Valid;
		}

		private void UpdateDetailsLabel(List<GroupByProtocolInfo> groups)
		{
			UpdateDetailsLabelVisibility();
			if (!_detailsLabel.IsVisible)
				return;

			if (groups.Count == 0)
			{
				_detailsLabel.Text = "No elements found on the selected protocol";
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
				lines.Add($"Too few instances have been selected, or instances are not trended for {groupsWithTooFewInstances.Select(s => $"'{s.ElementName}'").HumanReadableJoin()}");
			}

			if (groupsWithTooManyInstances.Count > 0)
			{
				lines.Add($"Too many instances have been selected for {groupsWithTooManyInstances.Select(s => $"'{s.ElementName}'").HumanReadableJoin()}");
			}

			_detailsLabel.Text = string.Join("\n", lines);
		}

		private void OnParameterSelectorChanged()
		{
			UpdateIsValid();
		}

		private void OnGroupPrefixTextBoxChanged()
		{
			UIValidationState newState = string.IsNullOrEmpty(_groupPrefixTextBox.Text) ? UIValidationState.Invalid : UIValidationState.Valid;
			if (newState != _groupPrefixTextBox.ValidationState)
				_groupPrefixTextBox.ValidationState = newState;

			UpdateDetailsLabelVisibility();
			UpdateIsValid();
		}
	}
}
