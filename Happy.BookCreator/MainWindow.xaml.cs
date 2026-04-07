namespace Happy.BookCreator
{
    using System.Collections.Specialized;
    using System.Text.RegularExpressions;
    using System.Windows;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var vm = new MainViewModel();
            DataContext = vm;
            vm.Chapters.CollectionChanged += Chapters_CollectionChanged;
        }

        private void Chapters_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && chaptersGrid.Items.Count > 0)
            {
                chaptersGrid.Dispatcher.BeginInvoke(() =>
                {
                    chaptersGrid.UpdateLayout();
                    chaptersGrid.ScrollIntoView(chaptersGrid.Items[^1]);
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private static readonly Regex _regex = new("[^0-9.-]+");

        private void txtChapterCount_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = _regex.IsMatch(e.Text);
        }

        private void txtChapterCount_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (_regex.IsMatch(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}
