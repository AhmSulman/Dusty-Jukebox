using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Dusty_Jukebox
{
    public partial class GuitarBud : Window
    {


        public Dictionary<string, string[]> tunings = new Dictionary<string, string[]>
        {
            { "Standard", new[] { "E", "A", "D", "G", "B", "E" } },
            { "Drop D", new[] { "D", "A", "D", "G", "B", "E" } },
            { "Drop C", new[] { "C", "G", "C", "F", "A", "D" } }
        };

        public Dictionary<string, string[]> alternateTunings = new Dictionary<string, string[]>
        {
            { "Open G", new[] { "D", "G", "D", "G", "B", "D" } },
            { "Open D", new[] { "D", "A", "D", "F#", "A", "D" } },
            { "Open C", new[] { "C", "G", "C", "E", "G", "C" } }
        };

        public Dictionary<string, string> noteFiles = new Dictionary<string, string>();

        public int frets = 22;
        public string[] currentTuning;
        public string selectedTuning = "Standard";

        public List<string> chromatic = new List<string>
        {
            "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"
        };

        public Dictionary<string, string> noteToWav = new Dictionary<string, string>();
        public string wavFolderPath = @"C:\Users\ahmed\source\repos\Dusty_Jukebox\Piano\";

        public GuitarBud()
        {
            InitializeComponent();
            currentTuning = tunings[selectedTuning];
            BuildFretboard();
        }

        private void BuildFretboard()
        {
            FretboardGrid.Children.Clear();
            FretboardGrid.Rows = currentTuning.Length;
            FretboardGrid.Columns = frets + 1;

            noteToWav.Clear();

            for (int stringIndex = 0; stringIndex < currentTuning.Length; stringIndex++)
            {
                string openNote = currentTuning[stringIndex];
                int startIndex = chromatic.IndexOf(openNote);
                int baseOctave = 2 + stringIndex;

                for (int fret = 0; fret <= frets; fret++)
                {
                    int chromaticIndex = (startIndex + fret) % 12;
                    int octave = baseOctave + ((startIndex + fret) / 12);

                    string noteName = chromatic[chromaticIndex];
                    string noteNameFlat = ConvertToFlat(noteName);
                    string fullNote = $"{noteNameFlat}{octave}";

                    string wavFile = Path.Combine(wavFolderPath, $"Piano.pp.{fullNote}.wav");

                    if (!noteFiles.ContainsKey(fullNote))
                    {
                        noteFiles[fullNote] = wavFile;
                    }

                    if (File.Exists(wavFile)) noteToWav[fullNote] = wavFile;

                    var btn = new Button
                    {
                        Content = $"{fret}\n{fullNote}",
                        Tag = fullNote,
                        FontSize = 10,
                        Margin = new Thickness(1)
                    };
                    btn.Click += Fret_Click;
                    Grid.SetRow(btn, stringIndex);
                    Grid.SetColumn(btn, fret);
                    FretboardGrid.Children.Add(btn);
                }
            }
        }

        private void PlayMelodyButton_Click(object sender, RoutedEventArgs e)
        {
            string rawMelody = MelodyTextBox.Text;
            string[] notes = rawMelody.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string note in notes)
            {
                string cleanedNote = note.Trim();
                // PlayNote(cleanedNote); // WAV method here
            }
        }

        private void AutoComposeButton_Click(object sender, RoutedEventArgs e)
        {
            // Scale is A Minor: A B C D E F G
            var scale = new List<string> { "A", "B", "C", "D", "E", "F", "G" };
            var chordProgression = new List<string> { "Am", "Dm", "Em", "F", "G", "C", "Bdim" };

            MelodyTextBox.Text = string.Join(" ", scale.Take(4)); // A B C D
            ChordTextBox.Text = string.Join(" - ", chordProgression.Take(4)); // Am - Dm - Em - F
        }
        private string ConvertToFlat(string note)
        {
            return note switch
            {
                "C#" => "Db",
                "D#" => "Eb",
                "F#" => "Gb",
                "G#" => "Ab",
                "A#" => "Bb",
                _ => note
            };
        }

        private void Fret_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string note = btn.Tag.ToString();
                if (noteToWav.TryGetValue(note, out string wavPath))
                {
                    PlayNote(wavPath);
                }
                else
                {
                    MessageBox.Show($"WAV not found for note: {note}");
                }
            }
        }

        private void PlayNote(string filePath)
        {
            var audioFile = new AudioFileReader(filePath);
            var outputDevice = new WaveOutEvent();
            outputDevice.Init(audioFile);
            outputDevice.Play();

            outputDevice.PlaybackStopped += (s, a) =>
            {
                outputDevice.Dispose();
                audioFile.Dispose();
            };
        }

        private List<string> selectedNotes = new();

        private void FretButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string note)
            {
                selectedNotes.Add(note);
                UpdateSelectedNotesDisplay();
            }
        }

        private void UpdateSelectedNotesDisplay()
        {
            //SelectedNotesDisplay.ItemsSource = null;
            //SelectedNotesDisplay.ItemsSource = selectedNotes;
        }

        private void PlayMelody_Click(object sender, RoutedEventArgs e)
        {
            // You can replace this with actual WAV playback using your dictionary
            foreach (var note in selectedNotes)
            {
                Debug.WriteLine($"Play: {note}");
                // PlayNote(note);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            selectedNotes.Clear();
            UpdateSelectedNotesDisplay();
            BuildFretboard(); // Your function to rebuild
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            // Cleanup if necessary
            foreach (var child in FretboardGrid.Children)
            {
                if (child is Button btn)
                {
                    btn.Click -= Fret_Click;
                }
            }

        }

        private void TuningSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
//            if (TuningSelector.SelectedItem is string selected)
  //          {
    //            selectedTuning = selected;
      //          currentTuning = tunings[selectedTuning];
        //        BuildFretboard();
          //  }
        }

        private void RefreshFretboardButton_Click(object sender, RoutedEventArgs e)
        {
            BuildFretboard();
        }

        /* Uncomment if you want to use these selectors in the future
        private void ScaleSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ChordSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }*/

    }
}