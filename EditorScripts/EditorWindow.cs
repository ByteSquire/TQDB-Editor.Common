using Godot;
using System.Collections;
using System.Net.Mime;
using System.Text;
using TQDB_Parser.DBR;

namespace TQDB_Editor.Controls
{
    public abstract partial class EditorWindow : Window
    {
        [Signal]
        public delegate void ReinitEventHandler();
        [Signal]
        public delegate void FocusOnEntryEventHandler();

        public DBRFile DBRFile { get; set; }


        private string statusBarScenePath = "res://Editors/Generic/StatusBar.tscn";
        private string menuBarScenePath = "res://Editors/Generic/MenuBar.tscn";
        private string findDialogScenePath = "res://Editors/Generic/FindWindow.tscn";

        protected Control statusBar;
        protected Control menuBar;
        protected Window findDialog;

        private UndoRedo undoRedo;

        private ConfirmationDialog dialog;
        private AcceptDialog searchFailedDialog;

        private ulong lastVersion;

        private TextEdit searchText;
        private ItemList searchResults;
        private CheckButton mValueButton;
        private CheckButton mNameButton;
        private CheckButton mTypeButton;
        private CheckButton mClassButton;
        private CheckButton mDescButton;
        private CheckButton mDefaultButton;
        private CheckButton mInvalidButton;

        Godot.Timer changedTimer;

        public override void _Ready()
        {
            statusBar = ResourceLoader.Load<PackedScene>(statusBarScenePath).Instantiate<Control>();
            menuBar = ResourceLoader.Load<PackedScene>(menuBarScenePath).Instantiate<Control>();
            var inner = new Control()
            {
                AnchorsPreset = ((int)Control.LayoutPreset.FullRect),
                SizeFlagsVertical = ((int)Control.SizeFlags.ExpandFill),
            };
            foreach (var child in GetChildren())
            {
                RemoveChild(child);
                inner.AddChild(child);
            }
            var contents = new VBoxContainer()
            {
                AnchorsPreset = ((int)Control.LayoutPreset.FullRect),
            };
            AddChild(contents);
            MoveChild(contents, 0);
            contents.AddChild(menuBar);
            contents.AddChild(inner);
            contents.AddChild(statusBar);

            undoRedo = new();
            lastVersion = undoRedo.GetVersion();
            changedTimer = new()
            {
                Autostart = false,
                Name = "SearchText timer",
                Paused = false
            };
            AddChild(changedTimer);
            changedTimer.Timeout += DoSearch;

            dialog = new ConfirmationDialog
            {
                DialogText = "You have unsaved changes, do you want to save before closing?",
                OkButtonText = "Save",
            };
            dialog.AddButton("Discard", true).Pressed += Close;
            dialog.Confirmed += () => { DBRFile.SaveFile(); Close(); };

            AddChild(dialog);

            findDialog = ResourceLoader.Load<PackedScene>(findDialogScenePath).Instantiate<Window>();
            AddChild(findDialog);

            searchFailedDialog = new AcceptDialog()
            {
                Title = "Search failed",
                DialogText = "No search results found",
            };
            findDialog.AddChild(searchFailedDialog);

            undoRedo.VersionChanged += CheckVersion;

            CloseRequested += OnCloseEditor;
            Title = Path.GetFileName(DBRFile.FileName);
            statusBar.GetNode<Label>("ProgressPath/PathContainer/Path").Text += Title;

            findDialog.GetNode<Button>("Base/Buttons/Cancel").Pressed += OnFindCancelled;
            findDialog.GetNode<Button>("Base/Buttons/Previous").Pressed += FindPrevious;
            findDialog.GetNode<Button>("Base/Buttons/Next").Pressed += FindNext;
            findDialog.CloseRequested += OnFindCancelled;

            searchText = findDialog.GetNode<TextEdit>("Base/Content/Split/SearchText");
            searchText.CustomMinimumSize = new Vector2i(300, 0);
            searchText.TextChanged += SearchTextChanged;

            searchResults = findDialog.GetNode<ItemList>("Base/Content/Split/ItemList");
            searchResults.CustomMinimumSize = new Vector2i(300, 0);
            searchResults.ItemSelected += SearchResults_ItemSelected;

            mValueButton = findDialog.GetNode<CheckButton>("Base/Content/Options/MatchValue");
            mNameButton = findDialog.GetNode<CheckButton>("Base/Content/Options/MatchName");
            mClassButton = findDialog.GetNode<CheckButton>("Base/Content/Options/MatchClass");
            mTypeButton = findDialog.GetNode<CheckButton>("Base/Content/Options/MatchType");
            mDescButton = findDialog.GetNode<CheckButton>("Base/Content/Options/MatchDescription");
            mDefaultButton = findDialog.GetNode<CheckButton>("Base/Content/Options/MatchDefaultValue");
            mInvalidButton = findDialog.GetNode<CheckButton>("Base/Content/Options/MatchInvalid");
        }

