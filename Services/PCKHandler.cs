//using Microsoft.Extensions.Logging;
//using Prism.Modularity;
//using System.Reflection;
//using System.Runtime.Loader;
//using System.Text.Json;
//using System.Text.Json.Nodes;
//using TQDB_Parser;

//namespace TQDB_Editor.Common.Services
//{
//    public partial class ModuleHandler
//    {
//        private Dictionary<string, List<IModule>> registeredFileEditors;
//        private Dictionary<string, List<IModule>> registeredViews;
//        private Dictionary<(string vName, string vClass, string vType), List<IModule>> registeredEntryEditors;
//        private Dictionary<(string vName, string vClass, string vType), List<IModule>> registeredValueControls;
//        //private Dictionary<string, Assembly> editorAssemblies;

//        private ILogger logger;
//        private ConfigService config;

//        public ModuleHandler(ConfigService config, ILogger logger)
//        {
//            registeredFileEditors = new();
//            registeredViews = new();
//            registeredEntryEditors = new();
//            registeredValueControls = new();
//            //editorAssemblies = new();

//            this.logger = logger;
//            this.config = config;

//            ReadPcks();
//        }

//        private Assembly Resolve(object sender, ResolveEventArgs args)
//        {
//            try
//            {
//                GD.Print(args.RequestingAssembly + " requested " + args.Name);
//                var resolved = AssemblyLoadContext.Default.Assemblies.Single(x => x.FullName == args.Name);
//                //var resolved = AppDomain.CurrentDomain.GetAssemblies().Single(x => x.FullName == args.Name);
//                //if (resolved.Location == string.Empty)
//                //resolved = AssemblyLoadContext.Default.LoadFromAssemblyName(resolved.GetName());
//                GD.Print("Resolved " + args.Name);
//                return resolved;
//            }
//            catch (InvalidOperationException)
//            {
//                logger?.LogError("Assembly dependency {dep} could not be found", args.Name);
//            }
//            return null;
//        }

//        private void ReadPcks()
//        {
//            //var editors = Directory.EnumerateDirectories("editors", "*", SearchOption.TopDirectoryOnly);

//            //foreach (var fileEditor in editors)
//            //{
//            //    var files = Directory.EnumerateFileSystemEntries(fileEditor);
//            //    try
//            //    {
//            //        var winDir = Path.GetFullPath(files.Single(x => x.EndsWith("win-x64")));
//            //        var dllFiles = Directory.EnumerateFiles(winDir);
//            //        var fileEditorAssembly = dllFiles.Single(x => x.EndsWith(".deps.json"))[..^10] + ".dll";

//            //        //AppDomain.CurrentDomain.AssemblyResolve += Resolve;
//            //        //AppDomain.CurrentDomain.TypeResolve += Resolve;
//            //        var assm = new PluginLoadContext(winDir).LoadFromAssemblyPath(fileEditorAssembly);
//            //        //Assembly.Load(File.ReadAllBytes(fileEditorDll));

//            //        editorAssemblies.Add(Path.GetFileName(fileEditor), assm);
//            //    }
//            //    catch (InvalidOperationException)
//            //    {
//            //        logger?.LogError("Failed to load editor: {pack}, must contain the exported win-x64 directory", fileEditor);
//            //        continue;
//            //    }
//            //    try
//            //    {
//            //        var fileEditorPck = files.Single(x => x.EndsWith(".pck"));
//            //        var success = ProjectSettings.LoadResourcePack(fileEditorPck);
//            //        if (!success)
//            //            logger?.LogError("Failed to load editor pack: {pack}", fileEditorPck);
//            //    }
//            //    catch (InvalidOperationException)
//            //    {
//            //        logger?.LogError("Failed to load editor: {pack}, must contain exactly one pck file", fileEditor);
//            //        continue;
//            //    }
//            //}

