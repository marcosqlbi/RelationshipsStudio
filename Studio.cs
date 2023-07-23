using Serilog;
using Serilog.Sinks.RichTextBoxForms.Themes;
using System.Xml.Xsl;
using Microsoft.AnalysisServices.Tabular;
using System.Diagnostics;
using System.Security.Policy;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Runtime;
using System.Configuration;
using Microsoft.VisualBasic;

namespace RelationshipsStudio
{
    public partial class Studio : Form
    {
        public Studio()
        {
            InitializeComponent();
        }

        Database? TabularDatabase;
        Model? StudioModel;

        private void OpenBimFile()
        {
            string path = textFilename.Text;
            var json = File.ReadAllText(path);
            TabularDatabase = JsonSerializer.DeserializeDatabase(json);
            PopulateModel();
            DumpRelatioships();
        }

        private void PopulateModel()
        {
            if (TabularDatabase == null)
            {
                Log.Error("Tabular model not available.");
                return;
            }
            StudioModel = new Model();
            foreach (var t in TabularDatabase.Model.Tables)
            {
                StudioModel.Tables.Add(new Table(t.Name, StudioModel));
            }
            foreach (SingleColumnRelationship r in TabularDatabase.Model.Relationships.Cast<SingleColumnRelationship>())
            {
                var fromTable = StudioModel.Tables.Find(t => t.Name == r.FromTable.Name);
                var toTable = StudioModel.Tables.Find(t => t.Name == r.ToTable.Name);
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
                StudioModel.Relationships.Add(studioRel);
            }
        }

        private void DumpRelatioships()
        {
            string dumpResult = string.Empty;
            if (StudioModel == null)
            {
                Log.Error("StudioModel not available.");
                return;
            }

            dumpResult += "TABLES\r\n";
            foreach (var t in StudioModel.Tables)
            {
                dumpResult += $"  {t.Name}\r\n";
            }
            dumpResult += "RELATIONSHIPS\r\n";
            foreach (var r in StudioModel.Relationships)
            {
                string relationshipDirection = (r.CrossFilter == Relationship.CrossFilterDirection.None)
                    ? "---"
                    : $"{(r.CrossFilter == Relationship.CrossFilterDirection.OneWay ? "-" : "<")}-{(r.CrossFilter == Relationship.CrossFilterDirection.OneWay_Inverted ? "-" : ">")}";
                dumpResult += $"  (From:{r.From.Name} ({(r.FromCardinality == Relationship.Cardinality.Many ? '*' : '1')}){relationshipDirection}({(r.ToCardinality == Relationship.Cardinality.Many ? '*' : '1')}) To:{r.To.Name})\r\n";
            }

            Log.Verbose("Dump relationships:\r\n" + dumpResult);
        }

        private static string DumpTablePaths(Table table)
        {
            string dumpResult = string.Empty;
            dumpResult += table.SourcePaths.Any() ? $"TABLE {table.Name}\r\n" : string.Empty;
            foreach (var p in table.SourcePaths)
            {
                dumpResult = DumpPath(p);
            }
            return dumpResult;
        }

        private static string DumpPath(Path p)
        {
            string dumpResult = $"    {p.From.Name} --> {p.To.Name}\r\n";
            var firstRelationship = p.Relationships.First();
            var sourceTable = p.From;
            var sourceColumn = firstRelationship.GetColumn(sourceTable);
            dumpResult += $"        {sourceTable.Name}[{sourceColumn}]";
            foreach (var r in p.Relationships)
            {
                var destTable = r.GetDestTable(sourceTable);
                var destColumn = r.GetColumn(destTable);
                dumpResult += $" --> {destTable.Name}[{destColumn}]";
                sourceTable = destTable;
            }
            dumpResult += $" {(p.Active ? "*ACTIVE*" : "-inactive-")} P:{p.Priority} W:{p.Weight}{(p.Current ? " ***CURRENT***" : "")}\r\n";
            return dumpResult;
        }

        public void DumpPathAllTables()
        {
            string dumpResult = string.Empty;
            if (StudioModel == null)
            {
                Log.Error("StudioModel not available.");
                return;
            }

            foreach (var t in StudioModel.Tables)
            {
                dumpResult += DumpTablePaths(t);
            }

            Log.Verbose("Dump paths:\r\n" + dumpResult);
        }

        public void DumpAmbiguities()
        {
            string dumpResult = string.Empty;
            if (StudioModel == null)
            {
                Log.Error("StudioModel not available.");
                return;
            }

            var relationshipModifiers = ParseRelationshipModifiers(textRelationships.Text).ToList();

            foreach (var p2p in StudioModel.Ambiguities)
            {
                dumpResult += $"{p2p.Key.FromTable.Name} -> {p2p.Key.ToTable.Name} : {p2p.Count()} relationships\r\n";

                var disambiguatedPath = (p2p).Disambiguate(relationshipModifiers);
                var currentPaths =
                    from path in disambiguatedPath
                    where path.Current
                    select path;
                dumpResult += currentPaths.Count() switch
                {
                    0 => "  NO ACTIVE PATHS\r\n",
                    1 => "  SELECTED PATH:\r\n",
                    _ => "  AMBIGUOUS PATHS:\r\n"
                };

                foreach (var path in currentPaths)
                {
                    dumpResult += DumpPath(path);
                }
                dumpResult += "  Other paths:\r\n";

                foreach (var path in disambiguatedPath.Where(p => !p.Current))
                {
                    dumpResult += DumpPath(path);
                }

                // Test to disambiguate
                ;
            }

            Log.Verbose("Dump paths:\r\n" + dumpResult);
        }

