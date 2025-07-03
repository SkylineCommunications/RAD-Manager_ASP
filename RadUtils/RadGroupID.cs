namespace RadUtils
{
	using System;

	public interface IRadGroupID
	{
		int DataMinerID { get; set; }

		string GroupName { get; set; }
	}

	public struct RadGroupID : IRadGroupID
	{
		public RadGroupID(int dataMinerID, string groupName)
		{
			DataMinerID = dataMinerID;
			GroupName = groupName;
		}

		public int DataMinerID { get; set; }

		public string GroupName { get; set; }

		public override bool Equals(object obj)
		{
			if (obj is RadGroupID other)
			{
				return DataMinerID == other.DataMinerID && string.Equals(GroupName, other.GroupName, StringComparison.OrdinalIgnoreCase);
			}

			return false;
		}

		public override int GetHashCode()
		{
			int hash = DataMinerID;
			hash ^= GroupName?.GetHashCode() ?? 0;

			return hash;
		}
	}

	public struct RadSubgroupID : IRadGroupID
	{
		public RadSubgroupID(int dataMinerID, string groupName, Guid subgroupID)
		{
			DataMinerID = dataMinerID;
			GroupName = groupName;
			SubgroupID = subgroupID;
			SubgroupName = null;
		}

		public RadSubgroupID(int dataMinerID, string groupName, string subgroupName)
		{
			DataMinerID = dataMinerID;
			GroupName = groupName;
			SubgroupID = null;
			SubgroupName = subgroupName;
		}

		public int DataMinerID { get; set; }

		public string GroupName { get; set; }

		public Guid? SubgroupID { get; set; }

		public string SubgroupName { get; set; }

		public override bool Equals(object obj)
		{
			if (obj is RadSubgroupID other)
			{
				return DataMinerID == other.DataMinerID &&
					string.Equals(GroupName, other.GroupName, StringComparison.OrdinalIgnoreCase) &&
					SubgroupID == other.SubgroupID &&
					string.Equals(SubgroupName, other.SubgroupName, StringComparison.OrdinalIgnoreCase);
			}

			return false;
		}

		public override int GetHashCode()
		{
			int hash = DataMinerID;
			hash ^= GroupName?.GetHashCode() ?? 0;
			hash ^= SubgroupID?.GetHashCode() ?? 0;
			hash ^= SubgroupName?.GetHashCode() ?? 0;
			return hash;
		}
	}
}
