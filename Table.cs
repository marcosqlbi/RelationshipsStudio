using Serilog;
using Microsoft.AnalysisServices.Tabular;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace RelationshipsStudio
{
    public class Table
    {
        public string Name { get; set; }
        private Model ParentModel { get; }
        public Table(string name, Model model)
        {
            Name = name;
            ParentModel = model;
        }
        private List<Path> GetAllSourcePaths(Table[]? excludeTables = null )
        {
            var paths = new List<Path>();
            excludeTables = 
                (excludeTables != null) 
                ? excludeTables.Union(new Table[] { this }).ToArray()
                : new Table[] { this };
            var list = SourceFilterRelationships;

            foreach (var relationship in list)
            {
                var destTable = relationship.GetDestTable(this);
                if (excludeTables.Any(t => t == destTable))
                {
                    Log.Verbose($"Skip relationship for circular reference to table {destTable.Name}");
                    continue;
                }
                var newPath = new Path(this,destTable,relationship);
                Debug.Assert(!paths.Any(p => p == newPath), "Found duplicated path during GetAllSourcePaths"); 
                paths.Add(newPath);

                var newPaths = destTable.GetAllSourcePaths(excludeTables);
                foreach (var path in newPaths)
                {
                    paths.Add(newPath + path);
                }
            }
            return paths;
        }

        public IEnumerable<Path> SourcePaths
        {
            get => GetAllSourcePaths();
        }

        /// <summary>
        /// List of relationships where the table is the source of filter propagation
        /// </summary>
        public IEnumerable<Relationship> SourceFilterRelationships {
            get
            {
                var result =
                    from relationship in ParentModel.Relationships
                    where relationship.IsFilterSourceTable(this)
                    select relationship;
                return result;
            }
        }

        /// <summary>
        /// List of relationships where the table is the destination filter propagation
        /// </summary>
        public IEnumerable<Relationship> DestFilterRelationships
        {
            get
            {
                var result =
                    from relationship in ParentModel.Relationships
                    where relationship.IsFilterDestTable(this)
                    select relationship;
                return result;
            }
        }
    }
}
