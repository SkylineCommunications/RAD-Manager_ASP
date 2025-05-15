namespace RadWidgets
{
	using System;
	using System.Collections.Generic;
	using RadUtils;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class AddSubgroupDialog : Dialog
	{
		private readonly RadSubgroupEditor _subgroupEditor;
		private readonly Button _okButton;
		private readonly Button _cancelButton;

		public AddSubgroupDialog(IEngine engine, List<string> existingSubgroups, List<string> labels, double? parentAnomalyThreshold, int? parentMinimalDuration) : base(engine)
		{
			Title = "Add Subgroup";
			_subgroupEditor = new RadSubgroupEditor(engine, existingSubgroups, labels, parentAnomalyThreshold, parentMinimalDuration);

			_okButton = new Button("OK")
			{
				Style = ButtonStyle.CallToAction,
				Tooltip = "Create the subgroup with the specified settings.",
			};
			_okButton.Pressed += (s, e) => Accepted?.Invoke(this, EventArgs.Empty);

			_cancelButton = new Button("Cancel")
			{
				Tooltip = "Do not add the subgroup specified above.",
			};
			_cancelButton.Pressed += (s, e) => Cancelled?.Invoke(this, EventArgs.Empty);

			int row = 0;
			AddSection(_subgroupEditor, 0, 0);
			row += _subgroupEditor.RowCount;

			AddWidget(_cancelButton, row, 0);
			AddWidget(_okButton, row, 1, 1, _subgroupEditor.ColumnCount - 1);
		}

		public event EventHandler Accepted;

		public event EventHandler Cancelled;

		public RadSubgroupSettings Settings
		{
			get
			{
				return _subgroupEditor.Settings;
			}
		}
	}
}
