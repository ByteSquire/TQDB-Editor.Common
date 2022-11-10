using Godot;
using Godot.Collections;
using Microsoft.Extensions.Logging;

namespace TQDBEditor.Common
{
    public partial class Config : Node
    {
        //Create new ConfigFile object.
        private ConfigFile config;
        private ILogger logger;
        const string configPath = "user://config.cfg";
        const string setupDialog = "res://Scenes/Popups/ConfigSetup.tscn";
        private ConfirmationDialog setupDialogNode;
        const string newModDialog = "res://Scenes/Popups/CreateNewMod.tscn";

        [Signal]
        public delegate void WorkingDirChangedEventHandler();
        [Signal]
        public delegate void ModNameChangedEventHandler();
        [Signal]
        public delegate void ShowDescriptionToggledEventHandler();
        [Signal]
        public delegate void TrulyReadyEventHandler();

        private string _workingDir;
        public string WorkingDir
        {
            get => _workingDir;
            set
            {
                if (_workingDir.Equals(value))
                    return;

                _workingDir = value;
                EmitSignal(nameof(WorkingDirChanged));
            }
        }
        public string BuildDir { get; set; }
        public string ToolsDir { get; set; }
        public Array<string> AdditionalDirs { get; set; }
        public string ModDir => Path.Combine(ModsDir, modSubDir);
        public string ModsDir => Path.Combine(WorkingDir, "CustomMaps");
        public string ModName
        {
            get => modSubDir;
            set
            {
                if (modSubDir.Equals(value))
                    return;

                modSubDir = value;
                EmitSignal(nameof(ModNameChanged));
            }
        }
        private string modSubDir;


        private bool viewDescriptions;
        public bool ViewDescriptions
        {
            get => viewDescriptions;
            set
            {
                if (viewDescriptions.Equals(value))
                    return;

                viewDescriptions = value;
                EmitSignal(nameof(ShowDescriptionToggled));
            }
        }
        public int NameColumnWidth { get; set; }
        public int ClassColumnWidth { get; set; }
        public int TypeColumnWidth { get; set; }
        public int DefaultValueColumnWidth { get; set; }
        public int DescriptionColumnWidth { get; set; }

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            config = new ConfigFile();
            GetNode<ConsoleLogHandler>("/root/Logging").Config = this;
            logger = this.GetConsoleLogger();

            AdditionalDirs = new Array<string>();

            var documentsFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            var myGamesFolder = Path.Combine(documentsFolder, "My Games");
            var tqITPath = Path.Combine(myGamesFolder, "Titan Quest - Immortal Throne");
            BuildDir = tqITPath;
            ToolsDir = string.Empty;
            _workingDir = Path.Combine(tqITPath, "Working");
            modSubDir = string.Empty;

            LoadConfig();
            ApplyValues();

            var tree = GetTree();
            tree.Root.GuiEmbedSubwindows = false;
            if (!ValidateConfig())
            {
                if (!ResourceLoader.Exists(setupDialog))
                {
                    GD.PushError("Could not find setup dialog " + setupDialog);
                    return;
                }
                setupDialogNode = ResourceLoader.Load<PackedScene>(setupDialog).Instantiate<ConfirmationDialog>();
                setupDialogNode.Cancelled += () => tree.Quit();
                setupDialogNode.Confirmed += SetupConfirmed;
                AddChild(setupDialogNode);
                setupDialogNode.PopupCentered();
            }
            else
                SetupConfirmed();
            //GD.Print(WorkingDir);
            //GD.Print(BuildDir);
            //GD.Print(ToolsDir);
            //GD.Print(AdditionalDirs);
            //GD.Print(ModDir);

            //GD.Print(ViewDescriptions);
            //GD.Print(NameColumnWidth);
            //GD.Print(ClassColumnWidth);
            //GD.Print(TypeColumnWidth);
            //GD.Print(DefaultValueColumnWidth);
            //GD.Print(DescriptionColumnWidth);
        }

        public bool ValidateConfig()
        {
            if (string.IsNullOrEmpty(WorkingDir) || string.IsNullOrEmpty(ToolsDir))
            {
                GD.Print("Invalid config:");
                GD.Print("Working: " + WorkingDir);
                GD.Print("Tools: " + ToolsDir);
                return false;
            }
            if (!Directory.Exists(WorkingDir) || !Directory.Exists(ToolsDir))
                return false;
            return true;
        }

        private void SetupConfirmed()
        {
            if (string.IsNullOrEmpty(modSubDir))
            {
                Directory.CreateDirectory(ModsDir);
                var mods = Directory.EnumerateDirectories(ModsDir, "*", SearchOption.TopDirectoryOnly);
                if (mods.Any())
                {
                    ModName = Path.GetRelativePath(ModsDir, mods.First());
                    EmitSignal(nameof(TrulyReady));
                }
                else
                {
                    if (!ResourceLoader.Exists(newModDialog))
                    {
                        GD.PushError("Could not find new mod dialog " + newModDialog);
                        return;
                    }
                    var newModDialogNode = ResourceLoader.Load<PackedScene>(newModDialog).Instantiate<ConfirmationDialog>();

                    newModDialogNode.Confirmed += () =>
                        {
                            var lineEdit = newModDialogNode.GetNode<LineEdit>("Grid/NewModText");
                            var modName = lineEdit.Text;
                            lineEdit.Clear();

                            Directory.CreateDirectory(Path.Combine(ModsDir, modName));
                            ModName = modName;
                            EmitSignal(nameof(TrulyReady));
                        };
                    AddChild(newModDialogNode);

                    var existingList = newModDialogNode.GetNode<ItemList>("Grid/ExistingMods");
                    var existingMods = Directory.EnumerateDirectories(ModsDir, "*", SearchOption.TopDirectoryOnly);
                    existingList.Clear();
                    foreach (var mod in existingMods)
                        existingList.AddItem(Path.GetFileName(mod), selectable: false);
                    newModDialogNode.PopupCenteredRatio(.5f);
                }
            }
            else
                EmitSignal(nameof(TrulyReady));
        }