//            var editorsPath = "res://Editors";
//            using var editorsDa = DirAccess.Open(editorsPath);
//            if (editorsDa != null)
//            {
//                var resEditors = editorsDa.GetDirectories();
//                foreach (var resEditor in resEditors)
//                {
//                    using var da = DirAccess.Open(PathExtensions.CombineGodotPath(editorsPath, resEditor));
//                    //if (!editorAssemblies.TryGetValue(resEditor, out var editorAssembly))
//                    //{
//                    //    GD.PrintErr("Could not find assembly for: " + resEditor);
//                    //    continue;
//                    //}
//                    if (!da.FileExists("info.json"))
//                    {
//                        logger?.LogError("Editor: {editor} is missing an info.json",
//                            PathExtensions.CombineGodotPath(editorsPath, resEditor));
//                        continue;
//                    }
//                    using var infoFile = Godot.FileAccess.Open(PathExtensions.CombineGodotPath(editorsPath, resEditor, "info.json"),
//                        Godot.FileAccess.ModeFlags.Read);
//                    //if (!editorAssemblies.TryGetValue(resEditor, out var editorAssembly))
//                    //{
//                    //    continue;
//                    //}
//                    try
//                    {
//                        var node = JsonNode.Parse(infoFile.GetAsText());

//                        var hasFEditors = node.AsObject().TryGetPropertyValue("fileEditors", out var fileEditorsNode);
//                        var hasVEditors = node.AsObject().TryGetPropertyValue("variableEditors", out var variableEditorsNode);
//                        var hasVControls = node.AsObject().TryGetPropertyValue("variableControls", out var variableControlsNode);

//                        if (!(hasFEditors || hasVEditors || hasVControls))
//                        {
//                            logger?.LogError("File {file}: Invalid root object", infoFile);
//                            continue;
//                        }

//                        if (hasFEditors)
//                            ParseFEditor(fileEditorsNode, infoFile.GetPath(), resEditor);
//                        if (hasVEditors)
//                            ParseVEditor(variableEditorsNode, infoFile.GetPath(), resEditor);
//                        if (hasVControls)
//                            ParseVControl(variableControlsNode, infoFile.GetPath(), resEditor);
//                    }
//                    catch (JsonException e)
//                    {
//                        logger?.LogError(e, "File: {file}", infoFile);
//                    }
//                    catch (InvalidOperationException e)
//                    {
//                        logger?.LogError(e, "File: {file}", infoFile);
//                    }
//                }
//            }
//            editorsDa.Dispose();

//            var viewsPath = "res://Views";
//            using var viewsDA = DirAccess.Open(viewsPath);
//            if (viewsDA != null)
//            {
//                var resViews = viewsDA.GetDirectories();
//                foreach (var resView in resViews)
//                {
//                    using var da = DirAccess.Open(PathExtensions.CombineGodotPath(viewsPath, resView));
//                    //if (!editorAssemblies.TryGetValue(resView, out var editorAssembly))
//                    //{
//                    //    GD.PrintErr("Could not find assembly for: " + resView);
//                    //    continue;
//                    //}
//                    if (!da.FileExists("info.json"))
//                    {
//                        logger?.LogError("View: {view} is missing an info.json",
//                            PathExtensions.CombineGodotPath(viewsPath, resView));
//                        continue;
//                    }
//                    using var infoFile = Godot.FileAccess.Open(PathExtensions.CombineGodotPath(viewsPath, resView, "info.json"),
//                        Godot.FileAccess.ModeFlags.Read);
//                    //if (!editorAssemblies.TryGetValue(resEditor, out var editorAssembly))
//                    //{
//                    //    continue;
//                    //}
//                    try
//                    {
//                        var node = JsonNode.Parse(infoFile.GetAsText());

//                        var views = node.AsArray();

//                        registeredViews.Add(resView, new List<PackedScene>());
//                        foreach (var view in views)
//                        {
//                            var scenePath = PathExtensions.CombineGodotPath(viewsPath, resView, view + ".tscn");
//                            if (!ResourceLoader.Exists(scenePath))
//                            {
//                                logger?.LogError("View {view} referenced in {info} not found", scenePath, infoFile);
//                                continue;
//                            }
//                            registeredViews[resView].Add(LoadPatched(scenePath/*, editorAssembly*/));
//                        }
//                    }
//                    catch (JsonException e)
//                    {
//                        logger?.LogError(e, "File: {file}", infoFile);
//                    }
//                    catch (InvalidOperationException e)
//                    {
//                        logger?.LogError(e, "File: {file}", infoFile);
//                    }
//                }
//            }
//        }

