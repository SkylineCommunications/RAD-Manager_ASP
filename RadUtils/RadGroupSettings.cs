namespace RadUtils
{
	using System.Collections.Generic;
	using Skyline.DataMiner.Analytics.DataTypes;

	public class RadGroupInfo : RadGroupSettings
	{
		public bool IsMonitored { get; set; } = true;
	}

	/// <summary>
	/// Represents the settings for a RAD group.
	/// </summary>
	public class RadGroupSettings
	{
		/// <summary>
		/// Gets or sets the name of the RAD group.
		/// </summary>
		public string GroupName { get; set; }

		/// <summary>
		/// Gets or sets the parameters in the RAD group.
		/// </summary>
		public IEnumerable<ParameterKey> Parameters { get; set; }

		/// <summary>
		/// Gets or sets the options for the RAD group.
		/// </summary>
		public RadGroupOptions Options { get; set; }
	}

	public class RadGroupOptions
	{
		public static readonly double DefaultAnomalyThreshold = 3.0;
		public static readonly int DefaultMinimalDuration = 5;

		/// <summary>
		/// Gets or sets a value indicating whether to update the model on new data.
		/// </summary>
		public bool UpdateModel { get; set; }

		/// <summary>
		/// Gets or sets threshold above which an anomaly will be generated. Leave empty to use the default threshold.
		/// </summary>
		public double? AnomalyThreshold { get; set; }

		/// <summary>
		/// Gets or sets the minimal duration (in minutes) the anomaly score should be above the threshold before a suggestion event is generated. Leave empty to use the default duration.
		/// </summary>
		public int? MinimalDuration { get; set; }

		/// <summary>
		/// Gets the anomaly threshold set in the options, or the default value if none was set.
		/// </summary>
		/// <returns>The anomaly threshold.</returns>
		public double GetAnomalyThresholdOrDefault()
		{
			return AnomalyThreshold ?? DefaultAnomalyThreshold;
		}

		/// <summary>
		/// Gets the minimal anomaly duration set in the options, or the default value if none was set.
		/// </summary>
		/// <returns>The minimal duration.</returns>
		public int GetMinimalDurationOrDefault()
		{
			return MinimalDuration ?? DefaultMinimalDuration;
		}
	}
}
