namespace Happy.BookCreator
{
    using Happy.Document.Word;
    using Happy.Reader;
    using System;
    using System.Collections.ObjectModel;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Forms;
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            dataGrid.ItemsSource = Chapters;
            ddlReader.ItemsSource = Enum.GetValues(typeof(ReaderEnum));
            txtFolder.Text = @"C:\temp\BookReader\";
        }

        public ObservableCollection<Chapter> Chapters { get; set; } = new ObservableCollection<Chapter>();


        private async void btnImport_Click(object sender, RoutedEventArgs e)
        {
            btnImport.IsEnabled = false;
            btnSave.IsEnabled = false;
            Chapters.Clear();

            var bookName = txtBookName.Text;
            var url = txtUrl.Text;
            var readerType = Enum.Parse<ReaderEnum>(ddlReader.SelectedValue?.ToString() ?? "");
            var chapterCount = 0;

            int.TryParse(txtChapterCount.Text, out chapterCount);

            BaseReader reader;

            switch (readerType)
            {
                case ReaderEnum.WebArchiveRoyalRoad:
                    reader = new WebArchiveRoyalReader(url, bookName, txtHeaderRemove.Text);
                    break;
                case ReaderEnum.RoyalRoad:
                    reader = new RoyalReader(url, bookName, txtHeaderRemove.Text);
                    break;
                case ReaderEnum.Worm:
                    reader = new WormReader(url, bookName);
                    break;
                case ReaderEnum.NovelFire:
                    reader = new NovelFireReader(url, bookName, txtHeaderRemove.Text);
                    break;
                default:
                    throw new Exception("No reader selected.");
            }

            int counter = 0;
            await foreach (var chapter in reader.GetChapters(chapterCount))
            {
                Chapters.Add(chapter);
                dataGrid.ScrollIntoView(chapter);
                counter++;
            }

            btnSave.IsEnabled = true;
            btnImport.IsEnabled = true;
        }

        private static readonly Regex _regex = new Regex("[^0-9.-]+"); //regex that matches disallowed text
        private static bool IsTextAllowed(string text)
        {
            return !_regex.IsMatch(text);
        }

        private void txtChapterCount_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void txtChapterCount_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!IsTextAllowed(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog openFileDlg = new FolderBrowserDialog();
            var result = openFileDlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                txtFolder.Text = openFileDlg.SelectedPath;
            }
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            btnSave.IsEnabled = false;
            btnSave.Content = "Saving...";

            var documentPath = $"{txtFolder.Text}{txtBookName.Text}.docx";
            var writer = new Writer(documentPath);

            await Task.Run(() =>
            {
                foreach (var chapter in Chapters)
                {
                    writer.WriteChapterFromHtml(chapter.Title, chapter.Html);
                }

                writer.Save();
            });

            btnSave.Content = "Saved";
        }
    }
}
