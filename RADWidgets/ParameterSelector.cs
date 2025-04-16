namespace RadWidgets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadUtils;
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
		public List<DynamicTableIndex> MatchingInstances { get; set; }

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
				if (MatchingInstances.Count != 1 || !string.Equals(MatchingInstances[0].DisplayValue, DisplayKeyFilter, StringComparison.OrdinalIgnoreCase))
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
				return MatchingInstances.Select(i => new ParameterKey(DataMinerID, ElementID, ParameterID, i.IndexValue)).ToList();
			else
				return new List<ParameterKey> { new ParameterKey(DataMinerID, ElementID, ParameterID) };
		}
	}

	public class ParameterSelector : ParameterSelectorBase<ParameterSelectorInfo>
	{
		private readonly DropDown<LiteElementInfoEvent> _elementsDropDown;

		public ParameterSelector(IEngine engine) : base(engine, true)
		{
			var elementsLabel = new Label("Element");
			var elements = Utils.FetchElements(engine).Where(e => !e.IsDynamicElement).OrderBy(e => e.Name).ToList();
			_elementsDropDown = new DropDown<LiteElementInfoEvent>()
			{
				Options = elements.Select(e => new Option<LiteElementInfoEvent>(e.Name, e)),
				IsDisplayFilterShown = true,
				IsSorted = true,
				MinWidth = 300,
			};
			_elementsDropDown.Changed += (sender, args) => OnSelectedElementChanged();
			OnSelectedElementChanged();

			AddWidget(elementsLabel, 0, 0);
			AddWidget(_elementsDropDown, 1, 0);
		}

		public override ParameterSelectorInfo SelectedItem
		{
			get
			{
				var element = _elementsDropDown.Selected;
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

				var matchingInstances = new List<DynamicTableIndex>();
				if (parameter.IsTableColumn && parameter.ParentTable != null)
				{
					matchingInstances = Utils.FetchMatchingInstancesWithTrending(Engine, element.DataMinerID, element.ElementID, parameter, InstanceTextBox.Text).ToList();
					if (matchingInstances.Count == 0)
					{
						ValidationState = UIValidationState.Invalid;
						ValidationText = "No matching instances found";
						return null;
					}
				}

				return new ParameterSelectorInfo
				{
					ElementName = element.Name,
					ParameterName = parameter.DisplayName,
					DataMinerID = element.DataMinerID,
					ElementID = element.ElementID,
					ParameterID = parameter.ID,
					DisplayKeyFilter = parameter.IsTableColumn ? InstanceTextBox.Text : string.Empty,
					MatchingInstances = matchingInstances,
					IsTableColumn = parameter.IsTableColumn,
				};
			}
		}

		protected override bool IsValidForRAD(ParameterInfo info)
		{
			return base.IsValidForRAD(info) && info.HasTrending();
		}

		private void OnSelectedElementChanged()
		{
			var element = _elementsDropDown.Selected;
			_elementsDropDown.Tooltip = element?.Name ?? string.Empty;
			if (element == null)
			{
				ClearPossibleParameters();
				return;
			}

			var protocol = Utils.FetchElementProtocol(Engine, element.DataMinerID, element.ElementID);
			SetPossibleParameters(protocol);
		}
	}
}
