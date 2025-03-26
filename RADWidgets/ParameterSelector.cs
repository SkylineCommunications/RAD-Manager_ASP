namespace RADWidgets
{
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Messages;

	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class ParameterSelectorInfo : MultiSelectorItem
	{
		public string ElementName { get; set; }

		public string ParameterName { get; set; }

		public int DataMinerID { get; set; }

		public int ElementID { get; set; }

		public int ParameterID { get; set; }

		public string DisplayKeyFilter { get; set; }

		public bool IsTableColumn { get; set; }

		/// <summary>
		/// Gets or sets a list of instance primary keys for which the display key matches the provided filter.
		/// </summary>
		public List<string> MatchingInstances { get; set; }

		public override string GetKey()
		{
			if (!string.IsNullOrEmpty(DisplayKeyFilter))
				return $"{DataMinerID}/{ElementID}/{ParameterID}/{DisplayKeyFilter}";
			else
				return $"{DataMinerID}/{ElementID}/{ParameterID}";
		}

		public override string GetDisplayValue()
		{
			if (IsTableColumn)
			{
				if (MatchingInstances.Count != 1)
					return $"{ElementName}/{ParameterName}/{DisplayKeyFilter} ({MatchingInstances.Count} matching instances)";
				else
					return $"{ElementName}/{ParameterName}/{DisplayKeyFilter}";
			}
			else
			{
				return $"{ElementName}/{ParameterName}";
			}
		}

		public IEnumerable<ParameterKey> GetParameterKeys()
		{
			if (MatchingInstances?.Count > 0)
				return MatchingInstances.Select(i => new ParameterKey(DataMinerID, ElementID, ParameterID, i)).ToList();
			else
				return new List<ParameterKey> { new ParameterKey(DataMinerID, ElementID, ParameterID) };
		}
	}

	public class ParameterSelector : ParameterSelectorBase<ParameterSelectorInfo>
	{
		private readonly DropDown<Element> elementsDropDown_;

		public ParameterSelector(IEngine engine) : base(engine, true)
		{
			var elementsLabel = new Label("Element");
			var elements = engine.FindElements(new ElementFilter()).OrderBy(e => e.ElementName).ToList();
			elementsDropDown_ = new DropDown<Element>()
			{
				Options = elements.Select(e => new Option<Element>(e.ElementName, e)),
				IsDisplayFilterShown = true,
				IsSorted = true,
				MinWidth = 300,
			};
			elementsDropDown_.Changed += (sender, args) => OnSelectedElementChanged();
			OnSelectedElementChanged();

			AddWidget(elementsLabel, 0, 0);
			AddWidget(elementsDropDown_, 1, 0);
		}

		public override ParameterSelectorInfo SelectedItem
		{
			get
			{
				var element = elementsDropDown_.Selected;
				if (element == null)
				{
					ValidationState = UIValidationState.Invalid;
					ValidationText = "Select a valid element";
					return null;
				}

				var parameter = ParametersDropDown.Selected;
				if (parameter == null)
				{
					ValidationState = UIValidationState.Invalid;
					ValidationText = "Select a valid parameter";
					return null;
				}

				var matchingInstances = new List<string>();
				if (parameter.IsTableColumn && parameter.ParentTable != null)
				{
					matchingInstances = Utils.FetchMatchingInstances(Engine, element.DmaId, element.ElementId, parameter, InstanceTextBox.Text).ToList();
					if (matchingInstances.Count == 0)
					{
						ValidationState = UIValidationState.Invalid;
						ValidationText = "No matching instances found";
						return null;
					}
				}

				return new ParameterSelectorInfo
				{
					ElementName = element.ElementName,
					ParameterName = parameter.DisplayName,
					DataMinerID = element.DmaId,
					ElementID = element.ElementId,
					ParameterID = parameter.ID,
					DisplayKeyFilter = parameter.IsTableColumn ? InstanceTextBox.Text : string.Empty,
					MatchingInstances = matchingInstances,
					IsTableColumn = parameter.IsTableColumn,
				};
			}
		}

		protected override bool IsValidForRAD(ParameterInfo info)
		{
			return base.IsValidForRAD(info) && (info.RealTimeTrending || info.AverageTrending);
		}

		private void OnSelectedElementChanged()
		{
			var element = elementsDropDown_.Selected;
			elementsDropDown_.Tooltip = element?.ElementName ?? string.Empty;
			if (element == null)
			{
				ClearPossibleParameters();
				return;
			}

			var protocol = Utils.FetchElementProtocol(Engine, element.DmaId, element.ElementId);
			SetPossibleParameters(protocol);
		}
	}
}
