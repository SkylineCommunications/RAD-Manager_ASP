using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Net.Messages;
using Skyline.DataMiner.Utils.InteractiveAutomationScript;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AddParameterGroup
{
	public class MultiParameterPerProtocolSelector : Section
	{
		private IEngine engine_;
		private DropDown<GetProtocolsResponseMessage> protocolNameDropDown_;
		private DropDown<string> protocolVersionDropDown_;
		private MultiProtocolParameterSelector parameterSelector_;

		public string ProtocolName => protocolNameDropDown_.Selected?.Protocol;
		public string ProtocolVersion => protocolVersionDropDown_.Selected;
		public List<ProtocolParameterSelectorInfo> SelectedParameters => parameterSelector_.SelectedItems;

		public event EventHandler Changed;

		public void OnSelectedProtocolVersionChanged()
		{
			var protocol = protocolNameDropDown_.Selected;
			var version = protocolVersionDropDown_.Selected;
			if (protocol == null || string.IsNullOrEmpty(version))
				return;

			parameterSelector_.SetProtocol(protocol.Protocol, version);
		}

		public void OnSelectedProtocolChanged()
		{
			var protocol = protocolNameDropDown_.Selected;
			if (protocol == null)
				protocolVersionDropDown_.Options = new List<Option<string>>();
			else
				protocolVersionDropDown_.Options = protocol.Versions.OrderBy(v => v).Select(s => new Option<string>(s)).ToList();
			OnSelectedProtocolVersionChanged();
		}

		private List<GetProtocolsResponseMessage> FetchProtocols()
		{
			try
			{
				var request = new GetInfoMessage(InfoType.Protocols);
				var responses = engine_.SendSLNetMessage(request);
				return responses.Select(r => r as GetProtocolsResponseMessage).Where(r => r != null).OrderBy(p => p.Protocol).ToList();
			}
			catch (Exception e)
			{
				engine_.Log($"Failed to fetch protocols: {e}", LogType.Error, 5);
				return new List<GetProtocolsResponseMessage>();
			}
		}

		public MultiParameterPerProtocolSelector(IEngine engine) : base()
		{
			engine_ = engine;

			var protocolNameLabel = new Label("Connector");
			protocolNameDropDown_ = new DropDown<GetProtocolsResponseMessage>()
			{
				Options = FetchProtocols().Select(p => new Option<GetProtocolsResponseMessage>(p.Protocol, p)),
				IsDisplayFilterShown = true,
				IsSorted = true,
			};
			protocolNameDropDown_.Changed += (sender, args) => OnSelectedProtocolChanged();

			var protocolVersionLabel = new Label("Connector version");
			protocolVersionDropDown_ = new DropDown<string>()
			{
				Options = new List<Option<string>>(),
				IsDisplayFilterShown = true,
				IsSorted = true,
			};
			protocolVersionDropDown_.Changed += (sender, args) => OnSelectedProtocolVersionChanged();

			parameterSelector_ = new MultiProtocolParameterSelector("", "", engine);
			parameterSelector_.Changed += (sender, args) => Changed?.Invoke(this, EventArgs.Empty);
			OnSelectedProtocolChanged();

			AddWidget(protocolNameLabel, 0, 0);
			AddWidget(protocolNameDropDown_, 0, 1, 1, parameterSelector_.ColumnCount - 1);

			AddWidget(protocolVersionLabel, 1, 0);
			AddWidget(protocolVersionDropDown_, 1, 1, 1, parameterSelector_.ColumnCount - 1);

			AddSection(parameterSelector_, 2, 0);
		}
	}
}
