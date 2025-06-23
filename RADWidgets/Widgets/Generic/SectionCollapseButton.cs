namespace RadWidgets.Widgets.Generic
{
	using System.Collections.Generic;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	/// <summary>
	/// A version of the <see cref="CollapseButton"/> that accepts <see cref="Section"/> objects as children.
	/// </summary>
	public class SectionCollapseButton : CollapseButton
	{
		private readonly List<Section> _linkedSections;

		public SectionCollapseButton(bool isCollapsed = false) : this(null, isCollapsed)
		{
		}

		public SectionCollapseButton(List<Section> linkedSections, bool isCollapsed = false) : this(linkedSections, new List<Widget>(), isCollapsed)
		{
			_linkedSections = linkedSections ?? new List<Section>();
			this.Pressed += (sender, args) => OnPressed();
		}

		public SectionCollapseButton(List<Section> linkedSections, List<Widget> linkedWidgets, bool isCollapsed = false) : base(linkedWidgets, isCollapsed)
		{
			_linkedSections = linkedSections ?? new List<Section>();
			this.Pressed += (sender, args) => OnPressed();
		}

		public List<Section> LinkedSections => _linkedSections;

		private void OnPressed()
		{
			foreach (var section in LinkedSections)
			{
				if (section != null)
					section.IsVisible = !IsCollapsed;
			}
		}

	}
}
