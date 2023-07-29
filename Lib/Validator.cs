using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelationshipsStudio
{
    /// <summary>
    /// Function tools to validate the execution of a path by using 
    /// the relationship in the model (DaxValidatePath) and the simulation 
    /// of the propagation without any relationship (DaxSimulatePath)
    /// </summary>
    public static class Validator
    {
        private static string TableReference(string table)
            => @$"'{table}'";

        private static string ColumnReference(string table, string column)
            => @$"{TableReference(table)}[{column}]";

        public static string DaxValidatePath(this Model model, Path path, IEnumerable<RelationshipModifier>? useRelationships = null)
        {
            var relationships = path.Relationships.ApplyModifiers(useRelationships).Select(r =>
            {
                bool isManyToMany = r.FromCardinality == Relationship.Cardinality.Many 
                                        && r.ToCardinality == Relationship.Cardinality.Many;
                var direction = r.CrossFilter switch
                {
                    Relationship.CrossFilterDirection.OneWay => isManyToMany ? "ONEWAY_RIGHTFILTERSLEFT" : "ONEWAY",
                    Relationship.CrossFilterDirection.OneWay_Inverted => isManyToMany ? "ONEWAY_LEFTFILTERSRIGHT" : "ONEWAY",
                    Relationship.CrossFilterDirection.Both => "BOTH",
                    _ => ""
                };
                if (string.IsNullOrEmpty(direction))
                {
                    throw new ApplicationException("Invalid crossfilter for relationship in active path.");
                }
                return @$",
            USERELATIONSHIP ( {ColumnReference(r.From.Name, r.FromColumn)}, {ColumnReference(r.To.Name, r.ToColumn)} ),
            CROSSFILTER ( {ColumnReference(r.From.Name, r.FromColumn)}, {ColumnReference(r.To.Name, r.ToColumn)}, {direction} )";
            });

            var allRelationships = model.Relationships.Select(r =>
            {
                return @$",
    CROSSFILTER ( {ColumnReference(r.From.Name, r.FromColumn)}, {ColumnReference(r.To.Name, r.ToColumn)}, NONE )";
            });
            string queryEngine = @$"EVALUATE
CALCULATETABLE (
    SUMMARIZECOLUMNS ( 
        {ColumnReference(path.From.Name, path.Relationships.First().GetSourceColumn(path.From))},
        ""Result"", CALCULATE (
            COUNTROWS ( VALUES ( {TableReference(path.To.Name)} ) ){string.Concat(relationships)}
        )
    ){string.Concat(allRelationships)}
)
ORDER BY {ColumnReference(path.From.Name, path.Relationships.First().GetSourceColumn(path.From))}
";
            return queryEngine;
        }

        public static string DaxSimulatePath(this Model model, Path path, IEnumerable<RelationshipModifier>? useRelationships = null)
        {
            const string STEP_PREFIX = "__Step";
            int varStep = 0;
            Table fromTable = path.From;
            string? previousColumnReference = null;
            var steps = path.Relationships.ApplyModifiers(useRelationships).Select(r =>
            {
                string OneToMany()
                {
                    string columnTo = ColumnReference(r.To.Name, r.ToColumn);
                    string columnFrom = ColumnReference(r.From.Name, r.FromColumn);
                    bool showPreviousColumnreference = 
                        previousColumnReference != null 
                        && previousColumnReference != columnTo 
                        && previousColumnReference != columnFrom;
                    return @$"
        CALCULATETABLE ( 
            SUMMARIZE ( VALUES ( {TableReference(r.From.Name)} ), {(showPreviousColumnreference ? $"{previousColumnReference}, ": "")}{columnTo}, {columnFrom} ),
            USERELATIONSHIP ( {columnFrom}, {columnTo} ),
            CROSSFILTER ( {columnFrom}, {columnTo}, ONEWAY )
        )" + "\r\n";
                }

                string ManyToOne()
                {
                    string columnTo = ColumnReference(r.To.Name, r.ToColumn);
                    string columnFrom = ColumnReference(r.From.Name, r.FromColumn);
                    bool showPreviousColumnreference =
                        previousColumnReference != null
                        && previousColumnReference != columnTo
                        && previousColumnReference != columnFrom;
                    return @$"
        CALCULATETABLE ( 
            SUMMARIZE ( VALUES ( {TableReference(r.To.Name)} ), {columnFrom}, {columnTo} ),
            USERELATIONSHIP ( {columnFrom}, {columnTo} ),
            CROSSFILTER ( {columnFrom}, {columnTo}, BOTH )
        )" + "\r\n";
                }
                string ManyToMany()
                {
                    string columnFrom = r.InvertSourceDest(fromTable) ? ColumnReference(r.From.Name, r.FromColumn) : ColumnReference(r.To.Name, r.ToColumn);
                    string columnTo = r.InvertSourceDest(fromTable) ? ColumnReference(r.To.Name, r.ToColumn) : ColumnReference(r.From.Name, r.FromColumn);
                    string previousStep = varStep > 1 ? $"{STEP_PREFIX}{varStep - 1}" : $"VALUES ( {columnFrom} )";
                    return @$"
        GENERATE (
            {previousStep},
            CALCULATETABLE ( TREATAS ( VALUES ( {columnFrom} ), {columnTo} ) )
        )" + "\r\n";
                }

                string relationshipStep = $"    VAR {STEP_PREFIX}{++varStep} = " + (r.FromCardinality, r.ToCardinality) switch
                {
                    (Relationship.Cardinality.One, Relationship.Cardinality.One) => OneToMany(),
                    (Relationship.Cardinality.One, Relationship.Cardinality.Many) => ManyToOne(),
                    (Relationship.Cardinality.Many, Relationship.Cardinality.One) => OneToMany(),
                    (Relationship.Cardinality.Many, Relationship.Cardinality.Many) => ManyToMany(),
                    _ => throw new ApplicationException("Invalid cardinality in relationship of an active path.")
                };
                if (varStep > 1)
                {
                    relationshipStep += $"    VAR {STEP_PREFIX}{++varStep} = \r\n        NATURALLEFTOUTERJOIN ( {STEP_PREFIX}{varStep - 2}, {STEP_PREFIX}{varStep - 1} )\r\n";
                }

                fromTable = r.GetDestTable(fromTable);
                previousColumnReference = ColumnReference(fromTable.Name,r.GetDestColumn(fromTable));

                return relationshipStep;
            });

            var x = steps.ToList()
                .Append($"    VAR {STEP_PREFIX}{++varStep} =\r\n        NATURALLEFTOUTERJOIN ( {STEP_PREFIX}{varStep - 1}, {TableReference(path.To.Name)} )\r\n" )
                .Append($"    VAR Result=\r\n        GROUPBY( {STEP_PREFIX}{varStep}, {ColumnReference(path.From.Name, path.Relationships.First().GetSourceColumn(path.From))}, \"Result\", COUNTX ( CURRENTGROUP(), 1 ) )" );
            var allRelationships = model.Relationships.Select(r =>
            {
                return @$",
    CROSSFILTER ( {ColumnReference(r.From.Name, r.FromColumn)}, {ColumnReference(r.To.Name, r.ToColumn)}, NONE )";
            });
            string queryEngine = @$"EVALUATE
CALCULATETABLE (
{string.Concat(x)}
    RETURN Result{string.Concat(allRelationships)}
)
ORDER BY {ColumnReference(path.From.Name, path.Relationships.First().GetSourceColumn(path.From))}
"; 
            return queryEngine;
        }


    }
}