        private void BtnBrowse(object sender, EventArgs e)
        {
            openFile.FileName = textFilename.Text;
            if (openFile.ShowDialog() == DialogResult.OK)
            {
                textFilename.Text = openFile.FileName;
                OpenBimFile();
            }
        }

        MyUserSettings settings;

        private void Studio_Load(object sender, EventArgs e)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.RichTextBox(logDisplay, theme: ThemePresets.Light)
            .CreateLogger();

            settings = new MyUserSettings();
            //textFilename.DataBindings.Add(new Binding(nameof(textFilename.Text), settings, nameof(MyUserSettings.BimFilename)));
            //textRelationships.DataBindings.Add(new Binding(nameof(textRelationships.Text), settings, nameof(MyUserSettings.Relationships)));

            if (!string.IsNullOrWhiteSpace(textFilename.Text))
            {
                OpenBimFile();
            }
            settings.Save();
        }

        private void BtnDump_Click(object sender, EventArgs e)
        {
            DumpRelatioships();
            settings.Save();
        }

        private void BtnPaths_Click(object sender, EventArgs e)
        {
            DumpPathAllTables();
            settings.Save();
        }

        private void BtnAmbiguities_Click(object sender, EventArgs e)
        {
            DumpAmbiguities();
            settings.Save();
        }

        [GeneratedRegex(@"(?<=').*?(?=')|.*?(?=\[)")]
        private static partial Regex MatchTableNameRegex();
        [GeneratedRegex(@"(?<=\[).*?(?=\])")]
        private static partial Regex MatchColumnNameRegex();

        private static (string Table, string Column) SplitColumnReference(string columnReference)
            => (Table: MatchTableNameRegex().Match(columnReference).Value.Trim(),
                Column: MatchColumnNameRegex().Match(columnReference).Value.Trim()
            );

        private void BtnRelationships_Click(object sender, EventArgs e)
        {
            ParseRelationshipModifiers(textRelationships.Text).ToList();
            settings.Save();
        }
        public IEnumerable<RelationshipModifier> ParseRelationshipModifiers(string text)
        {
            var lines = text.Replace("\r\n", "\n").Split("\n");

            foreach (var line in lines)
            {
                Relationship? relationship;
                RelationshipModifier.ModifierType modifierType;
                Relationship.CrossFilterDirection? direction = null;

                string trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine)) continue;
                var values = trimmedLine.Split(',');
                if (values.Length < 4)
                {
                    Log.Error($"Line ignores becasue it has less than 4 arguments: {line}");
                    continue;
                }

                (int Level, string Operation, string LeftColumn, string RightColumn, string? CrossFilter) item;
                item = (
                    Level: int.Parse(values[0]),
                    Operation: values[1].ToUpper(),
                    LeftColumn: values[2],
                    RightColumn: values[3],
                    CrossFilter: (values.Length >= 5) ? values[4] : null
                );
                try
                {
                    modifierType = item.Operation switch
                    {
                        "USERELATIONSHIP" => RelationshipModifier.ModifierType.ActiveState,
                        "CROSSFILTER" => RelationshipModifier.ModifierType.CrossFilterDirection,
                        _ => throw new ApplicationException($"Modifier {item.Operation} is not valid.")
                    };

                    var left = SplitColumnReference(item.LeftColumn);
                    var right = SplitColumnReference(item.RightColumn);

                    relationship = StudioModel?.Relationships.FindRelationship(left, right);
                    if (relationship == null)
                    {
                        throw new ApplicationException($"Relationship '{left.Table}'[{left.Column}]-'{right.Table}'[{right.Column}] not found.");
                    }

                    bool invertOneWay = (relationship.To.Name == left.Table);

                    if (item.CrossFilter != null && modifierType == RelationshipModifier.ModifierType.CrossFilterDirection)
                    {
                        direction = item.CrossFilter.ToUpper() switch
                        {
                            "NONE" => Relationship.CrossFilterDirection.None,
                            "ONEWAY" => Relationship.CrossFilterDirection.OneWay,
                            "BOTH" => Relationship.CrossFilterDirection.Both,
                            // TODO check whether it is correct
                            "ONEWAY_RIGHTFILTERSLEFT" => invertOneWay ? Relationship.CrossFilterDirection.OneWay_Inverted : Relationship.CrossFilterDirection.OneWay,
                            "ONEWAY_LEFTFILTERSRIGHT" => invertOneWay ? Relationship.CrossFilterDirection.OneWay : Relationship.CrossFilterDirection.OneWay_Inverted,
                            _ => throw new ApplicationException($"Crossfilter {item.CrossFilter} is not valid.")
                        };
                    }
                }
                catch (ApplicationException ex)
                {
                    Log.Error(ex.Message);
                    Log.Error($"Line skipped: {line}");
                    continue;
                }

                Log.Verbose($"Level={item.Level}, {item.Operation}, {item.LeftColumn}, {item.RightColumn}, {item.CrossFilter}");
                Log.Verbose($"  ({item.Level}) {modifierType}: '{relationship.To.Name}'[{relationship.ToColumn}] -> '{relationship.From.Name}'[{relationship.FromColumn}] {direction}");

#pragma warning disable CS8629 // Nullable value type may be null.
                yield return modifierType switch
                {
                    RelationshipModifier.ModifierType.ActiveState => new RelationshipModifier(relationship, item.Level, true),
                    RelationshipModifier.ModifierType.CrossFilterDirection => new RelationshipModifier(relationship, item.Level, (Relationship.CrossFilterDirection)direction),
                    _ => throw new ApplicationException("ModifierType type not defined")
                };
#pragma warning restore CS8629 // Nullable value type may be null.
            }
        }

        private void Studio_FormClosing(object sender, FormClosingEventArgs e)
        {
            settings.Save();
        }
    }
}