namespace Happy.BookCreator
{
    using Happy.Document;
    using Happy.Document.Html;
    using Happy.Document.Word;
    using Happy.Reader;
    using System;
    using System.Collections.ObjectModel;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Forms;
    using System.Windows.Input;

    public class MainViewModel : ViewModelBase
    {
        private string _bookName = string.Empty;
        private string _folderPath = @"C:\temp\BookReader\";
        private string _url = string.Empty;
        private ReaderEnum? _selectedReader;
        private string _chapterCount = string.Empty;
        private string _headerRemoveText = string.Empty;
        private string _headerReplaceText = string.Empty;
        private OutputTypeEnum? _selectedOutputType;
        private bool _isImportEnabled = true;
        private bool _isSaveEnabled;
        private string _importButtonText = "Import";
        private string _saveButtonText = "Save";

        private static readonly Regex NumericRegex = new("[^0-9.-]+");
        private CancellationTokenSource? _importCts;

        public MainViewModel()
        {
            ReaderOptions = new ObservableCollection<ReaderEnum>(Enum.GetValues<ReaderEnum>());
            OutputTypeOptions = new ObservableCollection<OutputTypeEnum>(Enum.GetValues<OutputTypeEnum>());

            ImportCommand = new RelayCommand(async _ => await ImportAsync(), _ => IsImportEnabled);
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => IsSaveEnabled);
            BrowseCommand = new RelayCommand(_ => Browse());
            StopCommand = new RelayCommand(_ => StopImport(), _ => IsStopVisible);
        }

        public ObservableCollection<Chapter> Chapters { get; } = new();
        public ObservableCollection<ReaderEnum> ReaderOptions { get; }
        public ObservableCollection<OutputTypeEnum> OutputTypeOptions { get; }

        public string BookName
        {
            get => _bookName;
            set => SetProperty(ref _bookName, value);
        }

        public string FolderPath
        {
            get => _folderPath;
            set => SetProperty(ref _folderPath, value);
        }

        public string Url
        {
            get => _url;
            set
            {
                if (SetProperty(ref _url, value))
                {
                    DetectReaderFromUrl(value);
                }
            }
        }

        public ReaderEnum? SelectedReader
        {
            get => _selectedReader;
            set => SetProperty(ref _selectedReader, value);
        }

        public string ChapterCount
        {
            get => _chapterCount;
            set
            {
                if (string.IsNullOrEmpty(value) || !NumericRegex.IsMatch(value))
                    SetProperty(ref _chapterCount, value);
            }
        }

        public string HeaderRemoveText
        {
            get => _headerRemoveText;
            set => SetProperty(ref _headerRemoveText, value);
        }

        public string HeaderReplaceText
        {
            get => _headerReplaceText;
            set => SetProperty(ref _headerReplaceText, value);
        }

        public OutputTypeEnum? SelectedOutputType
        {
            get => _selectedOutputType;
            set
            {
                if (SetProperty(ref _selectedOutputType, value))
                {
                    OnPropertyChanged(nameof(IsSaveVisible));
                }
            }
        }

        public bool IsImportEnabled
        {
            get => _isImportEnabled;
            set => SetProperty(ref _isImportEnabled, value);
        }

        public bool IsSaveEnabled
        {
            get => _isSaveEnabled;
            set => SetProperty(ref _isSaveEnabled, value);
        }

        public string ImportButtonText
        {
            get => _importButtonText;
            set => SetProperty(ref _importButtonText, value);
        }

        public string SaveButtonText
        {
            get => _saveButtonText;
            set => SetProperty(ref _saveButtonText, value);
        }

        public bool IsSaveVisible => true;

        public ICommand ImportCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand BrowseCommand { get; }
        public ICommand StopCommand { get; }

        private bool _isStopVisible;
        public bool IsStopVisible
        {
            get => _isStopVisible;
            set => SetProperty(ref _isStopVisible, value);
        }

        private void StopImport()
        {
            _importCts?.Cancel();
        }

        private void DetectReaderFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            var lower = url.ToLowerInvariant();

            if (lower.Contains("web.archive.org"))
                SelectedReader = ReaderEnum.WebArchiveRoyalRoad;
            else if (lower.Contains("royalroad.com"))
                SelectedReader = ReaderEnum.RoyalRoad;
            else if (lower.Contains("novelfire"))
                SelectedReader = ReaderEnum.NovelFire;
            else if (lower.Contains("parahumans.wordpress.com"))
                SelectedReader = ReaderEnum.Worm;
        }

        private void Browse()
        {
            var dialog = new FolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                FolderPath = dialog.SelectedPath;
            }
        }

        private async Task ImportAsync()
        {
            if (SelectedReader is null) return;

            _importCts?.Dispose();
            _importCts = new CancellationTokenSource();

            IsImportEnabled = false;
            IsSaveEnabled = false;
            IsStopVisible = true;
            Chapters.Clear();

            int.TryParse(ChapterCount, out var chapterCount);

            BaseReader reader = SelectedReader.Value switch
            {
                ReaderEnum.WebArchiveRoyalRoad => new WebArchiveRoyalReader(Url, BookName, HeaderRemoveText, HeaderReplaceText),
                ReaderEnum.RoyalRoad => new RoyalReader(Url, BookName, HeaderRemoveText, HeaderReplaceText),
                ReaderEnum.Worm => new WormReader(Url, BookName),
                ReaderEnum.NovelFire => new NovelFireReader(Url, BookName, HeaderRemoveText, HeaderReplaceText),
                _ => throw new InvalidOperationException("No reader selected.")
            };

            try
            {
                await foreach (var chapter in reader.GetChapters(chapterCount, _importCts.Token))
                {
                    Chapters.Add(chapter);
                    SaveButtonText = $"Save ({Chapters.Count})";
                }
            }
            catch (OperationCanceledException)
            {
                ImportButtonText = "Stopped";
            }
            finally
            {
                IsStopVisible = false;
                IsSaveEnabled = Chapters.Count > 0;
                IsImportEnabled = true;
            }
        }

        private async Task SaveAsync()
        {
            if (SelectedOutputType is null) return;

            var documentPath = $"{FolderPath}{BookName}";
            IWriter? writer = SelectedOutputType.Value switch
            {
                OutputTypeEnum.Docx => new WordWriter(BookName, documentPath),
                OutputTypeEnum.Html => new HtmlWriter(BookName, documentPath),
                _ => null
            };

            if (writer is null) return;

            IsSaveEnabled = false;
            SaveButtonText = "Saving...";

            await Task.Run(() =>
            {
                foreach (var chapter in Chapters)
                {
                    writer.WriteChapter(chapter);
                }

                writer.Save();
            });

            SaveButtonText = "Saved";
            IsSaveEnabled = true;
        }
    }
}
