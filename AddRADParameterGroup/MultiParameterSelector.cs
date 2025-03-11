using RADWidgets;
using Skyline.DataMiner.Automation;
using System.Collections.Generic;
using System.Linq;

namespace AddParameterGroup
{
    public class MultiParameterSelector : MultiSelector<ParameterSelectorInfo>
	{
        public MultiParameterSelector(IEngine engine) : base(new ParameterSelector(engine)) { }
    }
}
