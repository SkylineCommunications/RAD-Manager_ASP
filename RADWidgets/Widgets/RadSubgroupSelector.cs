namespace RadWidgets.Widgets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadUtils;
	using RadWidgets;
	using RadWidgets.Widgets.Dialogs;
	using RadWidgets.Widgets.Generic;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;
	using Skyline.DataMiner.Utils.RadToolkit;

	public class RadSubgroupSelectorParameter
	{
		public ParameterKey Key { get; set; }

		public string ElementName { get; set; }

		public string ParameterName { get; set; }

		public RadParameter ToRadParameter(string label = null)
		{
			return new RadParameter(Key, label);
		}

		public override string ToString()
		{
			if (!string.IsNullOrEmpty(Key?.DisplayInstance))
				return $"{ElementName}/{ParameterName}/{Key.DisplayInstance}";
			else if (!string.IsNullOrEmpty(Key?.Instance))
				return $"{ElementName}/{ParameterName}/{Key.Instance}";
			else
				return $"{ElementName}/{ParameterName}";
		}
	}

	public class RadSubgroupSelectorItem : SelectorItem
	{
		public RadSubgroupSelectorItem(Guid id, string name, RadSubgroupOptions options, List<RadSubgroupSelectorParameter> parameters,
			string displayName, List<string> parameterLabels)
		{
			ID = id;
			Name = name;
			Options = options;
			Parameters = parameters;
			DisplayName = displayName;
			UpdateHasMissingParameters(parameterLabels);
			HasDuplicatedParameters = Parameters != null && Parameters.Count >= 2 && Parameters.GroupBy(p => p?.Key, new ParameterKeyEqualityComparer()).Any(g => g.Count() > 1);
		}

		public Guid ID { get; private set; }

		public string Name { get; private set; }

		public string DisplayName { get; private set; }

		public bool HasMissingParameters { get; private set; }

		public bool HasDuplicatedParameters { get; private set; }

		public RadSubgroupOptions Options { get; private set; }

		public List<RadSubgroupSelectorParameter> Parameters { get; private set; }

		public RadSubgroupSettings ToRadSubgroupSettings(List<string> labels)
		{
			var parameters = new List<RadParameter>(labels.Count);
			for (int i = 0; i < labels.Count && i < Parameters.Count; i++)
				parameters.Add(Parameters[i].ToRadParameter(labels[i]));

			// Note that the name of the subgroup should be null instead of empty for SLAnalytics
			return new RadSubgroupSettings(string.IsNullOrEmpty(Name) ? null : Name, ID, parameters, Options);
		}

		public bool HasSameParameters(List<ParameterKey> parameters)
		{
			if (Parameters == null || parameters == null)
				return parameters == null && Parameters == null;

			var comparer = new ParameterKeyListEqualityComparer();
			return comparer.Equals(Parameters.Select(p => p?.Key).ToList(), parameters);
		}

		public void UpdateHasMissingParameters(List<string> parameterLabels)
		{
			if (parameterLabels == null)
				HasMissingParameters = false;
			else if (Parameters == null)
				HasMissingParameters = parameterLabels.Count > 0;
			else if (Parameters.Count < parameterLabels.Count)
				HasMissingParameters = true;
			else
				HasMissingParameters = Parameters.Take(Math.Min(parameterLabels.Count, Parameters.Count)).Any(p => p == null);
		}

		public override string GetKey()
		{
			return ID.ToString();
		}

		public override string GetDisplayValue()
		{
			if (HasMissingParameters)
				return $"{DisplayName} (missing parameters)";
			else if (HasDuplicatedParameters)
				return $"{DisplayName} (duplicated parameters)";
			else
				return DisplayName;
		}
	}

	public class RadSubgroupSelector : VisibilitySection
	{
		public const int MinNrOfSubgroups = 2;
		public const int MaxNrOfSubgroups = 2500;
		private readonly IEngine _engine;
		private readonly Label _noSubgroupsLabel;
		private readonly DetailsViewer<RadSubgroupSelectorItem> _subgroupViewer;
		private readonly RadSubgroupDetailsView _subgroupDetailsView;
		private readonly Button _editButton;
		private readonly Button _removeButton;
		private readonly ParametersCache _parametersCache;
		private List<string> _parameterLabels;
		private RadGroupOptions _parentOptions;
		private int _unnamedSubgroupCount = 0;
		private HashSet<string> _subgroupsWithMissingParameters;
		private HashSet<string> _subgroupsWithDuplicatedParameters;
		private List<string> _duplicatedSubgroupNames;
		private List<string> _subgroupsWithSameParameters;

		public RadSubgroupSelector(IEngine engine, RadGroupOptions parentOptions, List<string> parameterLabels, ParametersCache parametersCache,
			List<RadSubgroupInfo> subgroups = null, Guid? selectedSubgroup = null)
		{
			_engine = engine;
			_parameterLabels = parameterLabels;
			_parentOptions = parentOptions ?? throw new ArgumentNullException(nameof(parentOptions));
			_parametersCache = parametersCache;

			_noSubgroupsLabel = new Label("Add a subgroup by selecting 'Add subgroup...' below")
			{
				MinWidth = 600,
			};

			_subgroupDetailsView = new RadSubgroupDetailsView(parameterLabels, parentOptions);

			_subgroupViewer = new DetailsViewer<RadSubgroupSelectorItem>(_subgroupDetailsView);
			_subgroupViewer.SelectionChanged += (sender, args) => OnSubgroupViewerSelectionChanged(args.Selection);

			_editButton = new Button("Edit subgroup...")
			{
				Tooltip = "Edit the parameters and options of this subgroup.",
			};
			_editButton.Pressed += (sender, args) => OnEditButtonPressed();

			_removeButton = new Button("Remove subgroup")
			{
				Tooltip = "Remove this subgroup.",
			};
			_removeButton.Pressed += (sender, args) => OnRemoveButtonPressed();

			var whitespace = new WhiteSpace()
			{
				MinHeight = 100,
			};

			var addButton = new Button("Add subgroup...")
			{
				Tooltip = "Add a new subgroup.",
			};
			addButton.Pressed += (sender, args) => OnAddButtonPressed();

			SetSubgroups(subgroups, selectedSubgroup);

			AddWidget(_noSubgroupsLabel, 0, 0, 1, _subgroupViewer.ColumnCount, () => !GetSubgroupsViewerVisible());
			AddSection(_subgroupViewer, 1, 0, GetSubgroupsViewerVisible);
			AddWidget(_editButton, 0, 2, 3, 1, verticalAlignment: VerticalAlignment.Top);
			AddWidget(_removeButton, 3, 2, verticalAlignment: VerticalAlignment.Top);
			AddWidget(whitespace, 4, 2);
			AddWidget(addButton, 5, 2);
		}

		public event EventHandler ValidationChanged;

		public bool IsValid { get; private set; }

		public string ValidationText { get; private set; }

		public List<RadSubgroupSettings> GetSubgroups()
		{
			return _subgroupViewer.Items.Select(s => s.ToRadSubgroupSettings(_parameterLabels)).ToList();
		}

		public void UpdateParameterLabels(List<string> parameterLabels)
		{
			_parameterLabels = parameterLabels;
			_subgroupDetailsView.SetParameterLabels(parameterLabels);
			foreach (var item in _subgroupViewer.Items)
				item.UpdateHasMissingParameters(_parameterLabels);

			CalculateSubgroupsWithMissingParameters(_subgroupViewer.Items);
			CalculateSubgroupsWithDuplicatedParameters(_subgroupViewer.Items);
			CalculateSubgroupsWithSameParameters(_subgroupViewer.Items);
			UpdateIsValid(_subgroupViewer.Items);

			// We might need to append the missing parameters suffix to the display value, hence recalculate all items
			UpdateSelectorTreeViewItems(_subgroupViewer.Items, _subgroupViewer.GetSelected().Select(i => i.ID).ToArray());
		}

		public void UpdateParentOptions(RadGroupOptions parentOptions)
		{
			_parentOptions = parentOptions ?? throw new ArgumentNullException(nameof(parentOptions));
			_subgroupDetailsView.SetParentOptions(parentOptions);
		}

		private bool GetSubgroupsViewerVisible()
		{
			return GetSubgroupsViewerVisible(_subgroupViewer.Items);
		}

		private bool GetSubgroupsViewerVisible(List<RadSubgroupSelectorItem> subgroups)
		{
			return subgroups.Count > 0;
		}

		private void UpdateTreeViewAndLabelVisibility(List<RadSubgroupSelectorItem> subgroups)
		{
			bool subgroupsViewerVisible = GetSubgroupsViewerVisible(subgroups);
			_noSubgroupsLabel.IsVisible = IsSectionVisible && !subgroupsViewerVisible;
			_subgroupViewer.IsVisible = IsSectionVisible && subgroupsViewerVisible;
		}

		private string GetSubgroupPlaceHolderName(int count)
		{
			return $"Unnamed subgroup {count}";
		}

		private void SetSubgroups(List<RadSubgroupInfo> subgroups, Guid? selectedSubgroup)
		{
			List<RadSubgroupSelectorItem> items = new List<RadSubgroupSelectorItem>(subgroups?.Count ?? 0);
			if (subgroups == null)
			{
				UpdateSelectorTreeViewItems(items);
				CalculateAndUpdateIsValid(items);
				return;
			}

			foreach (var subgroup in subgroups)
			{
				var parameters = new List<RadSubgroupSelectorParameter>(subgroup.Parameters.Count);
				foreach (var p in subgroup.Parameters)
				{
					var element = _engine.FindElement(p.Key.DataMinerID, p.Key.ElementID);
					var paramInfo = RadWidgets.Utils.FetchParameterInfo(_engine, _parametersCache, p.Key.DataMinerID, p.Key.ElementID, p.Key.ParameterID);
					parameters.Add(new RadSubgroupSelectorParameter
					{
						Key = p.Key,
						ElementName = element?.ElementName ?? "Unknown element",
						ParameterName = paramInfo?.DisplayName ?? "Unknown parameter",
					});
				}

				string displayName = string.IsNullOrEmpty(subgroup.Name) ? GetSubgroupPlaceHolderName(++_unnamedSubgroupCount) : subgroup.Name;
				items.Add(new RadSubgroupSelectorItem(subgroup.ID, subgroup.Name, subgroup.Options, parameters, displayName, _parameterLabels));
			}

			if (selectedSubgroup.HasValue)
				UpdateSelectorTreeViewItems(items, selectedSubgroup.Value);
			else
				UpdateSelectorTreeViewItems(items);
			CalculateAndUpdateIsValid(items);
		}

		private void UpdateSelectorTreeViewItems(List<RadSubgroupSelectorItem> subgroups, params Guid[] selectedGroups)
		{
			UpdateTreeViewAndLabelVisibility(subgroups);
			if (subgroups.Count == 0)
			{
				_editButton.IsEnabled = false;
				_removeButton.IsEnabled = false;
				return;
			}

			_subgroupViewer.SetItems(subgroups.OrderBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase).ToList(), selectedGroups.Select(g => g.ToString()).ToArray());
		}

		private void CalculateSubgroupsWithMissingParameters(List<RadSubgroupSelectorItem> subgroups)
		{
			_subgroupsWithMissingParameters = subgroups.Where(s => s.HasMissingParameters).Select(s => s.DisplayName).ToHashSet();
		}

		private void CalculateSubgroupsWithDuplicatedParameters(List<RadSubgroupSelectorItem> subgroups)
		{
			_subgroupsWithDuplicatedParameters = subgroups.Where(s => s.HasDuplicatedParameters).Select(s => s.DisplayName).ToHashSet();
		}

		private void CalculateDuplicatedSubgroupNames(List<RadSubgroupSelectorItem> subgroups)
		{
			_duplicatedSubgroupNames = subgroups.Where(s => !string.IsNullOrEmpty(s.Name))
				.GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
				.Where(g => g.Count() > 1)
				.Select(g => g.Key)
				.ToList();
		}

		/// <summary>
		/// Check whether any two subgroups have exactly the same parameters. Stops as soon as it finds one collection of subgroups that have the same parameters.
		/// </summary>
		private void CalculateSubgroupsWithSameParameters(List<RadSubgroupSelectorItem> subgroups)
		{
			var parameterKeyLists = subgroups.Select(s => Tuple.Create(s.DisplayName, s.Parameters.Select(p => p?.Key).ToList()));
			var groupsWithSameParameters = parameterKeyLists.GroupBy(t => t.Item2, new ParameterKeyListEqualityComparer()).Where(g => g.Count() > 1);
			if (groupsWithSameParameters.Any())
				_subgroupsWithSameParameters = groupsWithSameParameters.First().Select(t => t.Item1).ToList();
			else
				_subgroupsWithSameParameters = new List<string>();
		}

		private void CalculateAndUpdateIsValidOnEditedSubgroup(List<RadSubgroupSelectorItem> subgroups, RadSubgroupSelectorItem newSettings, RadSubgroupSelectorItem oldSettings)
		{
			if (oldSettings.HasMissingParameters)
				_subgroupsWithMissingParameters.Remove(oldSettings.DisplayName);
			if (newSettings.HasMissingParameters)
				_subgroupsWithMissingParameters.Add(newSettings.DisplayName);

			if (oldSettings.HasDuplicatedParameters)
				_subgroupsWithDuplicatedParameters.Remove(oldSettings.DisplayName);
			if (newSettings.HasDuplicatedParameters)
				_subgroupsWithDuplicatedParameters.Add(newSettings.DisplayName);

			if (!string.Equals(newSettings.Name, oldSettings.Name))
				CalculateDuplicatedSubgroupNames(subgroups);

			if (!newSettings.HasSameParameters(oldSettings.Parameters?.Select(p => p?.Key).ToList()))
				CalculateSubgroupsWithSameParameters(subgroups);
		}

		private void CalculateAndUpdateIsValidOnAddedSubgroup(List<RadSubgroupSelectorItem> subgroups, RadSubgroupSelectorItem newSettings)
		{
			if (newSettings.HasMissingParameters)
				_subgroupsWithMissingParameters.Add(newSettings.DisplayName);

			if (newSettings.HasDuplicatedParameters)
				_subgroupsWithDuplicatedParameters.Add(newSettings.DisplayName);

			string newName = newSettings.Name;
			if (!string.IsNullOrEmpty(newName) && subgroups.Count(s => string.Equals(s.Name, newName, StringComparison.OrdinalIgnoreCase)) >= 2)
				_duplicatedSubgroupNames.Add(newName);

			CalculateSubgroupsWithSameParameters(subgroups);
		}

		private void UpdateValidationText(List<RadSubgroupSelectorItem> subgroups)
		{
			ValidationText = string.Empty;
			if (subgroups.Count < MinNrOfSubgroups || subgroups.Count > MaxNrOfSubgroups)
			{
				ValidationText = $"The number of subgroups must be between {MinNrOfSubgroups} and {MaxNrOfSubgroups}.";
				return;
			}

			if (_subgroupsWithMissingParameters.Count > 0)
			{
				if (_subgroupsWithMissingParameters.Count == 1)
					ValidationText = $"Subgroup {_subgroupsWithMissingParameters.First()} is missing parameters.";
				else
					ValidationText = $"Subgroups {_subgroupsWithMissingParameters.HumanReadableJoin()} are missing parameters.";
				return;
			}

			if (_subgroupsWithDuplicatedParameters.Count > 0)
			{
				if (_subgroupsWithDuplicatedParameters.Count == 1)
					ValidationText = $"Subgroup {_subgroupsWithDuplicatedParameters.First()} has duplicated parameters.";
				else
					ValidationText = $"Subgroups {_subgroupsWithDuplicatedParameters.HumanReadableJoin()} have duplicated parameters.";
				return;
			}

			if (_duplicatedSubgroupNames.Count > 0)
			{
				if (_duplicatedSubgroupNames.Count == 1)
					ValidationText = $"The name {_duplicatedSubgroupNames.First()} is used by multiple subgroups.";
				else
					ValidationText = $"The names {_duplicatedSubgroupNames.HumanReadableJoin()} are used by multiple subgroups.";
				return;
			}

			if (_subgroupsWithSameParameters.Count > 0)
				ValidationText = $"The parameters of the subgroups {_subgroupsWithSameParameters.HumanReadableJoin()} are exactly the same. Provide unique parameters for each subgroup.";
		}

		/// <summary>
		/// Update the IsValid state and ValidationText based on the already calculated state booleans.
		/// </summary>
		private void UpdateIsValid(List<RadSubgroupSelectorItem> subgroups)
		{
			UpdateValidationText(subgroups);
			IsValid = string.IsNullOrEmpty(ValidationText);
			ValidationChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Recaculate all the state booleans and update the IsValid state.
		/// </summary>
		private void CalculateAndUpdateIsValid(List<RadSubgroupSelectorItem> subgroups)
		{
			CalculateSubgroupsWithDuplicatedParameters(subgroups);
			CalculateSubgroupsWithMissingParameters(subgroups);
			CalculateDuplicatedSubgroupNames(subgroups);
			CalculateSubgroupsWithSameParameters(subgroups);

			UpdateIsValid(subgroups);
		}

		private void OnEditButtonPressed()
		{
			var settings = _subgroupViewer.GetSelected()?.FirstOrDefault();
			if (settings == null)
				return;
			var placeHolderName = string.IsNullOrEmpty(settings.Name) ? settings.DisplayName : GetSubgroupPlaceHolderName(_unnamedSubgroupCount + 1);

			InteractiveController app = new InteractiveController(_engine);
			EditSubgroupDialog dialog = new EditSubgroupDialog(_engine, _subgroupViewer.Items.ToList(), _parameterLabels, settings, placeHolderName, _parentOptions);
			dialog.Accepted += (sender, args) =>
			{
				var d = sender as EditSubgroupDialog;
				if (d == null)
					return;

				app.Stop();

				var newSettings = d.GetSettings();
				var newItems = _subgroupViewer.Items.Select(s => s.ID == settings.ID ? newSettings : s).ToList();
				if (string.IsNullOrEmpty(newSettings.Name) && !string.IsNullOrEmpty(settings.Name))
					_unnamedSubgroupCount++;

				UpdateSelectorTreeViewItems(newItems, settings.ID);
				CalculateAndUpdateIsValidOnEditedSubgroup(newItems, newSettings, settings);
				UpdateIsValid(newItems);
			};
			dialog.Cancelled += (sender, args) => app.Stop();

			app.ShowDialog(dialog);
		}

		private void OnRemoveButtonPressed()
		{
			var selectedKeys = _subgroupViewer.GetSelected().Select(i => i.ID).ToList();
			var newItems = _subgroupViewer.Items.Where(s => !selectedKeys.Contains(s.ID)).ToList();
			CalculateAndUpdateIsValid(newItems);
			UpdateSelectorTreeViewItems(newItems);
		}

		private void OnAddButtonPressed()
		{
			var placeHolderName = GetSubgroupPlaceHolderName(_unnamedSubgroupCount + 1);
			InteractiveController app = new InteractiveController(_engine);
			AddSubgroupDialog dialog = new AddSubgroupDialog(_engine, _subgroupViewer.Items.ToList(), _parameterLabels, _parentOptions, placeHolderName);
			dialog.Accepted += (sender, args) =>
			{
				var d = sender as AddSubgroupDialog;
				if (d == null)
					return;

				app.Stop();

				var newSettings = d.GetSettings();
				var newItems = _subgroupViewer.Items;
				newItems.Add(newSettings);
				if (string.IsNullOrEmpty(newSettings.Name))
					_unnamedSubgroupCount++;

				CalculateAndUpdateIsValidOnAddedSubgroup(newItems, newSettings);
				UpdateIsValid(newItems);
				UpdateSelectorTreeViewItems(newItems, newSettings.ID);
			};
			dialog.Cancelled += (sender, args) => app.Stop();

			app.ShowDialog(dialog);
		}

		private void OnSubgroupViewerSelectionChanged(List<RadSubgroupSelectorItem> selection)
		{
			_editButton.IsEnabled = selection?.Count == 1;
			_removeButton.IsEnabled = selection?.Count >= 1;
		}
	}
}
