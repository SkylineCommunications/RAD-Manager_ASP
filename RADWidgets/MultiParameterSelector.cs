namespace RADWidgets
{
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Automation;

	public class MultiParameterSelector : MultiSelector<ParameterSelectorInfo>
	{
		public MultiParameterSelector(IEngine engine, IEnumerable<ParameterKey> parameters = null) : base(new ParameterSelector(engine))
		{
			if (parameters == null)
				return;

			var selection = new List<ParameterSelectorInfo>(parameters.Count());
			foreach (var parameter in parameters)
			{
				var element = engine.FindElement(parameter.DataMinerID, parameter.ElementID);
				var protocol = Utils.FetchElementProtocol(engine, parameter.DataMinerID, parameter.ElementID);
				var paramInfo = protocol?.Parameters.FirstOrDefault(p => p.ID == parameter.ParameterID);
				selection.Add(new ParameterSelectorInfo()
				{
					ElementName = element?.ElementName ?? "Unknown element",
					ParameterName = paramInfo?.DisplayName ?? "Unknown parameter",
					DataMinerID = parameter.DataMinerID,
					ElementID = parameter.ElementID,
					ParameterID = parameter.ParameterID,
					DisplayKeyFilter = parameter.DisplayInstance,
					MatchingInstances = new List<string>() { parameter.Instance },
				});
			}

			SelectedItems = selection;
		}

		public IEnumerable<ParameterKey> GetSelectedParameters()
		{
			return SelectedItems.SelectMany(i => i.GetParameterKeys()).Distinct();
		}
    }
}
