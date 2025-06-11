namespace RadWidgets
{
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class MarginLabel : VisibilitySection
	{
		private readonly Label _label;
		private readonly WhiteSpace _topWhiteSpace;

		public MarginLabel(string text = null, int columnSpan = 1, int topMargin = 0)
		{
			_topWhiteSpace = new WhiteSpace()
			{
				MinHeight = topMargin,
			};

			_label = new Label(text ?? string.Empty);

			AddWidget(_topWhiteSpace, 0, 0, 1, columnSpan);
			AddWidget(_label, 1, 0, 1, columnSpan);
		}

		public string Text
		{
			get => _label.Text;
			set => _label.Text = value;
		}

		public int MaxWidth
		{
			get => _label.MaxWidth;
			set => _label.MaxWidth = value;
		}

		public int TopMargin
		{
			get => _topWhiteSpace.MinHeight;
			set => _topWhiteSpace.MinHeight = value;
		}
	}
}
