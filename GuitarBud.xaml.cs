using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

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
            TuningSelector.ItemsSource = tunings.Keys;
            TuningSelector.SelectedItem = selectedTuning;
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
            SelectedNotesDisplay.ItemsSource = null;
            SelectedNotesDisplay.ItemsSource = selectedNotes;
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

        private void ScaleSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ChordSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void TuningSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TuningSelector.SelectedItem is string selected)
            {
                selectedTuning = selected;
                currentTuning = tunings[selectedTuning];
                BuildFretboard();
            }
        }
    }
}
