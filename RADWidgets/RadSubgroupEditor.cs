namespace RadWidgets
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime;
	using RadUtils;
	using Skyline.DataMiner.Analytics.Rad;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class RadSubgroupEditor : VisibilitySection
	{
		private GroupNameSection _groupNameSection;
		private List<ParameterSelector> _parameterSelectors;//TODO: probably a dropdown for the display index instead of a text box
		//TODO: validation for when the parameters are exactly the same as the parameters of anonther group (show a warning here, but do not block the user
		//from pressing OK
		private RadSubgroupOptionsEditor _optionsEditor;
		private Label _detailsLabel;
		//TODO: accept empty group names as well. Probably add a placeholder to the group name text box

		public RadSubgroupEditor(IEngine engine, List<string> existingSubgroupNames, RadSubgroupSettings settings, double? parentAnomalyThreshold,
			int? parentMinimalDuration)
		{
			int parameterCount = settings.Parameters.Count;
			var parameterSelectorLabels = new List<Label>(parameterCount);
			_parameterSelectors = new List<ParameterSelector>(parameterCount);
			for (int i = 0; i < parameterCount; i++)
			{
				var p = settings.Parameters[i];
				parameterSelectorLabels.Add(new Label(string.IsNullOrEmpty(p.Label) ? $"Parameter {i + 1}" : p.Label));
				_parameterSelectors.Add(new ParameterSelector(engine));//TODO: show the current parameter key
			}

			ConstuctWidgets(existingSubgroupNames, settings.Name, parameterSelectorLabels, _parameterSelectors, settings.Options,
				parentAnomalyThreshold, parentMinimalDuration);
		}

		public RadSubgroupEditor(IEngine engine, List<string> existingSubgroupNames, List<string> parameterLabels, double? parentAnomalyThreshold,
			int? parentMinimalDuration)
		{
			var parameterSelectorLabels = new List<Label>(parameterLabels.Count);
			_parameterSelectors = new List<ParameterSelector>(parameterLabels.Count);
			for (int i = 0; i < parameterLabels.Count; i++)
			{
				parameterSelectorLabels.Add(new Label(string.IsNullOrEmpty(parameterLabels[i]) ? $"Parameter {i + 1}" : parameterLabels[i]));
				_parameterSelectors.Add(new ParameterSelector(engine));//TODO: show the current parameter key
			}

			ConstuctWidgets(existingSubgroupNames, string.Empty, parameterSelectorLabels, _parameterSelectors, null,
				parentAnomalyThreshold, parentMinimalDuration);
		}

		public RadSubgroupSettings Settings
		{
			get
			{
				return new RadSubgroupSettings
				{
					Name = _groupNameSection.GroupName,
					Parameters = new List<RADParameter>(),//TODO: fill this in. Also probably include more info than just the key, but also the element name and stuff
					Options = _optionsEditor.Options,
				};
			}
		}

		private void ConstuctWidgets(List<string> existingSubgroupNames, string groupName,
			List<Label> parameterSelectorLabels, List<ParameterSelector> parameterSelectors,
			RadSubgroupOptions options, double? parentAnomalyThreshold, int? parentMinimalDuration)
		{
			_parameterSelectors = parameterSelectors;

			int parameterSelectorColumnCount = parameterSelectors.FirstOrDefault()?.ColumnCount ?? 1;
			_groupNameSection = new GroupNameSection(groupName, existingSubgroupNames, parameterSelectorColumnCount);

			_optionsEditor = new RadSubgroupOptionsEditor(parameterSelectorColumnCount + 1, parentAnomalyThreshold, parentMinimalDuration, options);

			_detailsLabel = new Label();

			int row = 0;
			AddSection(_groupNameSection, row, 0);
			row += _groupNameSection.RowCount;

			for (int i = 0; i < parameterSelectorLabels.Count(); i++)
			{
				AddWidget(parameterSelectorLabels[i], row, 0, parameterSelectors[i].RowCount, 1);
				AddSection(parameterSelectors[i], row, 1);
				row += parameterSelectors[i].RowCount;
			}

			AddSection(_optionsEditor, row, 0);
			row += _optionsEditor.RowCount;

			AddWidget(_detailsLabel, row, 0, 1, 2);
		}
	}
}
