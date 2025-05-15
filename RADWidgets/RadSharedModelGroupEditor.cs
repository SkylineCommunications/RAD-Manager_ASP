namespace RadWidgets
{
	using System.Collections.Generic;
	using RadUtils;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	//TODO: probably prevent adding groups with same parameters in other subgroups
	public class RadSharedModelGroupEditor : VisibilitySection
	{
		private readonly GroupNameSection _groupNameSection;
		private readonly RadGroupOptionsEditor _optionsEditor;
		private readonly ParameterLabelEditor _parameterLabelEditor;
		private readonly List<RadSubgroupEditor> _subgroupEditors;
		private readonly Label _detailsLabel;
		private readonly List<string> _existingGroupNames;

		public RadSharedModelGroupEditor(IEngine engine, List<string> existingGroupNames, RadSharedModelGroupSettings settings = null)
		{
			_groupNameSection = new GroupNameSection(settings?.GroupName, existingGroupNames, 1);
			_groupNameSection.ValidationChanged += (sender, args) => OnGroupNameSectionValidationChanged();

			OnGroupNameSectionValidationChanged();

			// Add the "Number of parameters per subgroup" label and numeric input
			_parameterLabelEditor = new ParameterLabelEditor(RadGroupEditor.MIN_PARAMETERS, RadGroupEditor.MAX_PARAMETERS, 1);



			_optionsEditor = new RadGroupOptionsEditor(2, settings?.Options);

			_detailsLabel = new Label();

			int row = 0;
			AddSection(_groupNameSection, row, 0);
			row += _groupNameSection.RowCount;

			AddSection(_parameterLabelEditor, row, 0);
			row += _parameterLabelEditor.RowCount;

			AddSection(_optionsEditor, row, 0);
			row += _optionsEditor.RowCount;

			AddWidget(_detailsLabel, row, 0, 1, 2);
		}

		public void OnGroupNameSectionValidationChanged()
		{
			//TODO
		}
	}
}
