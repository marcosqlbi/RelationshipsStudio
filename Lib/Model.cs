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
    /// <summary>
    /// Simple representation of a Tabular model 
    /// with lightweight Table and Relationship objects
    /// </summary>
    public class Model
    {
        /// <summary>
        /// Create a Model instance based on the content 
        /// of the Tabular database provided
        /// </summary>
        /// <param name="tabularDatabase"></param>
        /// <returns></returns>
        public static Model GetModel(Database tabularDatabase)
        {
            var model = new Model();
            foreach (var t in tabularDatabase.Model.Tables)
            {
                model.Tables.Add(new Table(t.Name, model));
            }
            foreach (SingleColumnRelationship r in tabularDatabase.Model.Relationships.Cast<SingleColumnRelationship>())
            {
                var fromTable = model.Tables.Find(t => t.Name == r.FromTable.Name);
                var toTable = model.Tables.Find(t => t.Name == r.ToTable.Name);
                Debug.Assert(fromTable != null);
                Debug.Assert(toTable != null);
                var studioRel = new Relationship(fromTable, r.FromCardinality.ConvertCardinality(), r.FromColumn.Name, toTable, r.ToCardinality.ConvertCardinality(), r.ToColumn.Name, r.IsActive)
                {
                    OriginalDirection = r.CrossFilteringBehavior switch
                    {
                        CrossFilteringBehavior.OneDirection => Relationship.CrossFilterDirection.OneWay,
                        CrossFilteringBehavior.BothDirections => Relationship.CrossFilterDirection.Both,
                        _ => Relationship.CrossFilterDirection.None,
                    }
                };
                model.Relationships.Add(studioRel);
            }
            return model;
        }

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
