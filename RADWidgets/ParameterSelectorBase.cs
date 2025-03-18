using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Net.Messages;
using Skyline.DataMiner.Utils.InteractiveAutomationScript;
using System.Collections.Generic;
using System.Linq;

namespace RADWidgets
{
	public abstract class ParameterSelectorBase<T> : MultiSelectorItemSelector<T> where T : MultiSelectorItem
	{
		protected IEngine engine_;
		protected DropDown<ParameterInfo> parametersDropDown_;
		protected TextBox instanceTextBox_;

		protected void OnSelectedParameterChanged()
		{
			var parameter = parametersDropDown_.Selected;
			parametersDropDown_.Tooltip = parameter?.DisplayName ?? "";
			if (parameter?.IsTableColumn != true)
			{
				instanceTextBox_.IsEnabled = false;
				instanceTextBox_.Text = "";
			}
			else
			{
				instanceTextBox_.IsEnabled = true;
			}
		}

		protected virtual bool IsValidForRAD(ParameterInfo info)
		{
			//TODO: would be better to put this in SLNetTypes at some point
			if (info == null)
				return false;
			if ((info.ID >= 64300 && info.ID < 70000) || (info.ID >= 100000 && info.ID < 1000000))
				return false;
			if (info.WriteType || info.IsDuplicate)
				return false;
			if(!info.IsTrendAnalyticsSupported)
				return false;
			return !info.WriteType && !info.IsDuplicate;
		}

		protected void SetPossibleParameters(GetProtocolInfoResponseMessage protocol)
		{
			if (protocol == null)
			{
				engine_.Log("Got invalid protocol", LogType.Error, 5);
				ClearPossibleParameters();
				return;
			}

			parametersDropDown_.Options = protocol.Parameters.Where(p => IsValidForRAD(p)).OrderBy(p => p.DisplayName).Select(p => new Option<ParameterInfo>(p.DisplayName, p));
			OnSelectedParameterChanged();
		}

		protected void ClearPossibleParameters()
		{
			parametersDropDown_.Options = new List<Option<ParameterInfo>>();
			OnSelectedParameterChanged();
		}

		protected ParameterSelectorBase(IEngine engine, bool leaveFirstColEmpty)
		{
			engine_ = engine;

			var parametersLabel = new Label("Parameter");
			parametersDropDown_ = new DropDown<ParameterInfo>()
			{
				IsDisplayFilterShown = true,
				IsSorted = true,
			};
			parametersDropDown_.Changed += (sender, args) => OnSelectedParameterChanged();

			var instanceLabel = new Label("Display key filter");
			instanceTextBox_ = new TextBox();

			int parametersCol = leaveFirstColEmpty ? 1 : 0;
			int parametersColSpan = leaveFirstColEmpty ? 1 : 2;
			AddWidget(parametersLabel, 0, parametersCol, 1, parametersColSpan);
			AddWidget(parametersDropDown_, 1, parametersCol, 1, parametersColSpan);
			AddWidget(instanceLabel, 0, 2);
			AddWidget(instanceTextBox_, 1, 2);
		}
	}
}
