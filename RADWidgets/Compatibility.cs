namespace RadWidgets
{
	using System;
	using System.Runtime.CompilerServices;
	using Skyline.DataMiner.Analytics.Rad;

	public static class Compatibility
	{
		private static bool? _hasSharedModelGroups = null;

		/// <summary>
		/// Returns true if there are shared model groups in the current DataMiner, there are available from version TODO: version.
		/// </summary>
		/// <returns>True is shared model groups are available, false otherwise.</returns>
		public static bool HasSharedModelGroups()
		{
			if (_hasSharedModelGroups.HasValue)
				return _hasSharedModelGroups.Value;

			try // TODO: check whether this works
			{
				CheckAddRADSharedModelGroupMessage();
				_hasSharedModelGroups = true;
				return true;
			}
			catch (TypeLoadException)
			{
				_hasSharedModelGroups = false;
				return false;
			}
		}

		/// <summary>
		/// Supported from TODO: version.
		/// </summary>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void CheckAddRADSharedModelGroupMessage()
		{
			Type type = typeof(AddRADSharedModelGroupMessage);
		}
	}
}