        public string GetModPath()
        {
            return ModDir;
        }

        public override void _ExitTree()
        {
            if (!OS.HasFeature("editor"))
                SaveConfig();
            base._ExitTree();
        }

        public void SaveConfig()
        {
            //if (!ValidateConfig())
            //    return;
            ApplyValues();

            //Save it to a file (overwrite if already exists).
            config.Save(configPath);
        }

        public void ApplyValues()
        {
            // Set directory values
            config.SetValue("Directories", "workingDir", WorkingDir);
            config.SetValue("Directories", "buildDir", BuildDir);
            config.SetValue("Directories", "toolsDir", ToolsDir);
            config.SetValue("Directories", "additionalDirs", AdditionalDirs/*string.Join(",", AdditionalDirs)*/);
            config.SetValue("Directories", "modDir", modSubDir);

            //Set editor values
            config.SetValue("Editor", "viewDescriptions", ViewDescriptions);
            config.SetValue("Editor", "nameColumnWidth", NameColumnWidth);
            config.SetValue("Editor", "classColumnWidth", ClassColumnWidth);
            config.SetValue("Editor", "typeColumnWidth", TypeColumnWidth);
            config.SetValue("Editor", "defaultValueColumnWidth", DefaultValueColumnWidth);
            config.SetValue("Editor", "descriptionColumnWidth", DescriptionColumnWidth);
        }

        public void LoadConfig()
        {
            var dirSection = "Directories";
            var editorSection = "Editor";
            if (config.Load(configPath) != Error.Ok)
            {
                LoadArtManagerOptions();
                return;
            }

            // Load directory values
            _workingDir = (string)config.GetValue(dirSection, "workingDir", WorkingDir);
            BuildDir = (string)config.GetValue(dirSection, "buildDir", BuildDir);
            ToolsDir = (string)config.GetValue(dirSection, "toolsDir", ToolsDir);
            AdditionalDirs = new Array<string>(config.GetValue(dirSection, "additionalbuildDirs", AdditionalDirs).AsStringArray());
            modSubDir = (string)config.GetValue(dirSection, "modDir", modSubDir);

            // Load editor values
            ViewDescriptions = (bool)config.GetValue(editorSection, "viewDescriptions", true);
            NameColumnWidth = (int)config.GetValue(editorSection, "nameColumnWidth", -1);
            ClassColumnWidth = (int)config.GetValue(editorSection, "classColumnWidth", -1);
            TypeColumnWidth = (int)config.GetValue(editorSection, "typeColumnWidth", -1);
            DefaultValueColumnWidth = (int)config.GetValue(editorSection, "defaultValueColumnWidth", -1);
            DescriptionColumnWidth = (int)config.GetValue(editorSection, "descriptionColumnWidth", -1);

            logger?.LogInformation("Loaded editor config");
        }

        private void LoadArtManagerOptions()
        {
            var documentsFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            var myGamesFolder = Path.Combine(documentsFolder, "My Games");
            var tqToolsConfig = Path.Combine(myGamesFolder, "Titan Quest - Immortal Throne", "Tools.ini");
            if (!File.Exists(tqToolsConfig))
            {
                tqToolsConfig = Path.Combine(myGamesFolder, "Titan Quest", "Tools.ini");
                if (!File.Exists(tqToolsConfig))
                    return; // maybe continue fallback chain, depending on where else the Tools.ini can be
            }

            var lines = File.ReadAllLines(tqToolsConfig);

            var currentSection = "[None]";
            foreach (var line in lines)
            {
                if (line.StartsWith('['))
                {
                    currentSection = line;
                    continue;
                }
                // Load directory values
                if (currentSection == "[Login]")
                {
                    var split = line.Split('=', 2);
                    var key = split[0];
                    var value = split[1];

                    switch (key)
                    {
                        case "localdir":
                            _workingDir = value;
                            break;
                        case "builddir":
                            BuildDir = value;
                            break;
                        case "toolsdir":
                            ToolsDir = value;
                            break;
                        case "additionalbuilddirs":
                            foreach (var additionalDir in value.Split(","))
                                AdditionalDirs.Add(additionalDir);
                            break;
                        case "moddir":
                            modSubDir = value;
                            break;
                        default:
                            break;
                    }
                    continue;
                }
                // Load editor values
                if (currentSection == "[Database]")
                {
                    var split = line.Split('=', 2);
                    var key = split[0];
                    var value = split[1];

                    switch (key)
                    {
                        case "viewDescriptions":
                            ViewDescriptions = int.Parse(value) != 0;
                            break;
                        case "nameColumnWidth":
                            NameColumnWidth = int.Parse(value);
                            break;
                        case "classColumnWidth":
                            ClassColumnWidth = int.Parse(value);
                            break;
                        case "typeColumnWidth":
                            TypeColumnWidth = int.Parse(value);
                            break;
                        case "defaultValueColumnWidth":
                            DefaultValueColumnWidth = int.Parse(value);
                            break;
                        case "descriptionColumnWidth":
                            DescriptionColumnWidth = int.Parse(value);
                            break;
                        default:
                            break;
                    }
                    continue;
                }
            }
            logger?.LogInformation("Loaded ArtManager options from: [i]Documents[/i]{separator}{ArtManager-ToolsPath}",
                Path.DirectorySeparatorChar, Path.GetRelativePath(documentsFolder, tqToolsConfig));
        }
    }
}