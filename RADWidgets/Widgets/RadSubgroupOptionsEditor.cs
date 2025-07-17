namespace RadWidgets.Widgets
{
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;
	using Skyline.DataMiner.Utils.RadToolkit;

	public class RadSubgroupOptionsEditor : Section
    {
        private readonly RadGroupBaseOptionsEditor _baseOptionsEditor;

        public RadSubgroupOptionsEditor(RadHelper radHelper, int columnCount, RadGroupOptions parentOptions, RadSubgroupOptions options = null)
        {
            _baseOptionsEditor = new RadGroupBaseOptionsEditor(columnCount, 
				parentOptions?.GetAnomalyThresholdOrDefault(radHelper) ?? radHelper.DefaultAnomalyThreshold,
				parentOptions?.GetMinimalDurationOrDefault(radHelper) ?? radHelper.DefaultMinimumAnomalyDuration,
				options);

            AddSection(_baseOptionsEditor, 0, 0);
        }

        public RadSubgroupOptions Options
        {
            get
            {
                return new RadSubgroupOptions
                {
                    AnomalyThreshold = _baseOptionsEditor.AnomalyThreshold,
                    MinimalDuration = _baseOptionsEditor.MinimalDuration,
                };
            }
        }
    }
}
