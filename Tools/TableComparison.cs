using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelationshipsStudio.Tools
{
    public class TableComparison
    {
        public static bool CompareDataRecords(List<IDataRecord> records1, List<IDataRecord> records2)
        {
            if (records1.Count != records2.Count)
            {
                return false;
            }

            return records1.SequenceEqual(records2, new DataRecordComparer());
        }

        public class DataRecordComparer : IEqualityComparer<IDataRecord>
        {
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
            public bool Equals(IDataRecord x, IDataRecord y)
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
            {
                if (x.FieldCount != y.FieldCount)
                {
                    return false;
                }

                for (int i = 0; i < x.FieldCount; i++)
                {
                    if (!object.Equals(x[i], y[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            public int GetHashCode(IDataRecord obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