//        private void ParseFEditor(JsonNode node, string infoFile, string resEditor)
//        {
//            try
//            {
//                foreach (var infoObj in node.AsArray().Select(x => x.AsObject()))
//                {
//                    var nodeIndex = node.AsArray().IndexOf(infoObj);
//                    if (!infoObj.TryGetPropertyValue("name", out var nameNode))
//                    {
//                        logger?.LogError("File {file}: The editor {index} must have a name", infoFile, nodeIndex);
//                        continue;
//                    }
//                    var scenePath = "res://Editors/" + resEditor + '/' + nameNode + ".tscn";
//                    if (!ResourceLoader.Exists(scenePath))
//                    {
//                        logger?.LogError("Editor {editor} referenced in {info} not found", scenePath, infoFile);
//                        continue;
//                    }
//                    if (!infoObj.TryGetPropertyValue("templateName", out var templateNode))
//                    {
//                        //logger?.LogError("File {file}: Editor {index}, missing templateName property", infoFile);
//                        RegisterFileEditor(LoadPatched(scenePath/*, editorAssembly*/));
//                        continue;
//                    }
//                    RegisterFileEditor(LoadPatched(scenePath/*, editorAssembly*/), (string)templateNode);
//                }
//            }
//            catch (JsonException e)
//            {
//                logger?.LogError(e, "File: {file}", infoFile);
//            }
//            catch (InvalidOperationException e)
//            {
//                logger?.LogError(e, "File: {file}", infoFile);
//            }
//        }

//        private void ParseVEditor(JsonNode node, string infoFile, string resEditor)
//        {
//            try
//            {
//                foreach (var infoObj in node.AsArray().Select(x => x.AsObject()))
//                {
//                    var nodeIndex = node.AsArray().IndexOf(infoObj);
//                    if (!infoObj.TryGetPropertyValue("name", out var nameNode))
//                    {
//                        logger?.LogError("File {file}: Editor {index} missing name property", infoFile, nodeIndex);
//                        continue;
//                    }
//                    var scenePath = "res://Editors/" + resEditor + '/' + nameNode + ".tscn";
//                    if (!ResourceLoader.Exists(scenePath))
//                    {
//                        logger?.LogError("Editor {editor} referenced in {info} not found", scenePath, infoFile);
//                        continue;
//                    }
//                    if (!infoObj.TryGetPropertyValue("variable", out var variableNode))
//                    {
//                        //logger?.LogError("File {file}: Editor {index}, missing variable property", infoFile, nodeIndex);
//                        RegisterEntryEditor(LoadPatched(scenePath/*, editorAssembly*/));
//                        continue;
//                    }
//                    try
//                    {
//                        var variableObj = variableNode.AsObject();

//                        string vName = null;
//                        string vClass = null;
//                        string vType = null;
//                        if (variableObj.TryGetPropertyValue("name", out var vNameNode))
//                        {
//                            vName = vNameNode.ToString();
//                            //logger?.LogError("File {file}: Editor {index}, missing variable name property", infoFile, nodeIndex);
//                            //continue;
//                        }
//                        if (variableObj.TryGetPropertyValue("class", out var vClassNode))
//                        {
//                            vClass = vClassNode.ToString();
//                            //logger?.LogError("File {file}: Editor {index}, missing variable class property", infoFile, nodeIndex);
//                            //continue;
//                        }
//                        if (variableObj.TryGetPropertyValue("type", out var vTypeNode))
//                        {
//                            vType = vTypeNode.ToString();
//                            //logger?.LogError("File {file}: Editor {index}, missing variable type property", infoFile, nodeIndex);
//                            //continue;
//                        }
//                        RegisterEntryEditor(LoadPatched(scenePath/*, editorAssembly*/), vName, vClass, vType);
//                    }
//                    catch (InvalidOperationException)
//                    {
//                        logger?.LogError("File {file}: Editor {index}, variable property must be an object", infoFile, nodeIndex);
//                    }
//                }
//            }
//            catch (JsonException e)
//            {
//                logger?.LogError(e, "File: {file}", infoFile);
//            }
//            catch (InvalidOperationException e)
//            {
//                logger?.LogError(e, "File: {file}", infoFile);
//            }
//        }

