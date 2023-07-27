using Serilog;
using System.Diagnostics;
using Microsoft.AnalysisServices;
using Microsoft.AnalysisServices.Tabular;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Tracing;
using System.CodeDom;

namespace RelationshipsStudio
{
    public class Relationship
    {
        public enum PropagationType
        {
            None,
            OneToMany,
            ManyToOne,
            ManyToMany,
            OneToOne
        };

        public enum CrossFilterDirection
        {
            None,
            Both,
            OneWay,
            OneWay_Inverted
        };

        public enum Cardinality
        {
            None,
            One,
            Many
        }

        public string GetSymbol(Table sourceTable)
            => $" ({
                Relationship.GetSymbol(GetCardinality(sourceTable))})-{
                Relationship.GetSymbol(CrossFilter,InvertSourceDest(sourceTable))}-({
                Relationship.GetSymbol(GetDestCardinality(sourceTable))})";
        
        public static string GetSymbol(Cardinality cardinality)
            => cardinality switch { 
                Cardinality.One => "1", 
                Cardinality.Many => "*", 
                _ => "?" };

        public static string GetSymbol(CrossFilterDirection direction, bool invert = false)
            => direction switch { 
                CrossFilterDirection.None => "| ", 
                CrossFilterDirection.Both => "<>", 
                CrossFilterDirection.OneWay => invert ? ">" : "<", 
                CrossFilterDirection.OneWay_Inverted => invert ? "<" : ">", 
                _ => "??" };

        public PropagationType GetFilterPropagation(Table sourceTable)
            => GetFilterPropagation(InvertSourceDest(sourceTable));

        public PropagationType GetFilterPropagation(bool invert)
        {
            return ToCardinality switch
            {
                Cardinality.One
                    => FromCardinality switch
                    {
                        Cardinality.One => PropagationType.OneToOne,
                        Cardinality.Many => invert ? PropagationType.ManyToOne : PropagationType.OneToMany,
                        _ => PropagationType.None,
                    },
                Cardinality.Many
                    => FromCardinality switch
                    {
                        Cardinality.One => invert ? PropagationType.OneToMany : PropagationType.ManyToOne,
                        Cardinality.Many => PropagationType.ManyToMany,
                        _ => PropagationType.None,
                    },
                _ => PropagationType.None,
            };
        }
        public Table From { get; }
        public string FromColumn { get; }
        public Cardinality FromCardinality { get; }
        public Table To { get; }
        public string ToColumn { get; }
        public Cardinality ToCardinality { get; }
        public bool Active
        {
            get
            {
                return Weight >= 0 && CrossFilter != CrossFilterDirection.None;
            }
            init
            {
                Weight = value ? 0 : -1;
            }
        }
        /// <summary>
        /// Weight is -1 for inactive relationships and 0 for active relationships
        /// Weight can be dynamically modified by USERELATIONSHIP starting by 1 if 
        /// the initial state is disabled and increasing by 1 for each nested USERELATIONSHIP used
        /// NOTE: the weight corresponds to the level of nested CALCULATE/CALCULATETABLE for USERELATIONSHIP
        ///       and it is not the incremental addition to the previous value (to be tested, but this is the doc)
        /// See: https://learn.microsoft.com/en-us/power-bi/transform-model/desktop-relationships-understand#weight
        /// </summary>
        public int Weight { get; set; }

        private CrossFilterDirection _originalDirection = CrossFilterDirection.OneWay;
        /// <summary>
        /// Model direction that cannot be modified - it is the value 
        /// </summary>
        public CrossFilterDirection OriginalDirection
        {
            get => _originalDirection;
            init
            {
                _originalDirection = value;
                CrossFilter = _originalDirection;
            }
        }
        /// <summary>
        /// Filter direction that can be modified by CROSSFILTER
        /// </summary>
        public CrossFilterDirection CrossFilter { get; set; } = CrossFilterDirection.OneWay;
        // TODO replace RelationshipEndCardinality with a local Cardinality enum
        public Relationship(Table from, Cardinality fromCardinality, string fromColumn, Table to, Cardinality toCardinality, string toColumn, bool active)
        {
            From = from;
            FromCardinality = fromCardinality;
            FromColumn = fromColumn;
            To = to;
            ToCardinality = toCardinality;
            ToColumn = toColumn;
            Active = active;
        }
        internal bool InvertSourceDest(Table sourceTable)
        {
            bool? result = (sourceTable == this.From) ? true : (sourceTable == this.To) ? false : null;
            Debug.Assert(result.HasValue, "Invalid source table");
            return result.Value;
        }
        public Cardinality GetCardinality(Table table)
            => InvertSourceDest(table) ? this.FromCardinality : this.ToCardinality;

        public Cardinality GetSourceCardinality(Table sourceTable)
            => InvertSourceDest(sourceTable) ? this.FromCardinality : this.ToCardinality;

        public string GetSourceColumn(Table sourceTable)
            => InvertSourceDest(sourceTable) ? this.FromColumn : this.ToColumn;

        public Cardinality GetDestCardinality(Table sourceTable)
            => InvertSourceDest(sourceTable) ? this.ToCardinality : this.FromCardinality;

        public string GetDestColumn(Table sourceTable)
            => InvertSourceDest(sourceTable) ? this.ToColumn : this.FromColumn;

        public Table GetDestTable(Table sourceTable)
        {
            Table destTable = InvertSourceDest(sourceTable) ? this.To : this.From;
            if (!IsFilterDestTable(destTable))
            {
                string errorMessage = $"Table {destTable.Name} is not a destination of filter propagation";
                Log.Error(errorMessage);
                throw new Exception(errorMessage);
            }
            return destTable;
        }
        public string GetColumn(Table sourceTable)
        {
            string columnName = InvertSourceDest(sourceTable) ? this.FromColumn : this.ToColumn;
            return columnName;
        }
        public bool IsFilterDestTable(Table table)
        {
            var crossFilter = (CrossFilter == CrossFilterDirection.None) ? OriginalDirection : CrossFilter;
            switch (crossFilter)
            {
                case CrossFilterDirection.OneWay:
                    return table == this.From;
                case CrossFilterDirection.OneWay_Inverted:
                    return table == this.To;
                case CrossFilterDirection.Both:
                    return table == this.To || table == this.From;
                case CrossFilterDirection.None:
                    return false;
                default:
                    Log.Debug($"CrossFilter Automatic in relationship for table {table.Name}");
                    return false;
            }
        }

        public bool IsFilterSourceTable(Table table)
        {
            var crossFilter = (CrossFilter == CrossFilterDirection.None) ? OriginalDirection : CrossFilter;
            switch (crossFilter)
            {
                case CrossFilterDirection.OneWay:
                    return table == this.To;
                case CrossFilterDirection.OneWay_Inverted:
                    return table == this.From;
                case CrossFilterDirection.Both:
                    return table == this.To || table == this.From;
                case CrossFilterDirection.None:
                    return false;
                default:
                    Log.Debug($"CrossFilter Automatic in relationship for table {table.Name}");
                    return false;
            }
        }

        public Relationship CloneApplyingModifiers(IEnumerable<RelationshipModifier> useRelationships)
        {
            Relationship cloned = (Relationship)this.MemberwiseClone();

            var modifiersUseRelationships =
                from rm in useRelationships
                where rm.Type == RelationshipModifier.ModifierType.ActiveState
                      && rm.Relationship == this
                group rm by rm.Level into level
                orderby level descending
                select level;

            // Use the highest level of USERELATIONSHIP as weight value
            // for the relationship and activate the relationship
            var topLevel = modifiersUseRelationships.FirstOrDefault();
            if (topLevel != null)
            {
                cloned.Weight = topLevel.Key;
            }

            var modifiersCrossFilter =
                from rm in useRelationships
                where rm.Type == RelationshipModifier.ModifierType.CrossFilterDirection
                      && rm.Relationship == this
                group rm by rm.Level into level
                orderby level descending
                select level;

            // Use the highest level of CROSSFILTER to define the filter direction
            var topDirection = modifiersCrossFilter.FirstOrDefault();
            if (topDirection != null)
            {
                if (topDirection.Count() > 1)
                {
                    // TODO define a custom exception
                    throw new Exception("More than one CROSSFILTER applied at the same level");
                }
                cloned.CrossFilter = topDirection.First().Direction;
            }

            return cloned;
        }
    }

    public class RelationshipModifier
    {

        public enum ModifierType
        {
            ActiveState,
            CrossFilterDirection
        }

        public RelationshipModifier(Relationship relationship, int level, bool active)
        {
            Type = ModifierType.ActiveState;
            Relationship = relationship;
            Level = level;
            Active = active;
        }
        public RelationshipModifier(Relationship relationship, int level, Relationship.CrossFilterDirection direction)
        {
            Type = ModifierType.CrossFilterDirection;
            Relationship = relationship;
            Level = level;
            Direction = direction;
        }
        public Relationship Relationship { get; }

        public ModifierType Type { get; }

        public int Level { get; }

        public Relationship.CrossFilterDirection Direction { get; }

        public bool Active { get; }
    }


    public static class RelationshipTools
    {
        static public IEnumerable<Relationship> ApplyModifiers(this IEnumerable<Relationship> relationships, IEnumerable<RelationshipModifier>? useRelationships)
            => (useRelationships != null)
                ? from r in relationships
                  select r.CloneApplyingModifiers(useRelationships)
                : relationships;


        public static Relationship.Cardinality ConvertCardinality(this RelationshipEndCardinality cardinality)
            => cardinality switch
            {
                RelationshipEndCardinality.None => Relationship.Cardinality.None,
                RelationshipEndCardinality.One => Relationship.Cardinality.One,
                RelationshipEndCardinality.Many => Relationship.Cardinality.Many,
                _ => Relationship.Cardinality.None
            };
        static internal Relationship? FindRelationship(this IEnumerable<Relationship> relationships, (string Table, string Column) a, (string Table, string Column) b)
            => relationships.FindRelationship(a.Table, a.Column, b.Table, b.Column);

        static internal Relationship? FindRelationship(this IEnumerable<Relationship> relationships, string tableA, string columnA, string tableB, string columnB)
            => relationships.FirstOrDefault(r =>
                    (r.From.Name == tableA && r.FromColumn == columnA && r.To.Name == tableB && r.ToColumn == columnB)
                    || (r.From.Name == tableB && r.FromColumn == columnB && r.To.Name == tableA && r.ToColumn == columnA)
                );

        static internal PriorityType GetPriority(this IEnumerable<(Relationship Relationship, bool Inverted)> evaluateRelationships)
        {
            // 1. A path consisting of one-to-many relationships.
            //    Implementation: we consider that To must be ONE, as one-to-one are considered
            //                    like one-to-many, so we do not consider the one part
            if (evaluateRelationships.All(r =>
            {
                var filterPropagation = r.Relationship.GetFilterPropagation(r.Inverted);
                return filterPropagation == Relationship.PropagationType.OneToMany
                           || filterPropagation == Relationship.PropagationType.OneToOne;
            }))
            {
                return PriorityType.Type1_OneToMany;
            }

            // 2. A path consisting of one-to-many or many-to-many relationships
            //    Implementation: also in this case all the relationships must be either
            //                    ONE in the To, or both From and To must be MANY
            if (evaluateRelationships.All(r =>
            {
                var filterPropagation = r.Relationship.GetFilterPropagation(r.Inverted);
                return filterPropagation == Relationship.PropagationType.OneToMany
                           || filterPropagation == Relationship.PropagationType.ManyToMany;
            }))
            {
                return PriorityType.Type2_OneOrManyToMany;
            }

            // 3. A path consisting of many-to-one relationships.
            //    TODO could it be also one-to-one???
            //    Implementation: all the relationships must have To = MANY and From = ONE
            if (evaluateRelationships.All(r => r.Relationship.GetFilterPropagation(r.Inverted) == Relationship.PropagationType.ManyToOne))
            {
                return PriorityType.Type3_ManyToOne;
            }

            // 4. A path consisting of one-to-many relationships from the source table to an intermediate table followed by many-to-one relationships from the intermediate table to the target table.
            //    TODO could it be also one-to-one???
            //    Implementation: split the control in two parts, first skip the one-to-many, then check that the remaing part is all many-to-one
            if (evaluateRelationships.TakeWhile(r =>
            {
                var filterPropagation = r.Relationship.GetFilterPropagation(r.Inverted);
                return filterPropagation == Relationship.PropagationType.OneToMany
                           || filterPropagation == Relationship.PropagationType.OneToOne;
            }).Any())
            {
                if (evaluateRelationships
                        .SkipWhile(r =>
                        {
                            var filterPropagation = r.Relationship.GetFilterPropagation(r.Inverted);
                            return filterPropagation == Relationship.PropagationType.OneToMany
                                        || filterPropagation == Relationship.PropagationType.OneToOne;
                        })
                        .All(r => r.Relationship.GetFilterPropagation(r.Inverted) == Relationship.PropagationType.ManyToOne))
                {
                    return PriorityType.Type4_OneToIntermediateToOne;
                }
            }


            // 5. A path consisting of one-to-many or many-to-many relationships from the source table to an intermediate table followed by many-to-one or many-to-many relationships from the intermediate table to the target table.
            if (evaluateRelationships.TakeWhile(r =>
            {
                var filterPropagation = r.Relationship.GetFilterPropagation(r.Inverted);
                return filterPropagation == Relationship.PropagationType.OneToMany
                           || filterPropagation == Relationship.PropagationType.ManyToMany;
            }).Any())
            {
                if (evaluateRelationships
                        .SkipWhile(r =>
                        {
                            var filterPropagation = r.Relationship.GetFilterPropagation(r.Inverted);
                            return filterPropagation == Relationship.PropagationType.OneToMany
                                            || filterPropagation == Relationship.PropagationType.ManyToMany;
                        })
                        .All(r => r.Relationship.GetFilterPropagation(r.Inverted) == Relationship.PropagationType.ManyToOne))
                {
                    return PriorityType.Type5_ManyToIntermediateToMany;
                }
            }

            // 6. Any other path.
            return PriorityType.Type6_Other;
        }

    }
}