namespace RadWidgets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadUtils;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Analytics.Rad;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.AutomationUI.Objects;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class RadSubgroupSelector : VisibilitySection
	{
		//TODO: try this with a RadioButtonList instead of a TreeView
		private readonly IEngine _engine;
		private readonly TreeView _selectorTreeView;
		private readonly Label _noSubgroupsLabel;
		private readonly Label _detailsLabel;
		private readonly Button _editButton;
		private readonly Button _removeButton;
		private readonly Button _addButton;
		private readonly Dictionary<Guid, RadSubgroupSettings> _subgroups;
		private List<string> _parameterLabels;
		private RadGroupOptions _parentOptions;

		public RadSubgroupSelector(IEngine engine, RadGroupOptions parentOptions, List<string> parameterLabels, List<RadSubgroupSettings> subgroups = null)
		{
			_engine = engine;
			_subgroups = subgroups?.ToDictionary(s => s.ID, s => s) ?? new Dictionary<Guid, RadSubgroupSettings>();
			_parentOptions = parentOptions;
			_parameterLabels = parameterLabels;

			_noSubgroupsLabel = new Label("Add a subgroup by selecting 'Add subgroup...' below")
			{
				MinHeight = 100,
			};

			_selectorTreeView = new TreeView(new List<TreeViewItem>())
			{
				Tooltip = "The subgroups of the current group. Click on a subgroup to view its parameters and options.",
				MinHeight = 100,
				IsReadOnly = false,
			};
			_selectorTreeView.Changed += (sender, args) => OnSelectorTreeViewChanged();

			_detailsLabel = new Label()
			{
				Tooltip = "The parameters and options of the selected subgroup.",
				MinHeight = 100,
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

			_addButton = new Button("Add subgroup...")
			{
				Tooltip = "Add a new subgroup.",
			};
			_addButton.Pressed += (sender, args) => OnAddButtonPressed();

			UpdateSelectorTreeViewItems();

			AddWidget(_noSubgroupsLabel, 0, 0, 1, 2);
			AddWidget(_selectorTreeView, 1, 0, 2, 1);
			AddWidget(_detailsLabel, 1, 1, 2, 1);
			AddWidget(_editButton, 0, 2, 2, 1);
			AddWidget(_removeButton, 2, 2, verticalAlignment: VerticalAlignment.Top);
			AddWidget(_addButton, 3, 2);
		}

		public List<RadSubgroupSettings> Subgroups
		{
			get => _subgroups.Values.ToList();
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
			foreach (var s in _subgroups.Values)
			{
				var newParameters = new List<RADParameter>();
				for (int i = 0; i < parameterLabels.Count; ++i)
				{
					ParameterKey key = null;
					if (i < s.Parameters.Count)
						key = s.Parameters[i].Key;
					//TODO: probably I want to remember the old key as well for the case they reduce and then increase the number again
					newParameters.Add(new RADParameter(key, parameterLabels[i]));
				}

				s.Parameters = newParameters;
			}

			UpdateSelectedSubgroup();
		}

		public void UpdateParentOptions(RadGroupOptions parentOptions)
		{
			_parentOptions = parentOptions;
		}

		private void SetInvalidSelection(string detailsText)
		{
			_detailsLabel.Text = detailsText;
			_editButton.IsEnabled = false;
			_removeButton.IsEnabled = false;
		}

		private void UpdateTreeViewAndLabelVisibility()
		{
			_noSubgroupsLabel.IsVisible = _subgroups.Count == 0;
			_selectorTreeView.IsVisible = _subgroups.Count > 0;
			_detailsLabel.IsVisible = _subgroups.Count > 0;
		}

		private RadSubgroupSettings GetSelectedSubgroup()
		{
			var selected = _selectorTreeView.Items.Where(i => i.IsChecked).ToList();
			if (selected.Count() > 0 && _subgroups.TryGetValue(Guid.Parse(selected.First().KeyValue), out var settings))
				return settings;

			return null;
		}

		private void UpdateSelectorTreeViewItems(Guid? selectedGroupId = null)
		{
			var items = new List<TreeViewItem>(_subgroups.Count);
			int unnamedSubgroupCount = 0;
			foreach (var kvp in _subgroups)
			{
				string displayValue = string.IsNullOrEmpty(kvp.Value.Name) ? $"Unnamed subgroup {++unnamedSubgroupCount}" : kvp.Value.Name;
				var item = new TreeViewItem(displayValue, kvp.Key.ToString())
				{
					IsChecked = selectedGroupId != null && kvp.Key == selectedGroupId,
				};
				items.Add(item);
			}

			_selectorTreeView.Items = items;
			UpdateSelectedSubgroup();
		}

		private void UpdateSelectedSubgroup()
		{
			UpdateTreeViewAndLabelVisibility();
			if (_subgroups.Count == 0)
			{
				_editButton.IsEnabled = false;
				_removeButton.IsEnabled = false;
				return;
			}

			var selected = _selectorTreeView.Items.Where(i => i.IsChecked).ToList();
			if (selected.Count() == 0)
			{
				SetInvalidSelection("No subgroup selected.");
				return;
			}
			else if (selected.Count() >= 2)
			{
				SetInvalidSelection("Multiple subgroups selected.");
				return;
			}

			var settings = GetSelectedSubgroup();
			if (settings == null)
			{
				_detailsLabel.Text = "Invalid state";
				return;
			}

			_editButton.IsEnabled = true;
			_removeButton.IsEnabled = true;

			List<string> parameterTexts = new List<string>(settings.Parameters.Count);
			for (int i = 0; i < settings.Parameters.Count; ++i)
			{
				var p = settings.Parameters[i];
				string label = string.IsNullOrEmpty(p.Label) ? $"Parameter {i + 1}" : p.Label;
				string parameter = p.Key?.ToString() ?? "Not set";
				//TODO: display the element and parameter name just as in the parameter selector
				parameterTexts.Add($"  {label}: {parameter}");
			}

			var parameterText = string.Join("\n", parameterTexts);

			string anomalyThresholdText = settings.Options.AnomalyThreshold.HasValue ? settings.Options.AnomalyThreshold.ToString() : "same as parent group";
			string minimalDurationText = settings.Options.MinimalDuration.HasValue ? $"{settings.Options.MinimalDuration.ToString()} minutes" : "same as parent group";
			var optionsText = $"  Anomaly threshold: {anomalyThresholdText}\n" +
				$"  Minimal anomaly duration: {minimalDurationText}";

			_detailsLabel.Text = $"Parameters:\n{parameterText}\n\n" +
				$"Options:\n{optionsText}";
		}

		private void OnEditButtonPressed()
		{
			var settings = GetSelectedSubgroup();
			if (settings == null)
				return;

			InteractiveController app = new InteractiveController(_engine);
			EditSubgroupDialog dialog = new EditSubgroupDialog(_engine, _subgroups.Values.Select(s => s.Name).ToList(), settings, _parentOptions);
			dialog.Accepted += (sender, args) =>
			{
				var d = sender as EditSubgroupDialog;
				if (d == null)
					return;

				app.Stop();

				if (d.Settings.Name == settings.Name)
				{
					_subgroups[settings.ID] = d.Settings;
				}
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
			InteractiveController app = new InteractiveController(_engine);
			AddSubgroupDialog dialog = new AddSubgroupDialog(_engine, _subgroups.Values.Select(s => s.Name).ToList(), _parameterLabels, _parentOptions);
			dialog.Accepted += (sender, args) =>
			{
				var d = sender as AddSubgroupDialog;
				if (d == null)
					return;

				app.Stop();

				_subgroups.Add(d.Settings.ID, d.Settings);
				UpdateSelectorTreeViewItems(d.Settings.ID);
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
