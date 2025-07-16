namespace Happy.BookCreator
{
    using Happy.Document;
    using Happy.Document.Html;
    using Happy.Document.TTS;
    using Happy.Document.Word;
    using Happy.Reader;
    using System;
    using System.Buffers;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
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
            ddlOutputType.ItemsSource = Enum.GetValues(typeof(OutputTypeEnum));
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
            var outputType = Enum.Parse<OutputTypeEnum>(ddlOutputType.SelectedValue?.ToString() ?? "");

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
                    reader = new NovelFireReader(url, bookName, txtHeaderRemove.Text, tts: outputType == OutputTypeEnum.TTS);
                    break;
                default:
                    throw new Exception("No reader selected.");
            }

            int counter = 0;
            var path = txtFolder.Text;
            if (!path.EndsWith("\\")) path += "\\";
            await foreach (var chapter in reader.GetChapters(chapterCount, $"{path}{txtBookName.Text} tts"))
            {
                Chapters.Add(chapter);
                btnSave.Content = $"Save ({Chapters.Count})";
                if (outputType == OutputTypeEnum.TTS)
                {
                    btnImport.Content = $"Saved {Chapters.Count}";
                }
                dataGrid.ScrollIntoView(chapter);
                counter++;
            }

            if (outputType == OutputTypeEnum.TTS)
            {
                btnImport.Content = $"Import/Save";
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
            if (string.IsNullOrEmpty(ddlReader.SelectedValue?.ToString()))
                return;

            var outputType = Enum.Parse<OutputTypeEnum>(ddlOutputType.SelectedValue?.ToString() ?? "");
            var documentPath = $"{txtFolder.Text}{txtBookName.Text}";
            IWriter? writer = null;


            switch (outputType)
            {
                case OutputTypeEnum.None:
                    break;
                case OutputTypeEnum.Docx:
                    writer = new WordWriter(txtBookName.Text, documentPath);
                    break;
                case OutputTypeEnum.Html:
                    writer = new HtmlWriter(txtBookName.Text, documentPath);
                    break;
                case OutputTypeEnum.TTS:
                    writer = new KokoroWriter(txtBookName.Text, documentPath);
                    break;
                default:
                    break;
            }

            if (writer == null) return;

            btnSave.IsEnabled = false;
            btnSave.Content = "Saving...";

            await Task.Run(() =>
            {
                foreach (var chapter in Chapters)
                {
                    writer.WriteChapter(chapter);
                } 

                writer.Save();
            });

            btnSave.Content = "Saved";
            btnSave.IsEnabled = true;
        }

        private void ddlOutputType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(ddlOutputType.SelectedValue?.ToString()))
                return;

            var outputType = Enum.Parse<OutputTypeEnum>(ddlOutputType.SelectedValue?.ToString() ?? "");

            if (outputType != OutputTypeEnum.TTS)
            {
                btnSave.Visibility = Visibility.Visible;
                btnImport.Content = "Import";
                return;
            }

            btnSave.Visibility = Visibility.Hidden;
            btnImport.Content = "Import/Save";
        }
    }
}
