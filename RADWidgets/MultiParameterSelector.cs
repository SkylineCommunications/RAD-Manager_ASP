using Skyline.DataMiner.Automation;

namespace RADWidgets
{
    public class MultiParameterSelector : MultiSelector<ParameterSelectorInfo>
	{
        public MultiParameterSelector(IEngine engine) : base(new ParameterSelector(engine)) { }
    }
}