//        private void ParseVControl(JsonNode node, string infoFile, string resEditor)
//        {
//            try
//            {
//                foreach (var infoObj in node.AsArray().Select(x => x.AsObject()))
//                {
//                    var nodeIndex = node.AsArray().IndexOf(infoObj);
//                    if (!infoObj.TryGetPropertyValue("name", out var nameNode))
//                    {
//                        logger?.LogError("File {file}: Control {index} missing name property", infoFile, nodeIndex);
//                        continue;
//                    }
//                    var scenePath = "res://Editors/" + resEditor + '/' + nameNode + ".tscn";
//                    if (!ResourceLoader.Exists(scenePath))
//                    {
//                        logger?.LogError("Editor {editor} referenced in {info} not found", scenePath, infoFile);
//                        continue;
//                    }
//                    if (!infoObj.TryGetPropertyValue("variable", out var variableNode))
//                    {
//                        //logger?.LogError("File {file}: Editor {index}, missing variable property", infoFile, nodeIndex);
//                        RegisterEntryControl(LoadPatched(scenePath/*, editorAssembly*/));
//                        continue;
//                    }
//                    try
//                    {
//                        var variableObj = variableNode.AsObject();

//                        string vName = null;
//                        string vClass = null;
//                        string vType = null;
//                        if (variableObj.TryGetPropertyValue("name", out var vNameNode))
//                        {
//                            vName = vNameNode.ToString();
//                            //logger?.LogError("File {file}: Editor {index}, missing variable name property", infoFile, nodeIndex);
//                            //continue;
//                        }
//                        if (variableObj.TryGetPropertyValue("class", out var vClassNode))
//                        {
//                            vClass = vClassNode.ToString();
//                            //logger?.LogError("File {file}: Editor {index}, missing variable class property", infoFile, nodeIndex);
//                            //continue;
//                        }
//                        if (variableObj.TryGetPropertyValue("type", out var vTypeNode))
//                        {
//                            vType = vTypeNode.ToString();
//                            //logger?.LogError("File {file}: Editor {index}, missing variable type property", infoFile, nodeIndex);
//                            //continue;
//                        }
//                        RegisterEntryControl(LoadPatched(scenePath/*, editorAssembly*/), vName, vClass, vType);
//                    }
//                    catch (InvalidOperationException)
//                    {
//                        logger?.LogError("File {file}: Control {index}, variable property must be an object", infoFile, nodeIndex);
//                    }
//                }
//            }
//            catch (JsonException e)
//            {
//                logger?.LogError(e, "File: {file}", infoFile);
//            }
//            catch (InvalidOperationException e)
//            {
//                logger?.LogError(e, "File: {file}", infoFile);
//            }
//        }

//        //Loads a PackedScene but replaces the CScript with an Assembly instantiated version
//        public static PackedScene LoadPatched(string scenePath/*, Assembly assembly*/)
//        {
//            var x = ResourceLoader.Load<PackedScene>(scenePath);

//            //var bundled = x.Get("_bundled").AsGodotDictionary();

//            //var vars = bundled["variants"].AsGodotArray();

//            //var myAssembly = Assembly.GetExecutingAssembly();
//            //GD.Print(string.Join("\n", myAssembly.ExportedTypes.Select(x => x.Name)));
//            //GD.Print(string.Join("\n", assembly.ExportedTypes.Select(x => x.Name)));

//            //for (var i = 0; i < vars.Count; i++)
//            //{
//            //    var o = vars[i];

//            //    if (o.AsGodotObject() is CSharpScript c)
//            //    {
//            //        try
//            //        {
//            //            //Try to find the path of the script by the namespace and the resource itself
//            //            var fileName = c.ResourcePath.Split('/')[^1].Replace(".cs", "");
//            //            GD.Print("Resolving type of file " + fileName);

//            //            string nameSpace = c.SourceCode.Split("\n").FirstOrDefault(x => x.StartsWith("namespace"), string.Empty).Split(' ')[1];

//            //            var typeName = nameSpace + '.' + fileName;

//            //            GD.Print("Resolving type " + typeName);

//            //            var thingType = assembly.GetType(typeName);
//            //            thingType ??= myAssembly.GetType(typeName);

//            //            if (thingType is null)
//            //            {
//            //                GD.PrintErr("Could not resolve type " + typeName + " in file " + fileName);
//            //                continue;
//            //            }
//            //            GD.Print("Resolved type " + thingType.ToString());

//            //            var test = c.New();
//            //            //Attempt to instantiate that script, assuming it is a Node so we can get the script
//            //            var thing = thingType.GetConstructor(Type.EmptyTypes)?.Invoke(Array.Empty<object>()) as Node;

