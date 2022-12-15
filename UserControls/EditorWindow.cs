using Avalonia;
using Avalonia.Controls;
using Prism.Services.Dialogs;
using System.Collections;
using System.ComponentModel;
using System.Text;
using TQDB_Parser.DBR;

namespace TQDB_Editor.Common.Controls
{
    public class UndoRedo<TContainer>
    {
        private readonly TContainer container;
        private readonly List<(Activity, Activity)> history;
        private int historyIndex;

        public int CurrentVersion => historyIndex;
        public bool Changed => historyIndex > -1;

        public delegate void Activity(TContainer container);

        public UndoRedo(TContainer container)
        {
            history = new();
            historyIndex = -1;
            this.container = container;
        }

        public void Do(Activity whatToDo, Activity howToUndo)
        {
            if (whatToDo is null || howToUndo is null)
                throw new ArgumentNullException(whatToDo is null ? nameof(whatToDo) : nameof(howToUndo));

            whatToDo.Invoke(container);

            // Clear dangling history
            if (historyIndex++ < history.Count - 1)
                history.RemoveRange(historyIndex, history.Count - historyIndex + 1);

            history.Add((whatToDo, howToUndo));
        }

        public void Undo()
        {
            if (!Changed)
                return;
            history[historyIndex--].Item2.Invoke(container);
        }

        public void Redo()
        {
            if (!Changed || history.Count - 1 > historyIndex)
                return;
            history[++historyIndex].Item1.Invoke(container);
        }
    }

    public interface IEditorWindow
    {
        public void Init(DBRFile file);
        public void FocusOnEntry(string entryName);
    }

    public abstract partial class EditorWindow : Window, IEditorWindow
    {
        public abstract void InitFile(DBRFile file);
        public abstract void FocusOnEntry(string entryName);

        protected DBRFile? DBRFile { get; private set; }

        public event Action Reinit;

        private UndoRedo<DBRFile>? undoRedo;
        private int lastVersion;

        public void Init(DBRFile file)
        {
            DBRFile = file;
            Title = Path.GetFileName(file.FileName);
            undoRedo = new(file);
            InitFile(file);

            Closing += OnCloseEditor;
        }

        public void Do(string key, string value)
        {
            if (DBRFile![key].Value == value)
                return;

            void doAction(DBRFile file) { file[key].UpdateValue(value); Reinit?.Invoke(); }
            void undoAction(DBRFile file) { file[key].UpdateValue(DBRFile[key].Value); Reinit?.Invoke(); }
            undoRedo!.Do(doAction, undoAction);
        }

        public void Undo() => undoRedo!.Undo();

        public void Redo() => undoRedo!.Redo();

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
                clipboardBuilder.AppendLine(DBRFile![name].Value);
            }
            name = selectedVariables[^1];
            clipboardBuilder.Append('#');
            clipboardBuilder.AppendLine(name);
            clipboardBuilder.Append(DBRFile![name].Value);

            Application.Current?.Clipboard?.SetTextAsync(clipboardBuilder.ToString());
        }

        public void Paste()
        {
            Application.Current?.Clipboard?.GetTextAsync().ContinueWith(str => Paste(str.Result));
        }

        public void Paste(string pasteContent)
        {
            var lines = pasteContent.Split(Environment.NewLine);
            bool justValues = lines.Length == 1;
            if (lines.Length == 1)
                lines = pasteContent.Split(',');

            var selected = GetSelectedVariables();
            using var selectedEnumerator = new VariableEnumerator(selected.GetEnumerator(), this);
            string? lastName = null;
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
            private string? current;

            public VariableEnumerator(IEnumerator<string> baseEnumerator, EditorWindow window)
            {
                this.baseEnumerator = baseEnumerator;
                this.window = window;
            }

            public string Current => current;

            object? IEnumerator.Current => current;

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
                if (current != null && window.TryGetNextVariable(current, out var name))
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
            lastVersion = undoRedo!.CurrentVersion;
            CheckVersion();
        }

        private void CheckVersion()
        {
            if (lastVersion != undoRedo!.CurrentVersion)
                Title = Path.GetFileName(DBRFile!.FileName) + '*';
            else
                Title = Path.GetFileName(DBRFile!.FileName);
        }

        public void OnCloseEditor(object? caller, CancelEventArgs args)
        {
            if (args.Cancel)
                return;
            if (lastVersion != undoRedo!.CurrentVersion)
            {
                //dialog.PopupCentered();
            }
            else
                // consider using Hide
                Close();
        }
    }
}
