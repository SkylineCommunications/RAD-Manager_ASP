namespace AddRADParameterGroup
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using AddParameterGroup;
	using RADWidgets;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Analytics.Mad;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class RADGroupByProtocolCreator : Section
	{
		private readonly IEngine engine_;
		private readonly TextBox groupPrefixTextBox_;
		private readonly MultiParameterPerProtocolSelector parameterSelector_;
		private readonly RADGroupOptionsEditor optionsEditor_;
		private readonly Label detailsLabel_;
		private Dictionary<string, List<ParameterKey>> selectedInstancesPerElement_ = new Dictionary<string, List<ParameterKey>>();
		private bool parameterSelectorValid_ = false;
		private bool elementsOnProtocol_ = false;

		public RADGroupByProtocolCreator(IEngine engine)
		{
			engine_ = engine;
			var groupPrefixLabel = new Label("Group name prefix");

			groupPrefixTextBox_ = new TextBox()
			{
				MinWidth = 600,
			};
			groupPrefixTextBox_.Changed += (sender, args) => OnGroupPrefixTextBoxChanged();
			groupPrefixTextBox_.ValidationText = "Provide a prefix";

			parameterSelector_ = new MultiParameterPerProtocolSelector(engine)
			{
				IsVisible = false,
			};
			parameterSelector_.Changed += (sender, args) => OnParameterSelectorChanged();

			optionsEditor_ = new RADGroupOptionsEditor(parameterSelector_.ColumnCount);

			detailsLabel_ = new Label()
			{
				MaxWidth = 900,
			};

			OnGroupPrefixTextBoxChanged();
			OnParameterSelectorChanged();

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
			var groups = new List<MADGroupInfo>(selectedInstancesPerElement_.Count);
			foreach (var p in selectedInstancesPerElement_)
			{
				groups.Add(new MADGroupInfo(
					$"{groupPrefixTextBox_.Text} ({p.Key})",
					p.Value,
					optionsEditor_.Options.UpdateModel,
					optionsEditor_.Options.AnomalyThreshold,
					optionsEditor_.Options.MinimalDuration));
			}

			return groups;
		}

		private void UpdateIsValid()
		{
			IsValid = groupPrefixTextBox_.ValidationState == UIValidationState.Valid && parameterSelectorValid_ && elementsOnProtocol_;
			detailsLabel_.IsVisible = IsValid;

			var texts = new List<string>(2);
			if (groupPrefixTextBox_.ValidationState == UIValidationState.Invalid)
				texts.Add("provide a group name prefix");
			if (!elementsOnProtocol_)
				texts.Add("select a protocol with at least one element");
			else if (!parameterSelectorValid_)
				texts.Add("select at least two instances");

			// Capitalize the first letter of the first text
			if (texts.Count > 0)
				texts[0] = string.Concat(texts[0][0].ToString().ToUpper(), texts[0].Substring(1));

			ValidationText = Utils.HumanReadableJoin(texts);
			ValidationChanged?.Invoke(this, EventArgs.Empty);
		}

		private void UpdateSelectedInstancesPerElement()
		{
			var elements = engine_.FindElementsByProtocol(parameterSelector_.ProtocolName, parameterSelector_.ProtocolVersion);
			if (elements == null || elements.Length == 0)
			{
				selectedInstancesPerElement_ = new Dictionary<string, List<ParameterKey>>();
				return;
			}

			selectedInstancesPerElement_ = new Dictionary<string, List<ParameterKey>>();
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

				selectedInstancesPerElement_[element.ElementName] = pKeys;
			}
		}

		private void UpdateDetailsLabel()
		{
			detailsLabel_.IsVisible = IsValid;
			if (!IsValid)
				return;

			if (selectedInstancesPerElement_.Count == 0)
			{
				detailsLabel_.Text = "No elements found on the selected protocol";
				return;
			}

			var elementsWithGroups = selectedInstancesPerElement_.Where(p => p.Value.Count >= 2).ToList();
			var elementsWithTooFewInstances = selectedInstancesPerElement_.Where(p => p.Value.Count < 2).ToList();

			List<string> lines = new List<string>();
			if (elementsWithGroups.Count > 0)
			{
				lines.Add("The following groups will be created:");
				lines.AddRange(elementsWithGroups.OrderBy(k => k.Key).Select(p => $"\t'{groupPrefixTextBox_.Text} ({p.Key})' with {p.Value.Count} instances").Take(5));
				if (elementsWithGroups.Count > 5)
					lines.Add($"\t... and {elementsWithGroups.Count - 5} more");
			}

			if (elementsWithTooFewInstances.Count > 0)
			{
				lines.Add($"Too few instances have been selected for {Utils.HumanReadableJoin(elementsWithTooFewInstances.Select(s => $"'{s.Key}'"))}");
			}

			detailsLabel_.Text = string.Join("\n", lines);
		}

		private void OnParameterSelectorChanged()
		{
			UpdateSelectedInstancesPerElement();

			bool newElementsOnProtocol = selectedInstancesPerElement_.Count > 0;
			bool newParameterSelectorValid = selectedInstancesPerElement_.Values.Any(v => v.Count >= 2);
			if (newParameterSelectorValid != parameterSelectorValid_ || newElementsOnProtocol != elementsOnProtocol_)
			{
				parameterSelectorValid_ = newParameterSelectorValid;
				elementsOnProtocol_ = newElementsOnProtocol;
				UpdateIsValid();
			}

			UpdateDetailsLabel();
		}

		private void OnGroupPrefixTextBoxChanged()
		{
			UIValidationState newState = string.IsNullOrEmpty(groupPrefixTextBox_.Text) ? UIValidationState.Invalid : UIValidationState.Valid;
			if (newState != groupPrefixTextBox_.ValidationState)
			{
				groupPrefixTextBox_.ValidationState = newState;
				UpdateIsValid();
				UpdateDetailsLabel();
			}
		}
	}
}
