// TODO: try to not copy these files, but use the files from RadDataSourceUtils instead
namespace RadDataSourceUtils
{
	using System.Collections.Generic;

	public struct ElementKey
	{
		public int DataMinerID { get; set; }

		public int ElementID { get; set; }
	}

	public abstract class Cache<T>
	{
		private readonly Dictionary<ElementKey, T> cache_ = new Dictionary<ElementKey, T>();

		public bool TryGet(int dataMinerID, int elementID, out T value)
		{
			var key = new ElementKey { DataMinerID = dataMinerID, ElementID = elementID };
			if (cache_.TryGetValue(key, out value))
				return true;

			value = Fetch(dataMinerID, elementID);
			if (value != null)
			{
				cache_[key] = value;
				return true;
			}

			return false;
		}

		public bool TryGetFromCache(int dataMinerID, int elementID, out T value)
		{
			var key = new ElementKey { DataMinerID = dataMinerID, ElementID = elementID };
			return cache_.TryGetValue(key, out value);
		}

		protected abstract T Fetch(int dataMinerID, int elementID);
	}
}