//            //            GD.Print(thing);
//            //            //Change the script of the packed scene
//            //            vars[i] = thing.GetScript();
//            //            thing.Free();
//            //        }
//            //        catch (Exception)
//            //        {
//            //            continue;
//            //        }
//            //    }
//            //}

//            //bundled["variants"] = vars;

//            //x.Set("_bundled", bundled);
//            return x;
//        }

//        public void RegisterFileEditor(PackedScene editor, string templateName = null)
//        {
//            if (templateName is null)
//            {
//                var key = "any";
//                if (registeredFileEditors.ContainsKey(key))
//                    registeredFileEditors[key].Add(editor);
//                else
//                    registeredFileEditors.Add(key, new List<PackedScene>() { editor });

//                logger?.LogInformation("Loaded file editor {editor}", editor.ResourcePath);
//                return;
//            }

//            if (templateName.Contains(Path.DirectorySeparatorChar) ||
//                templateName.Contains(Path.AltDirectorySeparatorChar))
//                throw new ArgumentException(
//                        $"The provided name must be the filename without any directories",
//                        nameof(templateName));

//            if (!templateName.EndsWith(".tpl"))
//            {
//                if (!templateName.Contains('.'))
//                    templateName += ".tpl";
//                else
//                    throw new ArgumentException($"The provided name must be the filename" +
//                        $" or the filename with the .tpl extension, other extensions are invalid",
//                        nameof(templateName));
//            }

//            if (registeredFileEditors.ContainsKey(templateName))
//                registeredFileEditors[templateName].Add(editor);
//            else
//                registeredFileEditors.Add(templateName, new List<PackedScene>() { editor });

//            logger?.LogInformation("Loaded file editor {editor}", editor.ResourcePath);
//        }

//        public IReadOnlyList<PackedScene> GetFileEditors(string templateName)
//        {
//            if (registeredFileEditors.ContainsKey(templateName))
//                return registeredFileEditors[templateName];
//            else if (registeredFileEditors.TryGetValue("any", out var list))
//                return list;

//            logger?.LogError("Missing generic file editor!");
//            return null;
//        }

//        public void RegisterEntryEditor(PackedScene editor,
//            string variableName = null,
//            string variableClass = null,
//            string variableType = null)
//        {
//            //if (variableName is null && variableClass is null && variableType is null)
//            //    throw new ArgumentException("At least one variable constraint must be set!");

//            if (variableClass != null && !Enum.TryParse<VariableClass>(variableClass, true, out var _))
//            {
//                logger?.LogError("Unknown variable class: {class}", variableClass);
//                return;
//            }
//            if (variableType != null && !Enum.TryParse<VariableType>(variableType, true, out var _))
//            {
//                logger?.LogError("Unknown variable type: {type}", variableType);
//                return;
//            }

//            var key = (
//                vName: variableName ?? "any",
//                vClass: variableClass ?? "any",
//                vType: variableType ?? "any"
//            );

//            if (registeredEntryEditors.ContainsKey(key))
//                registeredEntryEditors[key].Add(editor);
//            else
//                registeredEntryEditors.Add(key, new List<PackedScene>() { editor });

//            logger?.LogInformation("Loaded entry editor {editor}", editor.ResourcePath);
//        }

//        public void RegisterEntryControl(PackedScene control,
//            string variableName = null,
//            string variableClass = null,
//            string variableType = null)
//        {
//            //if (variableName is null && variableClass is null && variableType is null)
//            //    throw new ArgumentException("At least one variable constraint must be set!");

//            if (variableClass != null && !Enum.TryParse<VariableClass>(variableClass, true, out var _))
//            {
//                logger?.LogError("Unknown variable class: {class}", variableClass);
//                return;
//            }
//            if (variableType != null && !Enum.TryParse<VariableType>(variableType, true, out var _))
//            {
//                logger?.LogError("Unknown variable type: {type}", variableType);
//                return;
//            }

//            var key = (
//                vName: variableName ?? "any",
//                vClass: variableClass ?? "any",
//                vType: variableType ?? "any"
//            );

//            if (registeredValueControls.ContainsKey(key))
//                registeredValueControls[key].Add(control);
//            else
//                registeredValueControls.Add(key, new List<PackedScene>() { control });

//            logger?.LogInformation("Loaded entry control {control}", control.ResourcePath);
//        }