        public Variant UndoRedoProp
        {
            get
            {
                // Should never be called
                GD.Print("Getting");
                return Variant.CreateFrom(0);
            }
            set
            {
                GD.Print("Doing: " + value);
                var input = (string[])value;
                (var key, var v) = (input[0], input[1]);

                DBRFile[key].UpdateValue(v);
                EmitSignal(nameof(Reinit));
            }
        }

        public void Do(string key, string value)
        {
            if (DBRFile[key].Value == value)
                return;

            undoRedo.CreateAction("Write value");

            undoRedo.AddDoProperty(this, nameof(UndoRedoProp),
                Variant.CreateFrom(new string[] { key, value }));
            undoRedo.AddUndoProperty(this, nameof(UndoRedoProp),
                Variant.CreateFrom(new string[] { key, DBRFile[key].Value }));

            undoRedo.CommitAction();
        }

        public void Undo() => undoRedo.Undo();

        public void Redo() => undoRedo.Redo();

        protected abstract IReadOnlyList<string> GetSelectedVariables();

        protected abstract bool TryGetNextVariable(string currentVar, out string varName);

        public void Copy()
        {
            var selectedVariables = GetSelectedVariables();
            if (selectedVariables.Count == 0)
                return;

            var clipboardBuilder = new StringBuilder();

            string name;
            for (int i = 0; i < selectedVariables.Count - 2; i++)
            {
                name = selectedVariables[i];
                clipboardBuilder.Append('#');
                clipboardBuilder.AppendLine(name);
                clipboardBuilder.AppendLine(DBRFile[name].Value);
            }
            name = selectedVariables[^1];
            clipboardBuilder.Append('#');
            clipboardBuilder.AppendLine(name);
            clipboardBuilder.Append(DBRFile[name].Value);

            DisplayServer.ClipboardSet(clipboardBuilder.ToString());
        }

        public void Paste()
        {
            if (DisplayServer.ClipboardHas())
                Paste(DisplayServer.ClipboardGet());
        }

        public void Paste(string pasteContent)
        {
            var lines = pasteContent.Split(System.Environment.NewLine);
            bool justValues = lines.Length == 1;
            if (lines.Length == 1)
                lines = pasteContent.Split(',');

            var selected = GetSelectedVariables();
            using var selectedEnumerator = new VariableEnumerator(selected.GetEnumerator(), this);
            string lastName = null;
            foreach (var line in lines)
            {
                if (!justValues && line.StartsWith('#'))
                {
                    lastName = line[1..];
                    continue;
                }
                if (lastName != null)
                {
                    Do(lastName, line);
                    lastName = null;
                    continue;
                }
                if (selectedEnumerator.MoveNext())
                {
                    Do(selectedEnumerator.Current, line);
                    continue;
                }
            }
        }

        class VariableEnumerator : IEnumerator<string>
        {
            private readonly IEnumerator<string> baseEnumerator;
            private readonly EditorWindow window;
            private string current;

            public VariableEnumerator(IEnumerator<string> baseEnumerator, EditorWindow window)
            {
                this.baseEnumerator = baseEnumerator;
                this.window = window;
            }

            public string Current => current;

            object IEnumerator.Current => current;

            public void Dispose()
            {
                baseEnumerator.Dispose();
            }

