using Avalonia.Controls;
using Avalonia.Input;
using DynamicData;
using Prism.Services.Dialogs;
using TQDB_Parser.DBR;

namespace TQDB_Editor.Common.Controls
{
    public interface IEditorDialog : IDialogAware
    {
        public abstract void InitVariable(DBREntry entry);

        public abstract string GetChangedValue();
    }

    public abstract partial class EditorDialog : Window, IEditorDialog
    {
        public const string VarName_ParamName = "varName";
        public const string DBRFile_ParamName = "dbrFile";
        public const string EditorWindow_ParamName = "editorWindow";

        protected DBRFile DBRFile { get; private set; }

        protected string VarName { get; private set; }

        protected DBREntry entry { get; private set; }

        protected EditorWindow parent { get; private set; }

        public bool CanCloseDialog() => true;

        public event Action<IDialogResult> RequestClose;

        protected abstract event Action Confirm;

        public void OnDialogClosed() { }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                OnConfirmed();
            }
        }

        private void OnConfirmed()
        {
            parent.Do(VarName, GetChangedValue());
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (!parameters.TryGetValue(VarName_ParamName, out string varName))
                return;
            if (!parameters.TryGetValue(DBRFile_ParamName, out DBRFile file))
                return;
            if (!parameters.TryGetValue(EditorWindow_ParamName, out EditorWindow parent))
                return;

            VarName = varName;
            DBRFile = file;
            entry = DBRFile[VarName];
            this.parent = parent;

            Title = DBRFile.FileName + ':' + VarName;

            InitVariable(entry);

            Confirm += OnConfirmed;
        }

        public abstract void InitVariable(DBREntry entry);

        public abstract string GetChangedValue();
    }
}