using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Dusty_Jukebox
{
    public class Music
    {
       
        public int BPM { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Key { get; set; }
        public string TimeSignature { get; set; }
        public List<string> Chords { get; set; }
        public Dictionary<string, double> note_in_ms { get; set; }

        public List<string> chromatic = new List<string>
        {
            "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"
        };

        private static readonly Dictionary<string, int[]> ScaleIntervals = new Dictionary<string, int[]>
    {
        { "Major", new[] {2, 2, 1, 2, 2, 2, 1} },   // Ionian
        { "Minor", new[] {2, 1, 2, 2, 1, 2, 2} },   // Aeolian
        { "Dorian", new[] {2, 1, 2, 2, 2, 1, 2} },
        { "Phrygian", new[] {1, 2, 2, 2, 1, 2, 2} },
        { "Lydian", new[] {2, 2, 2, 1, 2, 2, 1} },
        { "Mixolydian", new[] {2, 2, 1, 2, 2, 1, 2} },
        { "Locrian", new[] {1, 2, 2, 1, 2, 2, 2} }
    };

        public Dictionary<string, string> Intervals { get; } = new Dictionary<string, string>
    {
        { "1", "Perfect Unison" },
        { "b2", "Minor Second" },
        { "2", "Major Second" },
        { "b3", "Minor Third" },
        { "3", "Major Third" },
        { "4", "Perfect Fourth" },
        { "#4", "Augmented Fourth" },
        { "b5", "Diminished Fifth" },
        { "5", "Perfect Fifth" },
        { "#5", "Augmented Fifth" },
        { "b6", "Minor Sixth" },
        { "6", "Major Sixth" },
        { "b7", "Minor Seventh" },
        { "7", "Major Seventh" },
        { "8", "Octave" }
    };

        public Dictionary<string, List<string>> Chord_Formula { get; } = new Dictionary<string, List<string>>
    {
        { "Major", new List<string> { "1", "3", "5" } },
        { "Minor", new List<string> { "1", "b3", "5" } },
        { "Diminished", new List<string> { "1", "b3", "b5" } },
        { "Augmented", new List<string> { "1", "3", "#5" } },
        { "Major7", new List<string> { "1", "3", "5", "7" } },
        { "Minor7", new List<string> { "1", "b3", "5", "b7" } },
        { "Dominant7", new List<string> { "1", "3", "5", "b7" } },
        { "Suspended2", new List<string> { "1", "2", "5" } },
        { "Suspended4", new List<string> { "1", "4", "5" } }
    };
        public Dictionary<string, List<string>> Scale_Formula { get; } = new Dictionary<string, List<string>>
    {
        { "Major", new List<string> { "1", "2", "3", "4", "5", "6", "7" } },
        { "Natural Minor", new List<string> { "1", "2", "b3", "4", "5", "b6", "b7" } },
        { "Harmonic Minor", new List<string> { "1", "2", "b3", "4", "5", "b6", "7" } },
        { "Melodic Minor", new List<string> { "1", "2", "b3", "4", "5", "6", "7" } },
        { "Dorian", new List<string> { "1", "2", "b3", "4", "5", "6", "b7" } },
        { "Phrygian", new List<string> { "1", "b2", "b3", "4", "5", "b6", "b7" } },
        { "Lydian", new List<string> { "1", "2", "3", "#4", "5", "6", "7" } },
        { "Mixolydian", new List<string> { "1", "2", "3", "4", "5", "6", "b7" } },
        { "Locrian", new List<string> { "1", "b2", "b3", "4", "b5", "b6", "b7" } },
    };
        public Music()
        {
            note_in_ms = new Dictionary<string, double>();
        }

        public Dictionary<string, List<string>> Chord_Interval = new Dictionary<string, List<string>>
    {
        { "Major", new List<string> { "1", "3", "5" } },
        { "Minor", new List<string> { "1", "b3", "5" } }
    };

        public Dictionary<string, List<string>> Scales = new Dictionary<string, List<string>>
    {
        { "Natural Minor", new List<string> { "1", "2", "b3", "4", "5", "b6", "b7" } }
    };

        public List<string> GetChordFormula(string name)
        {
            
            return Chord_Interval.TryGetValue(name, out var result) ? result : new List<string>();
        }

        public List<string> GetScaleFormula(string name)
        {
            return Scales.TryGetValue(name, out var result) ? result : new List<string>();
        }

        

        public string GetNoteFromInterval(string rootNote, string interval)
        {
            Dictionary<string, int> intervalSteps = new Dictionary<string, int>
        {
            { "1", 0 }, { "b2", 1 }, { "2", 2 }, { "b3", 3 }, { "3", 4 }, { "4", 5 },
            { "#4", 6 }, { "b5", 6 }, { "5", 7 }, { "#5", 8 }, { "b6", 8 },
            { "6", 9 }, { "b7", 10 }, { "7", 11 }, { "8", 12 }
        };

            if (!intervalSteps.ContainsKey(interval) || !chromatic.Contains(rootNote))
                return "Invalid";

            int rootIndex = chromatic.IndexOf(rootNote);
            int targetIndex = (rootIndex + intervalSteps[interval]) % 12;
            return chromatic[targetIndex];
        }


        public List<string> BuildScale(string rootNote, List<string> intervalFormula)
        {
            return intervalFormula.Select(interval => GetNoteFromInterval(rootNote, interval)).ToList();
        }

        public List<string> BuildChord(string rootNote, List<string> chordFormula)
        {
            return chordFormula.Select(interval => GetNoteFromInterval(rootNote, interval)).ToList();
        }

        public Dictionary<string,string> noteFiles;
        
        public void PlaySound(string note)
        {
            if (!noteFiles.ContainsKey(note))
            {
                MessageBox.Show($"Note {note} not found, you idiot!");
                return;
            }

            try
            {
                using var audioFile = new AudioFileReader(noteFiles[note]);
                using var outputDevice = new WaveOutEvent();
                outputDevice.Init(audioFile);
                outputDevice.Play();
                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(20);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error playing {note}: {ex.Message}");
            }
        }

        public void PlayNoteSequence(List<string> notes, double durationMs)
        {
            foreach (var note in notes)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                new Thread(() => PlaySound(note)).Start();
                int sleepTime = (int)(durationMs - sw.ElapsedMilliseconds);
                if (sleepTime > 0)
                    Thread.Sleep(sleepTime);
            }
        }

        public void PlayChord(List<string> notes, double durationMs)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            foreach (var note in notes)
            {
                new Thread(() => PlaySound(note)).Start();
            }
            int sleepTime = (int)(durationMs - sw.ElapsedMilliseconds);
            if (sleepTime > 0)
                Thread.Sleep(sleepTime);
        }

        public float CalculateInterval()
        {
            return 60000f / BPM;
        }

        public Dictionary<string, double> Notes_Duration_ms()
        {
            float interval = CalculateInterval();
            Dictionary<string, double> noteFactors = new Dictionary<string, double>
        {
            { "w", 4.0 }, { "h", 2.0 }, { "q", 1.0 }, { "e", 0.5 }, { "s", 0.25 }
        };

            foreach (var note in noteFactors)
            {
                note_in_ms[note.Key] = interval * note.Value;
            }
            return note_in_ms;
        }

        public void Automate_Music()
        {
            string root = Key ?? "A";
            string scaleName = Type ?? "Natural Minor";
            var formula = GetScaleFormula(scaleName);
            var scaleNotes = BuildScale(root, formula);

            MessageBox.Show($"Playing {Name} in {root} {scaleName}: {string.Join(" ", scaleNotes)}");

            // Chord progression: Am-F-C-G
            var chordRoots = Chords ?? new List<string> { "A", "F", "C", "G" };
            var chordNotes = new List<List<string>>();
            foreach (var chordRoot in chordRoots)
            {
                var chordFormula = chordRoot == "A" || chordRoot == "G" ? GetChordFormula("Minor") : GetChordFormula("Major");
                chordNotes.Add(BuildChord(chordRoot, chordFormula));
            }

            // Improvised melody: Random A minor notes
            var melody = new List<string>();
            var random = new Random();
            for (int i = 0; i < 8; i++) // 8 quarter notes for simplicity
            {
                melody.Add(scaleNotes[random.Next(scaleNotes.Count)]);
            }

            // Play for 180 seconds
            double quarterNoteMs = note_in_ms["q"];
            double barMs = quarterNoteMs * 4; // 4/4 time
            int bars = (int)(180000 / barMs); // ~90 bars
            MessageBox.Show($"Playing {bars} bars at {BPM} BPM");

            for (int i = 0; i < bars; i++)
            {
                int chordIndex = i % chordNotes.Count;
                int melodyStart = (i % 2) * 4; // Cycle melody every 2 bars
                var barMelody = melody.Skip(melodyStart).Take(4).ToList();

                for (int beat = 0; beat < 4; beat++)
                {
                    if (beat == 0)
                    {
                        PlayChord(chordNotes[chordIndex], quarterNoteMs);
                    }
                    PlayNoteSequence(new List<string> { barMelody[beat] }, quarterNoteMs);
                }
            }
        }

        public void PlayMelody(List<string> melody, double durationMs)
        {
            if (melody == null || melody.Count == 0)
            {
                MessageBox.Show("No melody provided to play.");
                return;
            }
            PlayNoteSequence(melody, durationMs);
        }
    
        public void PlayChordProgression(List<string> chords, double durationMs)
        {
            if (chords == null || chords.Count == 0)
            {
                MessageBox.Show("No chords provided to play.");
                return;
            }
            foreach (var chord in chords)
            {
                var chordNotes = GetChordFormula(chord);
                if (chordNotes.Count > 0)
                {
                    PlayChord(chordNotes, durationMs);
                }
                else
                {
                    MessageBox.Show($"Chord {chord} not found.");
                }
            }
        }

        public void PlayScale(string rootNote, string scaleName, double durationMs)
        {
            if (string.IsNullOrEmpty(rootNote) || string.IsNullOrEmpty(scaleName))
            {
                MessageBox.Show("Root note or scale name is empty.");
                return;
            }
            var scaleFormula = GetScaleFormula(scaleName);
            if (scaleFormula.Count == 0)
            {
                MessageBox.Show($"Scale {scaleName} not found.");
                return;
            }
            var scaleNotes = BuildScale(rootNote, scaleFormula);
            PlayNoteSequence(scaleNotes, durationMs);
        }

        public void PlayChordFromFormula(string rootNote, List<string> chordFormula, double durationMs)
        {
            if (string.IsNullOrEmpty(rootNote) || chordFormula == null || chordFormula.Count == 0)
            {
                MessageBox.Show("Root note or chord formula is empty.");
                return;
            }
            var chordNotes = BuildChord(rootNote, chordFormula);
            PlayChord(chordNotes, durationMs);
        }

        public void Play()
        {
            var song = new Music
            {
                Name = "Moody Loop",
                BPM = 120,
                Key = "A",
                Type = "Natural Minor",
                TimeSignature = "4/4",
                Chords = new List<string> { "A", "F", "C", "G" },
                note_in_ms = new Dictionary<string, double>
                {
                    { "w", 60000 / 120 * 4 }, // Whole note
                    { "h", 60000 / 120 * 2 }, // Half note
                    { "q", 60000 / 120 },     // Quarter note
                    { "e", 60000 / 120 / 2 }, // Eighth note
                    { "s", 60000 / 120 / 4 }  // Sixteenth note
                }
            };
        }
    }
}

