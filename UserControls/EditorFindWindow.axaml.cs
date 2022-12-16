using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using ReactiveUI;
using System.Collections.Specialized;
using System.Linq;
using TQDB_Parser.DBR;

namespace TQDB_Editor.Common.Controls
{
    public class SearchResult
    {
        public string Value { get; private set; }

        public SearchResult(string value, Action<string> focusCallback)
        {
            Value = value;
        }
    }

    public partial class EditorFindWindow : Window
    {
        private readonly DispatcherTimer changedTimer;

        private readonly List<SearchResult> results;

        private readonly TextBox searchInput;

        private readonly ItemsRepeater resultControl;

        public event Action<DBREntry>? FocusOnEntry;

        private readonly ToggleSwitch valueToggle;
        private readonly ToggleSwitch nameToggle;
        private readonly ToggleSwitch classToggle;
        private readonly ToggleSwitch descToggle;
        private readonly ToggleSwitch typeToggle;
        private readonly ToggleSwitch defaultToggle;
        private readonly ToggleSwitch invalidToggle;

        public EditorFindWindow()
        {
            InitializeComponent();
            changedTimer = new DispatcherTimer(TimeSpan.FromSeconds(.5), DispatcherPriority.Background, (o, args) => DoSearch());
            results = new();

            resultControl = this.FindControl<ItemsRepeater>("ResultList")!;
            //resultControl.Items = results;
            resultControl.Children.CollectionChanged += ResultChildrenAdded;

            searchInput = this.FindControl<TextBox>("SearchInput")!;
            searchInput.TextChanged += SearchTextChanged;

            valueToggle = this.FindControl<ToggleSwitch>("MatchValue")!;
            valueToggle.Click += SearchFilterToggled;

            nameToggle = this.FindControl<ToggleSwitch>("MatchName")!;
            nameToggle.Click += SearchFilterToggled;

            classToggle = this.FindControl<ToggleSwitch>("MatchClass")!;
            classToggle.Click += SearchFilterToggled;

            descToggle = this.FindControl<ToggleSwitch>("MatchDescription")!;
            descToggle.Click += SearchFilterToggled;

            typeToggle = this.FindControl<ToggleSwitch>("MatchType")!;
            typeToggle.Click += SearchFilterToggled;

            invalidToggle = this.FindControl<ToggleSwitch>("MatchInvalid")!;
            invalidToggle.Click += SearchFilterToggled;

            defaultToggle = this.FindControl<ToggleSwitch>("MatchDefault")!;
            defaultToggle.Click += SearchFilterToggled;
        }

        public void SearchTextChanged(object? caller, RoutedEventArgs args)
        {
            if (changedTimer.IsEnabled)
                changedTimer.Stop();
            changedTimer.Start();
        }

        public void SearchFilterToggled(object? caller, RoutedEventArgs args)
        {
            DoSearch();
        }

        private void DoSearch()
        {
            changedTimer.Stop();
            results.Clear();
            var str = searchInput.Text;
            if (string.IsNullOrEmpty(str))
                return;

            bool matchValue = valueToggle.IsChecked ?? false;
            bool matchName = nameToggle.IsChecked ?? false;
            bool matchClass = classToggle.IsChecked ?? false;
            bool matchType = typeToggle.IsChecked ?? false;
            bool matchDesc = descToggle.IsChecked ?? false;
            bool matchDefault = defaultToggle.IsChecked ?? false;
            bool matchInvalid = invalidToggle.IsChecked ?? false;

            if (!(matchValue || matchName || matchClass || matchType || matchDesc || matchDefault || matchInvalid))
            {
                searchInput.Foreground = new SolidColorBrush(Colors.Red);
                return;
            }

            var split = str.Split('*', StringSplitOptions.RemoveEmptyEntries);
            var matchingEntries = (DataContext as DBRFile)!.Entries.Where(entry =>
                (matchInvalid || entry.IsValid()) &&
                (
                    (matchValue && split.Any(str => entry.Value.Contains(str))) ||
                    (matchName && split.Any(str => entry.Name.Contains(str))) ||
                    (matchType && split.Any(str => entry.Template.Type.ToString().Contains(str))) ||
                    (matchClass && split.Any(str => entry.Template.Class.ToString().Contains(str))) ||
                    (matchDesc && split.Any(str => entry.Template.Description.ToString().Contains(str))) ||
                    (matchDefault && split.Any(str => entry.Template.GetDefaultValue().Contains(str)))
                )
            );

            if (!matchingEntries.Any())
            {
                searchInput.Foreground = new SolidColorBrush(Colors.Red);
                return;
            }

            foreach (var matchingEntry in matchingEntries)
                results.Add(new(matchingEntry.Name, SearchResults_ItemSelected));

            resultControl.Items = results;
        }

        private void ResultChildrenAdded(object? caller, NotifyCollectionChangedEventArgs args)
        {
            if (args.NewItems is null)
                return;
            foreach (var child in args.NewItems)
            {
                (child as IControl)!.ObservableForProperty(x => x.IsFocused).Subscribe(ctrl => SearchResults_ItemSelected(((ctrl.Sender as Border)!.Child as TextBlock)!.Text));
            }
            resultControl.Children.First().Focus();
        }

        private void SearchResults_ItemSelected(string? value)
        {
            if (value is null)
                return;
            FocusOnEntry?.Invoke((DataContext as DBRFile)![value]);
        }

        //private void FindPrevious()
        //{
        //    if (searchResults.ItemCount == 0)
        //        return;
        //    if (!searchResults.IsAnythingSelected())
        //        return;
        //    var curr = searchResults.GetSelectedItems()[0];
        //    if (curr < 1)
        //        return;
        //    searchResults.Select(curr - 1);
        //    SearchResults_ItemSelected(curr - 1);
        //}

        //private void FindNext()
        //{
        //    if (searchResults.ItemCount == 0)
        //        return;
        //    if (!searchResults.IsAnythingSelected())
        //    {
        //        searchResults.Select(0);
        //        return;
        //    }
        //    var curr = searchResults.GetSelectedItems()[0];
        //    if (curr > searchResults.ItemCount - 2)
        //        return;
        //    searchResults.Select(curr + 1);
        //    SearchResults_ItemSelected(curr + 1);
        //}

        //public void ShowFind()
        //{
        //    findDialog.Position = new Vector2i(Math.Max(0, Position.x - 500), Position.y);
        //    findDialog.Size = new Vector2i(500, Size.y);
        //    findDialog.Show();
        //}

    }
}
