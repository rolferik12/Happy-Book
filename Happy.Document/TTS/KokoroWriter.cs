namespace Happy.Document.TTS
{
    using Happy.Reader;
    using KokoroSharp;
    using KokoroSharp.Core;
    using KokoroSharp.Utilities;
    using Microsoft.ML.OnnxRuntime;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;


    public class KokoroWriter : IWriter
    {
        private List<Chapter> chapters = new List<Chapter>();
        private KokoroWavSynthesizer? synth = null;
        private string _folderPath = string.Empty;
        private string _name = string.Empty;
        private int _counter = 1;
        public KokoroWriter(string name, string folderPath)
        {
            _folderPath = folderPath + " tts";
            _name = name;

            if (!Directory.Exists(_folderPath))
            {
                Directory.CreateDirectory(_folderPath);
            }
            
            var currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (currentPath == null) return;

            var options = new SessionOptions();
            options.AppendExecutionProvider_CUDA();
            synth = new KokoroWavSynthesizer($@"{currentPath}\kokoro.onnx", options);
        }

        public void Save()
        {
            if (synth == null) return;

            foreach (var chapter in chapters)
            {
                synth.SaveAudioToFile(chapter.TTS, $@"{_folderPath}\{chapter.Title.RemoveSpecialCharacters()}.wav");
            }
        }

        public void WriteChapter(Chapter chapter)
        {
            chapters.Add(chapter);
        }
    }
}
