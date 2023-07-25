using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace RelationshipsStudio
{
    public enum PriorityType
    {
        Undefined = 0,
        Type1_OneToMany = 1,
        Type2_OneOrManyToMany,
        Type3_ManyToOne,
        Type4_OneToIntermediateToOne,
        Type5_ManyToIntermediateToMany,
        Type6_Other
    }

    public class Path : IEquatable<Path>, ICloneable
    {
        public Table From { get; }
        public Table To { get; }
        public IEnumerable<Relationship> Relationships { get; internal set; }
        public IEnumerable<(Relationship Relationship, bool Inverted)> GetDirectionRelationships(IEnumerable<RelationshipModifier>? useRelationships)
        {
            var sourceTable = From;
            foreach (var r in Relationships.ApplyModifiers(useRelationships))
            {
                var destTable = r.GetDestTable(sourceTable);
                yield return (Relationship: r, Inverted: r.InvertSourceDest(sourceTable));
                sourceTable = destTable;
            }
        }
        public Path(Table from, Table to, IEnumerable<Relationship> relationships)
        {
            From = from;
            To = to;
            Relationships = relationships;
        }
        public Path(Table from, Table to, Relationship relationship)
            : this(from, to, new Relationship[] { relationship }) { }

        private Path(Path left, Path right)
        {
            From = left.From;
            To = right.To;
            Relationships =
                left.Relationships.Union(right.Relationships);
        }

        public PriorityType Priority { get; set; }
        public int Weight { get; set; }
        // Depth corresponds the number of relationships in the complete path
        public int Depth { get => Relationships.Count(); }
        /// <summary>
        /// The path is active when all the relationships are active
        /// </summary>
        public bool Active { get; set; }
        /// <summary>
        /// The path is current only after disambiguation
        /// If more paths are current, there is an ambiguous situation
        /// </summary>
        public bool Current { get; set; }

        public static Path operator +(Path left, Path right)
            => new(left, right);

        #region Implement value semantics - we don't use record because the Weigths could change
        public override bool Equals(object? obj)
            => this.Equals(obj as Path);

        public bool Equals(Path? other)
        {
            if (other is null) return false;
            if (Object.ReferenceEquals(this, other)) return true;
            if (this.GetType() != other.GetType()) return false;
            return (From == other.From && To == other.To && Relationships.SequenceEqual(other.Relationships));
        }

        public override int GetHashCode() => (From, To, Relationships.GetHashCode()).GetHashCode();

        public static bool operator ==(Path? lhs, Path? rhs)
        {
            if (lhs is null)
            {
                if (rhs is null)
                {
                    return true;
                }

                // Only the left side is null.
                return false;
            }
            // Equals handles case of null on right side.
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Path? lhs, Path? rhs) => !(lhs == rhs);
        #endregion

        public Path Clone()
        {
            Path cloned = (Path)this.MemberwiseClone();
            return cloned;
        }

        object ICloneable.Clone() => this.Clone();

    }

    public static class PathTools {
        static public IEnumerable<Path> Disambiguate(this IEnumerable<Path> paths, IEnumerable<RelationshipModifier>? useRelationships = null)
        {
            // Validate paths have the same endpoint
            var test =
                from p in paths
                group p by (FromTable: p.From, toTable: p.To) into p2p
                select p2p;
            Debug.Assert(test.Count() == 1, "Different endpoints in Disambiguate request");

            // Copy existing paths (deep copy internal relationships)
            // It's important to create a list, otherwise the Clone is
            // executed at every reference of copyPaths!
            var copyPaths = (from p in paths select p.Clone()).ToList();
            var totalPaths = copyPaths.Count;

            // Remove common relationships
            var relationships =
                from p in copyPaths
                from r in p.Relationships
                group p by r into rel
                select new { Relationship = rel.Key, IncludedPaths = rel.Count() };

            var commonRelationships =
                from r in relationships
                where r.IncludedPaths == totalPaths
                select r.Relationship;

            var inspectPaths =
                from p in copyPaths
                select new
                {
                    Path = p,
                    EvaluateRelationships =
                        (from r in p.GetDirectionRelationships(useRelationships)
                         where !commonRelationships.Any(common => common == r.Relationship)
                         select r).ToList()
                };

            // Apply Priority & Weight
            foreach (var p in inspectPaths)
            {
                // Receive a list of USERELATIONSHIP / CROSSFILTER changes
                // it affects both active and relationships weight
                var pathRelationships = p.Path.Relationships.ApplyModifiers(useRelationships);

                // Apply Active state (path active if all the relationships are active)
                p.Path.Active = pathRelationships.All(r => r.Active);

                // Apply Priority
                p.Path.Priority = p.EvaluateRelationships.GetPriority();

                // Apply Weight
                p.Path.Weight = pathRelationships.Max(r => r.Weight);

            }

            // Sort by prioritization - in each group there are paths with the same priority
            // 2023-07-25: added disambiguation by longer depth of relationships - not documented,
            //             but it corresponds to the current implementation, verified for Type 1 and Type 2
            var prioritization =
                from p in inspectPaths
                where p.Path.Active == true
                group p by (p.Path.Priority, p.Path.Weight, Depth: p.Path.Relationships.Count()) into priorityBucket
                orderby priorityBucket.Key.Priority ascending, priorityBucket.Key.Weight descending, priorityBucket.Key.Depth descending
                // Commented version that corresponds to documented behavior (ambigous path with different length should raise an error)
                // group p by (p.Path.Priority, p.Path.Weight) into priorityBucket
                // orderby priorityBucket.Key.Priority ascending, priorityBucket.Key.Weight descending
                select priorityBucket;

            var priorityPaths = prioritization.FirstOrDefault();
            if (priorityPaths == null)
            {
                Log.Warning("No active paths after Disambiguate");
            }
            else
            {
                foreach (var p in priorityPaths)
                {
                    p.Path.Current = true;
                }
            }

            // Return disambiguated paths
            return copyPaths;
        }

    }
}
