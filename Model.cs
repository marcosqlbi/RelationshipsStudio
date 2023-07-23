using Microsoft.AnalysisServices.Tabular;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RelationshipsStudio
{
    public class Model
    {
        public List<Table> Tables { get; } = new List<Table>();
        public List<Relationship> Relationships { get; } = new List<Relationship>();

        public IEnumerable<Path> GetPaths((Table FromTable, Table ToTable) key)
            => GetPaths(key.FromTable, key.ToTable);

        public IEnumerable<Path> GetPaths(Table fromTable, Table toTable)
        {
            return
                from t in Tables
                from p in t.SourcePaths
                where p.From == fromTable && p.To == toTable
                select p;
        }

        public IEnumerable<IGrouping<(Table FromTable, Table ToTable), Path>> Ambiguities 
        { 
            get =>  from t in Tables
                    from p in t.SourcePaths
                    group p by (FromTable: p.From, ToTable: p.To) into p2p
                    where p2p.Count() > 1
                    select p2p;
        }

        public Path GetActivePath(Table fromTable, Table toTable)
        {
            // TODO
            throw new NotImplementedException();
        }

    }
}
