using Serilog;
using Serilog.Sinks.RichTextBoxForms.Themes;
using System.Xml.Xsl;
using Microsoft.AnalysisServices.Tabular;
using System.Diagnostics;
using System.Security.Policy;
using System.Data.Common;
using System.Text.RegularExpressions;
using RelationshipsStudio.Tools;
using System.Windows.Forms;
using Microsoft.AnalysisServices.AdomdClient;
using System.Data;

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
        string ModelConnectionString => $"Provider=MSOLAP;Initial Catalog={TabularDatabase?.Name};{TabularDatabase?.Server.ConnectionString}";

        private void OpenLocalModel(PowerBIInstance localModel)
        {
            TabularDatabase = null;
            try
            {
                string serverName = $"localhost:{localModel.Port}";
                using var server = new Server();
                Log.Verbose("Connecting to {sergerName}");
                server.Connect($"Data Source={serverName};");
                TabularDatabase = server.Databases[0];
                PopulateModel();
                DumpRelatioships();
                Log.Verbose("Model updated");
            }
            catch (Exception ex)
            {
                Log.Error($"OpenLocalModel error: {ex.Message}");
            }
        }

        private void OpenBimFile()
        {
            TabularDatabase = null;
            var json = File.ReadAllText(settings.SelectedModel);
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
            Log.Information("Dump relationships");

            string dumpResult = string.Empty;
            if (StudioModel == null)
            {
                Log.Error("StudioModel not available.");
                return;
            }

            dumpResult += "{bold}TABLES{!bold}\r\n";
            foreach (var t in StudioModel.Tables)
            {
                dumpResult += $"  {t.Name}\r\n";
            }
            dumpResult += "{bold}RELATIONSHIPS{!bold}\r\n";
            foreach (var r in StudioModel.Relationships)
            {
                string relationshipDirection = r.GetSymbol(r.From);
                //(r.CrossFilter == Relationship.CrossFilterDirection.None)
                //    ? "---"
                //    : $"{(r.CrossFilter == Relationship.CrossFilterDirection.OneWay ? "-" : "<")}-{(r.CrossFilter == Relationship.CrossFilterDirection.OneWay_Inverted ? "-" : ">")}";
                dumpResult += $"  {r.To.Name} {relationshipDirection} {r.From.Name}\r\n";
            }
            textResult.WriteRichText("{bold}{ul}Dump relationships{reset}\r\n" + dumpResult);
        }

        private static string DumpTablePaths(Table table)
        {
            string dumpResult = string.Empty;
            dumpResult += table.SourcePaths.Any() ? $"{{bold}}TABLES{{!bold}} {table.Name}\r\n" : string.Empty;
            foreach (var p in table.SourcePaths)
            {
                dumpResult = DumpPath(p, false, showPathName: true, showState: true);
            }
            return dumpResult;
        }

        private static string DumpPath(Path p, bool ambiguous, bool showPathName = false, bool showState = false)
        {
            string dumpResult = showPathName ? $"  {p.From.Name} --> {p.To.Name}\r\n" : string.Empty;
            var firstRelationship = p.Relationships.First();
            var sourceTable = p.From;
            var sourceColumn = firstRelationship.GetColumn(sourceTable);
            dumpResult += $"    {sourceTable.Name}[{sourceColumn}]";
            foreach (var r in p.Relationships)
            {
                var destTable = r.GetDestTable(sourceTable);
                var destColumn = r.GetColumn(destTable);
                dumpResult += $" {
                    Relationship.GetSymbol(r.GetCardinality(sourceTable))}-{
                    Relationship.GetSymbol(r.CrossFilter,r.InvertSourceDest(destTable))}-{
                    Relationship.GetSymbol(r.GetCardinality(destTable))} {
                    destTable.Name}[{destColumn}]";
                sourceTable = destTable;
            }
            if (showState)
            {
                dumpResult += $" {(p.Active ? "{bold}{blue}ACTIVE{reset}" : "{red}inactive{reset}")} P:{p.Priority} W:{p.Weight} D:{p.Depth}{(p.Current ? $" {{!{(ambiguous ? "yellow" : "palegreen")}}}{{{(ambiguous ? "red" : "fg")}}}{{bold}}CURRENT{{reset}}" : "")}\r\n";
            }
            return dumpResult;
        }

        public void DumpPathAllTables()
        {
            Log.Information("Dump paths");

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

            textResult.WriteRichText("{bold}{ul}Dump paths{reset}\r\n" + dumpResult);
        }

        private string DumpPaths(bool onlyAmbiguities)
        {
            string dumpResult = string.Empty;
            if (StudioModel == null)
            {
                Log.Error("StudioModel not available.");
                return dumpResult;
            }

            foreach (var (lines, groupName) in GroupRelationshipsModifiers(settings.Relationships))
            {
                dumpResult += $"{{bold}}{{maroon}}*** RELATIONSHIP GROUP {groupName} ***{{reset}}\r\n";
                var relationshipModifiers = ParseRelationshipModifiers(lines).ToList();

                foreach (var p2p in StudioModel.Ambiguities)
                {
                    string groupResult = $"{p2p.Key.FromTable.Name} -> {p2p.Key.ToTable.Name} : {p2p.Count()} relationships\r\n";

                    var disambiguatedPath = (p2p).Disambiguate(relationshipModifiers).Where(p => p.Active || !onlyAmbiguities);
                    var currentPaths =
                        from path in disambiguatedPath
                        where path.Current
                        select path;
                    groupResult += currentPaths.Count() switch
                    {
                        0 => "  {!lightgray}{bold}NO ACTIVE PATHS{reset}\r\n",
                        1 => "  {!powderblue}{bold}SELECTED PATH{reset}:\r\n",
                        _ => "  {!lightyellow}{red}{bold}AMBIGUOUS PATHS{reset}:\r\n"
                    };

                    foreach (var path in currentPaths)
                    {
                        groupResult += DumpPath(path, currentPaths.Count() > 1, showState: true);
                    }
                    groupResult += "  {!whitesmoke}{bold}Other paths{reset}:\r\n";

                    foreach (var path in disambiguatedPath.Where(p => !p.Current))
                    {
                        groupResult += DumpPath(path, false, showState: true);
                    }

                    if (!onlyAmbiguities || disambiguatedPath.Count() > 1)
                    {
                        dumpResult += groupResult;
                    }
                }
            }

            return dumpResult;
        }

        private bool CompareResultSets(AdomdDataReader reader1, AdomdDataReader reader2)
        {
            if (reader1.FieldCount != reader2.FieldCount) { return false; }
            while (reader1.Read() && reader2.Read())
            {
                for (int i = 0; i < reader1.FieldCount; i++)
                {
                    if (!reader1[i].Equals(reader2[i]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private bool ValidatePath(Path path, AdomdConnection connection, out string validateQueries, IEnumerable<RelationshipModifier>? useRelationships = null)
        {
            Debug.Assert(StudioModel != null);
            Debug.Assert(TabularDatabase != null);

            bool validationResult = false;
            validateQueries = $@"// ORIGINAL PATH
// {DumpPath(path, false)}
{StudioModel.DaxValidatePath(path, useRelationships)}

// SIMULATE PATH
// {DumpPath(path,false)}
{StudioModel.DaxSimulatePath(path, useRelationships)}";

            try
            {
                using AdomdCommand command = connection.CreateCommand();
                command.CommandType = System.Data.CommandType.Text;
                command.CommandText = validateQueries;
                using AdomdDataReader reader = command.ExecuteReader();

                var originalResult = reader.Cast<IDataRecord>().ToList();
                if (!reader.NextResult())
                {
                    Log.Error("Missing simulation table");
                    return false;
                }
                var simulationResult = reader.Cast<IDataRecord>().ToList();
                validationResult = TableComparison.CompareDataRecords(originalResult, simulationResult);
                Log.Write(
                    validationResult ? Serilog.Events.LogEventLevel.Verbose : Serilog.Events.LogEventLevel.Warning,
                    $"{(validationResult ? "Validated" : "Different content found in")} path {path.From.Name} --> {path.To.Name}"
                );
            }
            catch(Exception ex)
            {
                Log.Error(ex.ToString() );
                Log.Error($"Failed query:\r\n{validateQueries}");
                validationResult = false;
            }
            return validationResult;
        }

        private string ValidationQueries(AdomdConnection connection)
        {
            string dumpResult = string.Empty;
            if (StudioModel == null)
            {
                Log.Error("StudioModel not available.");
                return dumpResult;
            }

            foreach (var (lines, groupName) in GroupRelationshipsModifiers(settings.Relationships))
            {
                dumpResult += $"{{bold}}{{maroon}}*** RELATIONSHIP GROUP {groupName} ***{{reset}}\r\n";
                var relationshipModifiers = ParseRelationshipModifiers(lines).ToList();

                foreach (var p2p in StudioModel.Ambiguities)
                {
                    string groupResult = $"PATH {p2p.Key.FromTable.Name} -> {p2p.Key.ToTable.Name}\r\n";

                    var disambiguatedPath = (p2p).Disambiguate(relationshipModifiers).Where(p => p.Active);
                    var currentPaths =
                        from path in disambiguatedPath
                        where path.Current
                        select path;
                    if (currentPaths.Count() != 1) continue;

                    if (!ValidatePath(currentPaths.Single(), connection, out string validateQueries, relationshipModifiers))
                    {
                        dumpResult += groupResult + "\r" + validateQueries;
                    }
                    
                }
            }
            return dumpResult;
        }

        public void DumpPathSelection()
        {
            Log.Information("Dump complete path selection");
            string dumpResult = DumpPaths(false);
            textResult.WriteRichText("{bold}{ul}Dump complete path selection{reset}\r\n" + dumpResult);
        }

        public void DumpAmbiguities()
        {
            Log.Information("Dump ambiguities");
            string dumpResult = DumpPaths(true);
            textResult.WriteRichText("{bold}{ul}Dump ambiguities{reset}\r\n" + dumpResult);
        }

        private void BtnBrowse(object sender, EventArgs e)
        {
            openFile.FileName = settings.SelectedModel;
            if (openFile.ShowDialog() == DialogResult.OK)
            {
                settings.SelectedModel = openFile.FileName;
                OpenBimFile();
                settings.Save();
            }
        }

        MyUserSettings settings = new();

        private void Studio_Load(object sender, EventArgs e)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.RichTextBox(logDisplay, theme: ThemePresets.Light)
            .CreateLogger();

            settings = new MyUserSettings();
            textRelationships.DataBindings.Add(new Binding(nameof(textRelationships.Text), settings, nameof(MyUserSettings.Relationships)));
            CboLocalModels.DataBindings.Add(new Binding(nameof(CboLocalModels.Text), settings, nameof(MyUserSettings.SelectedModel)));

            var localModels = RefreshComboLocalModels();

            if (!string.IsNullOrWhiteSpace(settings.SelectedModel) && System.IO.Path.Exists(settings.SelectedModel))
            {
                OpenBimFile();
            }
            else
            {
                var localModel = localModels.FirstOrDefault(m => m.Name == settings.SelectedModel);
                if (localModel != null)
                {
                    Log.Information($"Opening local model: {localModel.Name}");
                    OpenLocalModel(localModel);
                }
                else if (!string.IsNullOrEmpty(settings.SelectedModel))
                {
                    Log.Warning($"Local model not found: {settings.SelectedModel}");
                    settings.SelectedModel = string.Empty;
                }
            }
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

        private void BtnPathSelection_Click(object sender, EventArgs e)
        {
            DumpPathSelection();
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
            ParseGroupRelationshipModifiers(settings.Relationships);
            settings.Save();
        }

        static public IEnumerable<(IEnumerable<string> lines, string groupName)> GroupRelationshipsModifiers(string text)
        {
            var lines = text.Replace("\r\n", "\n").Split("\n");
            string groupName = string.Empty;

            var currentGroup = new List<string>();
            foreach (var rawline in lines)
            {
                string line = rawline.Trim();
                // Skip comments
                if (line.StartsWith("//")) continue;

                // Intercept new group
                if (line.StartsWith("#"))
                {
                    groupName = line[1..];

                    if (currentGroup.Count > 0)
                    {
                        yield return (currentGroup, groupName);
                        currentGroup = new List<string>();
                    }
                }
                else
                {
                    currentGroup.Add(line);
                }
            }
            if (currentGroup.Count > 0)
            {
                yield return (currentGroup, groupName);
            }
        }

        public void DumpRelationshipModifiersGroups(IEnumerable<(IEnumerable<string> lines, string groupName)> modifiers)
        {
            textResult.WriteRichText($"{{bold}}{{ul}}Relationship modifiers{{reset}}\r\n", append: false);
            foreach (var (lines, groupName) in modifiers)
            {
                textResult.WriteRichText($"{{bold}}GROUP {groupName}{{reset}}\r\n", append: true);
                foreach (var modifier in ParseRelationshipModifiers(lines))
                {
                    textResult.WriteRichText($"  ({modifier.Level}) {modifier.Type}: '{modifier.Relationship.To.Name}'[{modifier.Relationship.ToColumn}] -> '{modifier.Relationship.From.Name}'[{modifier.Relationship.FromColumn}] {modifier.Direction}\r\n", append: true);
                }

            }
        }

        public void ParseGroupRelationshipModifiers(string text)
        {
            DumpRelationshipModifiersGroups(GroupRelationshipsModifiers(text));
        }

        public IEnumerable<RelationshipModifier> ParseRelationshipModifiers(IEnumerable<string> lines)
        {
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
                    Log.Error($"Line ignores because it has less than 4 arguments: {line}");
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

        private void BtnAddSyntaxExample_Click(object sender, EventArgs e)
        {
            settings.Relationships += @"
// The syntax is:
// <level>,<USERELATIONSHIP|CROSSFILTER>,<columnReferenceLeft>,<columnReferenceRight>[,NONE|ONEWAY|BOTH|ONEWAY_RIGHTFILTERSLEFT|ONEWAY_LEFTFILTERSRIGHT]
// Use # at mark the start of a group - the following part of the line becomes the new group name
//
// Examples:

// Apply USERELATIONSHIP in the outer CALCULATE, Apply CROSSFILTER in the inner CALCULATE
# Demo 1
1,USERELATIONSHIP,B2[Name],B1[Name]
2,CROSSFILTER,B2[Name],B1[Name],NONE

// Apply two USERELATIONSHIP in the same CALCULATE
# Demo 2
1,USERELATIONSHIP,C[Name],A[Name]
1,USERELATIONSHIP,C[Name],B1[Name]

// Apply USERELATIONSHIP for C-A in the outer CALCULATE, apply USERELATIONSHIP for C-B1 in the inner CALCULATE
# Demo 3
1,USERELATIONSHIP,C[Name],A[Name]
2,USERELATIONSHIP,C[Name],B1[Name]
";
        }

        private void BtnValidateSelection_Click(object sender, EventArgs e)
        {
            textResult.WriteRichText($"{{bold}}{{ul}}Relationship modifiers{{reset}}\r\n", append: false);
            using AdomdConnection connection = new(ModelConnectionString);
            connection.Open();
            textResult.WriteRichText(ValidationQueries(connection));
        }

        private void BtnRefreshLocalInstancesList_Click(object sender, EventArgs e)
        {
            RefreshComboLocalModels();
        }

        private IEnumerable<PowerBIInstance> RefreshComboLocalModels()
        {
            var localInstances = PowerBIHelper.GetLocalInstances(false);
            CboLocalModels.Items.AddRange(localInstances.ToArray());
            return localInstances;
        }

        private void CboLocalModels_SelectedIndexChanged(object sender, EventArgs e)
        {
            Log.Information($"Selected index: {CboLocalModels.SelectedItem}");
            if (CboLocalModels.SelectedItem is PowerBIInstance selectedModel)
            {
                OpenLocalModel(selectedModel);
            }
        }

    }
}