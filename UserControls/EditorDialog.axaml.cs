using Avalonia.Controls;
using Avalonia.Input;
using Prism.Services.Dialogs;
using TQDB_Parser.DBR;

namespace TQDB_Editor.Controls
{
    public interface IEditorDialog : IDialogAware
    {
        public DBRFile DBRFile { get; set; }

        public string VarName { get; set; }

        protected abstract void InitVariable(DBREntry entry);

        protected abstract string GetChangedValue();

        public void Do()
        {
            VarName = "hi";
        }
    }

    public abstract class EditorDialog : UserControl, IDialogAware
    {
        protected DBRFile DBRFile { get; private set; }

        protected string VarName { get; private set; }

        public string Title { get; private set; }

        protected DBREntry entry;

        protected EditorWindow parent;

        public event Action<IDialogResult> RequestClose;

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                OnConfirmed();
            }
        }

        protected abstract void InitVariable(DBREntry entry);

        protected abstract string GetChangedValue();

        private void OnConfirmed()
        {
            parent.Do(VarName, GetChangedValue());
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
        }

        public bool CanCloseDialog() => true;

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            Title = DBRFile.FileName + ':' + VarName;

            entry = DBRFile[VarName];
            parent = Parent as EditorWindow;

            InitVariable(entry);

            (Content as Grid).Children.Add();
        }
    }
}