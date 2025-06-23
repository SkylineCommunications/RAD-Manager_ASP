namespace RadWidgets.Widgets.Generic
{
	using System.Collections.Generic;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class TooltipDropDown<T> : DropDown<T>
	{
		public TooltipDropDown() : base()
		{
			this.Changed += (sender, args) => UpdateTooltip();
			UpdateTooltip();
		}

		public TooltipDropDown(IEnumerable<Option<T>> options) : base(options)
		{
			this.Changed += (sender, args) => UpdateTooltip();
			UpdateTooltip();
		}

		public TooltipDropDown(IEnumerable<Option<T>> options, Option<T> selected) : base(options, selected)
		{
			this.Changed += (sender, args) => UpdateTooltip();
			UpdateTooltip();
		}

		public TooltipDropDown(IEnumerable<T> options, T selected) : base(options, selected)
		{
			this.Changed += (sender, args) => UpdateTooltip();
			UpdateTooltip();
		}

		private void UpdateTooltip()
		{
			this.Tooltip = SelectedOption?.DisplayValue ?? string.Empty;
		}
	}
}
