namespace RadWidgets
{
	using System;
	using System.Collections.Generic;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public delegate bool VisibilityChecker();

	public struct VisibilitySectionChildInfo<T>
	{
		public VisibilitySectionChildInfo(T child, VisibilityChecker visibilityChecker)
		{
			Child = child;
			VisibilityChecker = visibilityChecker;
		}

		public T Child { get; set; }

		public VisibilityChecker VisibilityChecker { get; set; }
	}

	public class VisibilitySection : Section
	{
		private readonly List<VisibilitySectionChildInfo<Widget>> _childWidgets = new List<VisibilitySectionChildInfo<Widget>>();
		private readonly List<VisibilitySectionChildInfo<Section>> _childSections = new List<VisibilitySectionChildInfo<Section>>();

		public override bool IsVisible
		{
			get => IsSectionVisible;
			set
			{
				if (IsSectionVisible == value)
					return;

				IsSectionVisible = value;

				foreach (var child in _childWidgets)
					child.Child.IsVisible = IsSectionVisible && (child.VisibilityChecker != null ? child.VisibilityChecker() : true);

				foreach (var child in _childSections)
					child.Child.IsVisible = IsSectionVisible && (child.VisibilityChecker != null ? child.VisibilityChecker() : true);
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the section itself is visible. Changing this property will not show or hide any of the child widgets or sections.
		/// </summary>
		protected bool IsSectionVisible { get; set; } = true;

		public void AddSection(Section section, ILayout layout, VisibilityChecker visibilityChecker)
		{
			_childSections.Add(new VisibilitySectionChildInfo<Section>(section, visibilityChecker));
			base.AddSection(section, layout);
		}

		public new void AddSection(Section section, ILayout layout) => AddSection(section, layout, null);

		public void AddSection(Section section, int row, int column, VisibilityChecker visibilityChecker)
		{
			_childSections.Add(new VisibilitySectionChildInfo<Section>(section, visibilityChecker));
			base.AddSection(section, row, column);
		}

		public new void AddSection(Section section, int row, int column) => AddSection(section, row, column, null);

		public void AddWidget(Widget widget, IWidgetLayout layout, VisibilityChecker visibilityChecker)
		{
			_childWidgets.Add(new VisibilitySectionChildInfo<Widget>(widget, visibilityChecker));
			base.AddWidget(widget, layout);
		}

		public new void AddWidget(Widget widget, IWidgetLayout layout) => AddWidget(widget, layout, null);

		public void AddWidget(
			Widget widget,
			int row,
			int column,
			VisibilityChecker visibilityChecker,
			HorizontalAlignment horizontalAlignment,
			VerticalAlignment verticalAlignment)
		{
			_childWidgets.Add(new VisibilitySectionChildInfo<Widget>(widget, visibilityChecker));
			base.AddWidget(widget, row, column, horizontalAlignment, verticalAlignment);
		}

		public new void AddWidget(
			Widget widget,
			int row,
			int column,
			HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment verticalAlignment = VerticalAlignment.Center)
			=> AddWidget(widget, row, column, null, horizontalAlignment, verticalAlignment);

		public void AddWidget(
			Widget widget,
			int row,
			int column,
			int rowSpan,
			int columnSpan,
			VisibilityChecker visibilityChecker,
			HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment verticalAlignment = VerticalAlignment.Center)
		{
			_childWidgets.Add(new VisibilitySectionChildInfo<Widget>(widget, visibilityChecker));
			base.AddWidget(widget, row, column, rowSpan, columnSpan, horizontalAlignment, verticalAlignment);
		}

		public new void AddWidget(
			Widget widget,
			int row,
			int column,
			int rowSpan,
			int columnSpan,
			HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment verticalAlignment = VerticalAlignment.Center)
			=> AddWidget(widget, row, column, rowSpan, columnSpan, null, horizontalAlignment, verticalAlignment);
	}
}
