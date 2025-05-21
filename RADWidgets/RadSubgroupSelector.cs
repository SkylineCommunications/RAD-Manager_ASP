namespace RadWidgets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadUtils;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.AutomationUI.Objects;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class RadSubgroupSelectorParameter
	{
		public ParameterKey Key { get; set; }

		public string ElementName { get; set; }

		public string ParameterName { get; set; }

		public RadParameter ToRadParameter(string label = null)
		{
			return new RadParameter() { Key = Key, Label = label };
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

	public class RadSubgroupSelectorItem
	{
		public RadSubgroupSelectorItem(Guid id, string name, RadSubgroupOptions options, List<RadSubgroupSelectorParameter> parameters,
			string displayValue)
		{
			ID = id;
			Name = name;
			Options = options;
			Parameters = parameters;
			DisplayValue = displayValue;
		}

		public Guid ID { get; private set; }

		public string Name { get; private set; }

		public string DisplayValue { get; private set; }

		public RadSubgroupOptions Options { get; private set; }

		public List<RadSubgroupSelectorParameter> Parameters { get; set; }

		public RadSubgroupSettings ToRadSubgroupSettings(List<string> labels)
		{
			var parameters = new List<RadParameter>(labels.Count);
			for (int i = 0; i < labels.Count && i < Parameters.Count; i++)
				parameters.Add(Parameters[i].ToRadParameter(labels[i]));

			return new RadSubgroupSettings
			{
				ID = ID,
				Name = Name,
				Parameters = parameters,
				Options = Options,
			};
		}

		public bool HasSameParameters(List<ParameterKey> parameters)
		{
			if (Parameters == null || parameters == null)
				return parameters == null && Parameters == null;

			return parameters.SequenceEqual(Parameters.Select(p => p.Key), new ParameterKeyEqualityComparer());
		}
	}

	public class RadSubgroupSelector : VisibilitySection
	{
		//TODO: try this with a RadioButtonList instead of a TreeView
		private readonly IEngine _engine;
		private readonly TreeView _selectorTreeView;
		private readonly Label _noSubgroupsLabel;
		private readonly Label _groupNameLabel;
		private readonly Label _invalidSelectionLabel;
		private readonly Label _detailsLabel;
		private readonly Button _editButton;
		private readonly Button _removeButton;
		private readonly Button _addButton;
		private List<string> _parameterLabels;
		private RadGroupOptions _parentOptions;
		private Dictionary<Guid, RadSubgroupSelectorItem> _subgroups;
		private int _unnamedSubgroupCount = 0;

		public RadSubgroupSelector(IEngine engine, RadGroupOptions parentOptions, List<string> parameterLabels, List<RadSubgroupSettings> subgroups = null)
		{
			_engine = engine;
			_parentOptions = parentOptions;
			_parameterLabels = parameterLabels;

			_noSubgroupsLabel = new Label("Add a subgroup by selecting 'Add subgroup...' below");

			_selectorTreeView = new TreeView(new List<TreeViewItem>())
			{
				Tooltip = "The subgroups of the current group. Click on a subgroup to view its parameters and options.",
				IsReadOnly = false,
			};
			_selectorTreeView.Changed += (sender, args) => OnSelectorTreeViewChanged();

			_groupNameLabel = new Label()
			{
				Tooltip = "The name of the selected subgroup.",
				Style = TextStyle.Heading,
			};

			_invalidSelectionLabel = new Label()
			{
				Tooltip = "Invalid selection.",
			};

			_detailsLabel = new Label()
			{
				Tooltip = "The parameters and options of the selected subgroup.",
			};

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
				MinHeight = 50,
			};

			_addButton = new Button("Add subgroup...")
			{
				Tooltip = "Add a new subgroup.",
			};
			_addButton.Pressed += (sender, args) => OnAddButtonPressed();

			SetSubgroups(subgroups);

			AddWidget(_noSubgroupsLabel, 0, 0, 1, 2);
			AddWidget(_selectorTreeView, 1, 0, 4, 1, verticalAlignment: VerticalAlignment.Top);
			AddWidget(_groupNameLabel, 1, 1, verticalAlignment: VerticalAlignment.Top);
			AddWidget(_invalidSelectionLabel, 2, 1, verticalAlignment: VerticalAlignment.Top);
			AddWidget(_detailsLabel, 3, 1, 2, 1, verticalAlignment: VerticalAlignment.Top);
			AddWidget(_editButton, 0, 2, 3, 1, verticalAlignment: VerticalAlignment.Top);
			AddWidget(_removeButton, 3, 2, verticalAlignment: VerticalAlignment.Top);
			AddWidget(whitespace, 4, 2);
			AddWidget(_addButton, 5, 2);
		}

		public List<RadSubgroupSettings> Subgroups
		{
			get => _subgroups.Values.Select(s => s.ToRadSubgroupSettings(_parameterLabels)).ToList();
		}

		/// <inheritdoc />
		public override bool IsVisible
		{
			// Note: we had to override this, since otherwise all child widgets are made visible when this is set to true.
			get => IsSectionVisible;
			set
			{
				if (value == IsSectionVisible)
					return;

				IsSectionVisible = value;

				_editButton.IsVisible = value;
				_removeButton.IsVisible = value;
				_addButton.IsVisible = value;
				UpdateTreeViewAndLabelVisibility();
			}
		}

		public void UpdateParameterLabels(List<string> parameterLabels)
		{
			_parameterLabels = parameterLabels;
			UpdateSelectedSubgroup();
		}

		public void UpdateParentOptions(RadGroupOptions parentOptions)
		{
			_parentOptions = parentOptions;
			UpdateSelectedSubgroup();
		}

		private void SetInvalidSelection(string detailsText, bool removeButtonEnabled)
		{
			_groupNameLabel.Text = string.Empty;
			_invalidSelectionLabel.Text = detailsText;
			_detailsLabel.Text = string.Empty;
			_editButton.IsEnabled = false;
			_removeButton.IsEnabled = removeButtonEnabled;
		}

		private void UpdateTreeViewAndLabelVisibility()
		{
			int count = _subgroups.Count();
			int selectedCount = _selectorTreeView.Items.Count(i => i.IsChecked);
			_noSubgroupsLabel.IsVisible = IsSectionVisible && count == 0;
			_selectorTreeView.IsVisible = IsSectionVisible && count > 0;
			_detailsLabel.IsVisible = IsSectionVisible && count > 0 && selectedCount == 1;
			_groupNameLabel.IsVisible = IsSectionVisible && count > 0 && selectedCount == 1;
			_invalidSelectionLabel.IsVisible = IsSectionVisible && count > 0 && selectedCount != 1;
		}

		private string GetSubgroupPlaceHolderName(int count)
		{
			return $"Unnamed subgroup {count}";
		}

		private RadSubgroupSelectorItem GetSelectedSubgroup()
		{
			var selectedItem = _selectorTreeView.Items.FirstOrDefault(i => i.IsChecked);
			return _subgroups.TryGetValue(Guid.Parse(selectedItem.KeyValue), out var subgroup) ? subgroup : null;
		}

		private void SetSubgroups(List<RadSubgroupSettings> subgroups)
		{
			if (subgroups == null)
			{
				_subgroups = new Dictionary<Guid, RadSubgroupSelectorItem>();
				UpdateSelectorTreeViewItems();
				return;
			}

			_subgroups = new Dictionary<Guid, RadSubgroupSelectorItem>(subgroups.Count);
			foreach (var subgroup in subgroups)
			{
				var parameters = new List<RadSubgroupSelectorParameter>(subgroup.Parameters.Count);
				foreach (var p in subgroup.Parameters)
				{
					var element = _engine.FindElement(p.Key.DataMinerID, p.Key.ElementID);
					var paramInfo = Utils.FetchParameterInfo(_engine, p.Key.DataMinerID, p.Key.ElementID, p.Key.ParameterID);
					parameters.Add(new RadSubgroupSelectorParameter
					{
						Key = p.Key,
						ElementName = element?.ElementName ?? "Unknown element",
						ParameterName = paramInfo?.DisplayName ?? "Unknown parameter",
					});
				}

				string displayValue = string.IsNullOrEmpty(subgroup.Name) ? GetSubgroupPlaceHolderName(_unnamedSubgroupCount++) : subgroup.Name;
				_subgroups[subgroup.ID] = new RadSubgroupSelectorItem(subgroup.ID, subgroup.Name, subgroup.Options, parameters, displayValue);
			}

			UpdateSelectorTreeViewItems();
		}

		private void UpdateSelectorTreeViewItems(Guid? selectedGroupId = null)
		{
			_selectorTreeView.Items = _subgroups.OrderBy(kvp => kvp.Value.DisplayValue, StringComparer.CurrentCultureIgnoreCase)
				.Select(kvp => new TreeViewItem(kvp.Value.DisplayValue, kvp.Key.ToString())
			{
				IsChecked = kvp.Value.ID == selectedGroupId,
			});
			UpdateSelectedSubgroup();
		}

		private void UpdateSelectedSubgroup()
		{
			UpdateTreeViewAndLabelVisibility();
			if (_subgroups.Count() == 0)
			{
				_editButton.IsEnabled = false;
				_removeButton.IsEnabled = false;
				return;
			}

			var selected = _selectorTreeView.Items.Where(i => i.IsChecked).ToList();
			if (selected.Count() == 0)
			{
				SetInvalidSelection("No subgroup selected.", false);
				return;
			}
			else if (selected.Count() >= 2)
			{
				SetInvalidSelection("Multiple subgroups selected.", true);
				return;
			}

			var settings = GetSelectedSubgroup();
			if (settings == null)
			{
				_detailsLabel.Text = "Invalid state";
				return;
			}

			_groupNameLabel.Text = settings.DisplayValue;
			_invalidSelectionLabel.Text = string.Empty;
			_editButton.IsEnabled = true;
			_removeButton.IsEnabled = true;

			List<string> parameterTexts = new List<string>(_parameterLabels.Count);
			for (int i = 0; i < _parameterLabels.Count; ++i)
			{
				string label = string.IsNullOrEmpty(_parameterLabels[i]) ? $"Parameter {i + 1}" : _parameterLabels[i];
				if (i < settings.Parameters.Count)
					parameterTexts.Add($"  {label}: {settings.Parameters[i].ToString()}");
				else
					parameterTexts.Add($"  {label}: Not set");//TOOD: I might want to mark groups with missing parameters in the tree view (add (parameters missing to the name or something))
			}

			var parameterText = string.Join("\n", parameterTexts);
			double anomalyThreshold = settings.Options.GetAnomalyThresholdOrDefault(_parentOptions.AnomalyThreshold);
			string anomalyThresholdText = settings.Options.AnomalyThreshold.HasValue ? anomalyThreshold.ToString() : $"{anomalyThreshold} (same as parent group)";
			int minimalDuration = settings.Options.GetMinimalDurationOrDefault(_parentOptions.MinimalDuration);
			string minimalDurationText = settings.Options.MinimalDuration.HasValue ? $"{minimalDuration} minutes" : $"{minimalDuration} minutes (same as parent group)";
			_detailsLabel.Text = $"Parameters:\n{parameterText}\n\n" +
				$"Options:\n" +
				$"  Anomaly threshold: {anomalyThresholdText}\n" +
				$"  Minimal anomaly duration: {minimalDurationText}";
		}

		private void OnEditButtonPressed()
		{
			var settings = GetSelectedSubgroup();
			if (settings == null)
				return;
			var placeHolderName = string.IsNullOrEmpty(settings.Name) ? settings.DisplayValue : GetSubgroupPlaceHolderName(_unnamedSubgroupCount + 1);

			InteractiveController app = new InteractiveController(_engine);
			EditSubgroupDialog dialog = new EditSubgroupDialog(_engine, _subgroups.Values.ToList(), _parameterLabels, settings, placeHolderName, _parentOptions);
			dialog.Accepted += (sender, args) =>
			{
				var d = sender as EditSubgroupDialog;
				if (d == null)
					return;

				app.Stop();

				var newSettings = d.Settings;
				_subgroups[settings.ID] = newSettings;
				if (string.IsNullOrEmpty(newSettings.Name) && !string.IsNullOrEmpty(settings.Name))
					_unnamedSubgroupCount++;

				UpdateSelectorTreeViewItems(settings.ID);
			};
			dialog.Cancelled += (sender, args) => app.Stop();

			app.ShowDialog(dialog);
		}

		private void OnRemoveButtonPressed()
		{
			foreach (var item in _selectorTreeView.Items.Where(i => i.IsChecked))
				_subgroups.Remove(Guid.Parse(item.KeyValue));

			UpdateSelectorTreeViewItems();
		}

		private void OnAddButtonPressed()
		{
			var placeHolderName = GetSubgroupPlaceHolderName(_unnamedSubgroupCount + 1);
			InteractiveController app = new InteractiveController(_engine);
			AddSubgroupDialog dialog = new AddSubgroupDialog(_engine, _subgroups.Values.ToList(), _parameterLabels, _parentOptions, placeHolderName);
			dialog.Accepted += (sender, args) =>
			{
				var d = sender as AddSubgroupDialog;
				if (d == null)
					return;

				app.Stop();

				var newSettings = d.Settings;
				_subgroups[newSettings.ID] = newSettings;
				if (string.IsNullOrEmpty(newSettings.Name))
					_unnamedSubgroupCount++;

				UpdateSelectorTreeViewItems(newSettings.ID);
			};
			dialog.Cancelled += (sender, args) => app.Stop();

			app.ShowDialog(dialog);
		}

		private void OnSelectorTreeViewChanged()
		{
			UpdateSelectedSubgroup();
		}
	}
}
