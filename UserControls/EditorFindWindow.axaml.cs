using Avalonia.Controls;
using TQDB_Parser.DBR;

namespace TQDB_Editor.Common.Controls
{
    public partial class EditorFindWindow : Window
    {
        private DBREntry selectedResult;

        public EditorFindWindow()
        {
            InitializeComponent();
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

    }
}
