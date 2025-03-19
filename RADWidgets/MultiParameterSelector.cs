namespace RADWidgets
{
	using System.Collections.Generic;
	using Skyline.DataMiner.Automation;

	public class MultiParameterSelector : MultiSelector<ParameterSelectorInfo>
	{
        public MultiParameterSelector(IEngine engine, List<ParameterSelectorInfo> parameters = null) : base(new ParameterSelector(engine), parameters)
		{
		}
    }
}