            public bool MoveNext()
            {
                if (baseEnumerator.MoveNext())
                {
                    current = baseEnumerator.Current;
                    return true;
                }
                if (window.TryGetNextVariable(current, out var name))
                {
                    current = name;
                    return true;
                }
                return false;
            }

            public void Reset()
            {
                baseEnumerator.Reset();
            }
        }

        public void OnFileSaved()
        {
            lastVersion = undoRedo.GetVersion();
            CheckVersion();
        }

        private void OnFindCancelled()
        {
            searchText.Text = string.Empty;
            searchResults.Clear();
            findDialog.Hide();
        }

        private void SearchTextChanged()
        {
            changedTimer.Start(.3);
        }

        private void DoSearch()
        {
            changedTimer.Stop();
            var str = searchText.Text;
            searchResults.Clear();

            var matchValue = mValueButton.ButtonPressed;
            var matchName = mNameButton.ButtonPressed;
            var matchClass = mClassButton.ButtonPressed;
            var matchType = mTypeButton.ButtonPressed;
            var matchDesc = mDescButton.ButtonPressed;
            var matchDefault = mDefaultButton.ButtonPressed;
            var matchInvalid = mInvalidButton.ButtonPressed;

            str = '*' + str + '*';
            var matchingEntries = DBRFile.Entries.Where(entry =>
                (matchInvalid || entry.IsValid()) &&
                (
                (matchValue && entry.Value.MatchN(str)) ||
                (matchName && entry.Name.MatchN(str)) ||
                (matchType && entry.Template.Type.ToString().MatchN(str)) ||
                (matchClass && entry.Template.Class.ToString().MatchN(str)) ||
                (matchDesc && entry.Template.Description.ToString().MatchN(str)) ||
                (matchDefault && entry.Template.GetDefaultValue().MatchN(str))
                )
            );

            if (!matchingEntries.Any())
            {
                searchFailedDialog.Position = findDialog.Position + findDialog.Size / 2;
                searchFailedDialog.Popup();
                return;
            }

            foreach (var matchingEntry in matchingEntries)
                searchResults.AddItem(matchingEntry.Name);

            searchResults.Select(0);
            selectedResult = DBRFile[searchResults.GetItemText(0)];
            EmitSignal(nameof(FocusOnEntry));
        }

        private DBREntry selectedResult;
        public DBREntry GetFocussedEntry() => selectedResult;

        private void SearchResults_ItemSelected(long index)
        {
            selectedResult = DBRFile[searchResults.GetItemText((int)index)];
            EmitSignal(nameof(FocusOnEntry));
        }

        private void FindPrevious()
        {
            if (searchResults.ItemCount == 0)
                return;
            if (!searchResults.IsAnythingSelected())
                return;
            var curr = searchResults.GetSelectedItems()[0];
            if (curr < 1)
                return;
            searchResults.Select(curr - 1);
            SearchResults_ItemSelected(curr - 1);
        }

        private void FindNext()
        {
            if (searchResults.ItemCount == 0)
                return;
            if (!searchResults.IsAnythingSelected())
            {
                searchResults.Select(0);
                return;
            }
            var curr = searchResults.GetSelectedItems()[0];
            if (curr > searchResults.ItemCount - 2)
                return;
            searchResults.Select(curr + 1);
            SearchResults_ItemSelected(curr + 1);
        }

        public void ShowFind()
        {
            findDialog.Position = new Vector2i(Math.Max(0, Position.x - 500), Position.y);
            findDialog.Size = new Vector2i(500, Size.y);
            findDialog.Show();
        }

        private void CheckVersion()
        {
            if (lastVersion != undoRedo.GetVersion())
                Title = Path.GetFileName(DBRFile.FileName) + '*';
            else
                Title = Path.GetFileName(DBRFile.FileName);
        }

        protected abstract void OnClose();

        public void OnCloseEditor()
        {
            if (lastVersion != undoRedo.GetVersion())
            {
                dialog.PopupCentered();
            }
            else
                Close();
        }

        private void Close()
        {
            OnClose();
            CallDeferred("queue_free");
        }
    }
}