//        public IReadOnlyList<PackedScene> GetEntryEditors(
//            string variableName,
//            string variableClass,
//            string variableType)
//        {
//            if (!Enum.TryParse<VariableClass>(variableClass, out var _))
//                logger?.LogWarning("Using unknown variable class {class}", variableClass);
//            if (!Enum.TryParse<VariableType>(variableType, out var _))
//                logger?.LogWarning("Using unknown variable type {type}", variableType);

//            var key = (
//                vName: variableName,
//                vClass: variableClass,
//                vType: variableType
//            );

//            return GetEntryEditors(key);
//        }

//        private IReadOnlyList<PackedScene> GetEntryEditors((string vName, string vClass, string vType) key)
//        {
//            if (registeredEntryEditors.ContainsKey(key))
//                return registeredEntryEditors[key];
//            else
//            {
//                if (key.vName != "any")
//                {
//                    key.vName = "any";
//                    return GetEntryEditors(key);
//                }
//                if (key.vClass != "any")
//                {
//                    IReadOnlyList<PackedScene> result = null;
//                    if (key.vType != "any")
//                    {
//                        var tmpKey = key;
//                        tmpKey.vType = "any";
//                        result = GetEntryEditors(tmpKey);
//                    }
//                    if (result is null)
//                    {
//                        var tmpKey = key;
//                        tmpKey.vClass = "any";
//                        result = GetEntryEditors(tmpKey);
//                    }
//                    if (result != null)
//                        return result;
//                }
//                if (key.vType != "any")
//                {
//                    key.vType = "any";
//                    return GetEntryEditors(key);
//                }

//                logger?.LogError("Missing generic entry editor!");
//                return null;
//            }
//        }

//        public IReadOnlyList<PackedScene> GetEntryControls(
//            string variableName,
//            string variableClass,
//            string variableType)
//        {
//            if (!Enum.TryParse<VariableClass>(variableClass, out var _))
//                logger?.LogWarning("Using unknown variable class {class}", variableClass);
//            if (!Enum.TryParse<VariableType>(variableType, out var _))
//                logger?.LogWarning("Using unknown variable type {type}", variableType);

//            var key = (
//                vName: variableName,
//                vClass: variableClass,
//                vType: variableType
//            );

//            return GetEntryControls(key);
//        }

//        private IReadOnlyList<PackedScene> GetEntryControls((string vName, string vClass, string vType) key)
//        {
//            if (registeredValueControls.ContainsKey(key))
//                return registeredValueControls[key];
//            else
//            {
//                if (key.vName != "any")
//                {
//                    key.vName = "any";
//                    return GetEntryControls(key);
//                }
//                if (key.vClass != "any")
//                {
//                    IReadOnlyList<PackedScene> result = null;
//                    if (key.vType != "any")
//                    {
//                        var tmpKey = key;
//                        tmpKey.vType = "any";
//                        result = GetEntryControls(tmpKey);
//                    }
//                    if (result is null)
//                    {
//                        var tmpKey = key;
//                        tmpKey.vClass = "any";
//                        result = GetEntryControls(tmpKey);
//                    }
//                    if (result != null)
//                        return result;
//                }
//                if (key.vType != "any")
//                {
//                    key.vType = "any";
//                    return GetEntryControls(key);
//                }

//                logger?.LogError("Missing generic entry control!");
//                return null;
//            }
//        }

//        public IReadOnlyDictionary<string, List<PackedScene>> GetViews()
//        {
//            return registeredViews;
//        }
//    }

//    class PluginLoadContext : AssemblyLoadContext
//    {
//        private readonly AssemblyDependencyResolver _resolver;

//        public PluginLoadContext(string pluginPath)
//        {
//            _resolver = new AssemblyDependencyResolver(pluginPath);
//            GD.Print(pluginPath);
//        }

//        protected override Assembly Load(AssemblyName assemblyName)
//        {
//            GD.Print(assemblyName);
//            string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
//            GD.Print(assemblyPath);
//            if (assemblyPath != null)
//                return LoadFromAssemblyPath(assemblyPath);

//            return null;
//        }
//    }

//    static class PathExtensions
//    {
//        public static string CombineUsingSeparator(char separator, params string[] paths)
//        {
//            var ret = Path.Combine(paths);
//            return ret.Replace(Path.DirectorySeparatorChar, separator);
//        }

//        public static string CombineGodotPath(params string[] paths)
//        {
//            return CombineUsingSeparator('/', paths);
//        }
//    }
//}