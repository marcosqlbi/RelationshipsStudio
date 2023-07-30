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
        ""Result"", IGNORE ( CALCULATE (
            COUNTROWS ( VALUES ( {TableReference(path.To.Name)} ) ){string.Concat(relationships)}
        ) )
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
                    bool lastRelationship = path.To == r.From || path.To == r.To;
                    string columnTo = ColumnReference(r.To.Name, r.ToColumn);
                    string columnFrom = ColumnReference(r.From.Name, r.FromColumn);
                    string tableFrom = TableReference(r.From.Name);
                    bool showPreviousColumnreference = 
                        previousColumnReference != null 
                        && previousColumnReference != columnTo 
                        && previousColumnReference != columnFrom;
                    return @$"
        CALCULATETABLE ( 
            SUMMARIZE ( 
                VALUES ( {tableFrom} ), 
                {(showPreviousColumnreference ? $"{previousColumnReference}, ": "")}{columnTo}, {columnFrom}{(lastRelationship?@$", 
                ""@Rows_Target"", COUNTROWS ( VALUES ( {tableFrom} ) )":"")}
            ),
            USERELATIONSHIP ( {columnFrom}, {columnTo} ),
            CROSSFILTER ( {columnFrom}, {columnTo}, ONEWAY )
        )" + "\r\n";
                }

                string ManyToOne()
                {
                    bool lastRelationship = path.To == r.From || path.To == r.To;
                    string columnTo = ColumnReference(r.To.Name, r.ToColumn);
                    string columnFrom = ColumnReference(r.From.Name, r.FromColumn);
                    string tableTo = TableReference(r.To.Name);
                    bool showPreviousColumnreference =
                        previousColumnReference != null
                        && previousColumnReference != columnTo
                        && previousColumnReference != columnFrom;
                    return @$"
        CALCULATETABLE ( 
            SUMMARIZE ( 
                VALUES ( {tableTo} ), 
                {columnFrom}, {columnTo}{(lastRelationship ? @$", 
                ""@Rows_Target"", COUNTROWS ( VALUES ( {tableTo} ) )" : "")}
            ),
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

            var firstColumnReference = ColumnReference(path.From.Name, path.Relationships.First().GetSourceColumn(path.From));
            var x = steps.ToList()
                .Append(path.Relationships.Last().GetDestCardinality(path.To) == Relationship.Cardinality.Many
                    ? $"    VAR {STEP_PREFIX}{++varStep} =\r\n        NATURALLEFTOUTERJOIN ( {STEP_PREFIX}{varStep - 1}, {TableReference(path.To.Name)} )\r\n"
                    : "" )
                .Append($"    VAR {STEP_PREFIX}{++varStep} =\r\n        NATURALLEFTOUTERJOIN ( VALUES ( {firstColumnReference} ), {STEP_PREFIX}{varStep - 1} )\r\n")
                .Append(@$"    VAR Result=
        GROUPBY (
            {STEP_PREFIX}{varStep}, 
            {firstColumnReference}, 
            ""Result"", {(path.Relationships.Last().GetFilterPropagation(path.To) == Relationship.PropagationType.ManyToMany 
                ? $"COUNTX ( CURRENTGROUP(), {ColumnReference(path.To.Name, path.Relationships.Last().GetSourceColumn(path.To))} )" 
                : @"SUMX ( CURRENTGROUP(), [@Rows_Target] )")} 
        )" );
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
