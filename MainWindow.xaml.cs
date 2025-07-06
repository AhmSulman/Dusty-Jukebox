using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.Win32;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Window = System.Windows.Window;

namespace Dusty_Jukebox
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WaveOutEvent outputDevice;
        private AudioFileReader audioFile;
        private bool isTrackLoaded = false;
        private bool isPlaying = false;

        public MainWindow()
        {
            InitializeComponent();

            _play_btn.Click += Play_Click;
            _pause_btn.Click += Pause_Click;
            _analyse_btn.Click += Analyse_Click;
            Volume.ValueChanged += Volume_Changed;
            _open_btn.Click += Open_Click;
        
        LoadAudioFiles();
        }

        private string GetSelectedFilePath()
        {
            string selectedFile = listBox.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(selectedFile))
                return null;

            string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio", selectedFile);
            return File.Exists(fullPath) ? fullPath : null;
        }

        private void LoadAudioFiles()
        {
            string audioDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio");

            if (!Directory.Exists(audioDir))
                Directory.CreateDirectory(audioDir);

            var files = Directory.GetFiles(audioDir)
                .Where(f => f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                .ToList();

            listBox.ItemsSource = files.Select(System.IO.Path.GetFileName).ToList();
            listBox.SelectionChanged += ListBox_SelectionChanged;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedFile = listBox.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(selectedFile))
                return;

            string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio", selectedFile);
            LoadTrack(fullPath);
            Play_Click(null, null); // auto-play selected track

        }
        private void Open_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Audio Files (*.mp3;*.wav)|*.mp3;*.wav",
                Multiselect = true,
                Title = "Select an MP3 or WAV file"
            };

            if (dialog.ShowDialog() == true)
            {
                string selectedPath = dialog.FileName;
                string fileName = System.IO.Path.GetFileName(selectedPath);
                string targetDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio");
                string targetPath = System.IO.Path.Combine(targetDir, fileName);

                try
                {
                    if (!Directory.Exists(targetDir))
                        Directory.CreateDirectory(targetDir);

                    // Copy file to Audio folder if it doesn't already exist
                    if (!File.Exists(targetPath))
                        File.Copy(selectedPath, targetPath);

                    // Refresh the ListBox
                    LoadAudioFiles();

                    // Auto-select newly added file
                    listBox.SelectedItem = fileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load file: {ex.Message}");
                }
            }
        }
        private void LoadTrack(string path)
        {
            try
            {
                outputDevice?.Stop();
                outputDevice?.Dispose();
                audioFile?.Dispose();

                var reader = new MediaFoundationReader(path);
                outputDevice = new WaveOutEvent();
                outputDevice.Init(reader);
                audioFile = null; // Reset audioFile to null to avoid memory leaks
                outputDevice.Volume = (float)Volume.Value;
                outputDevice.PlaybackStopped += (s, e) => { isPlaying = false; };

                isTrackLoaded = true;
                isPlaying = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading track: {ex.Message}");
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (!isTrackLoaded || outputDevice == null)
            {
                MessageBox.Show("No track loaded. Please select a track first.");
                return;
            }

            try
            {
                if (!isPlaying)
                {
                    outputDevice.Play();
                    isPlaying = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Playback error: " + ex.Message);
            }
        }



        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            if (outputDevice != null && isPlaying)
            {
                outputDevice.Pause();
                isPlaying = false;
            }
        }


        private void Volume_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            string filePath = GetSelectedFilePath();
            if (filePath == null) return;

            var reader = new MediaFoundationReader(filePath);
            var volumeProvider = new VolumeWaveProvider16(reader) { Volume = (float)Volume.Value };
            outputDevice = new WaveOutEvent();
            outputDevice.Init(volumeProvider);


        }

        private void Analyse_Click(object sender, RoutedEventArgs e)
        {
            string filePath = GetSelectedFilePath();
            if (filePath == null)
            {
                MessageBox.Show("No valid file selected!");
                return;
            }

            string pythonScript = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Python", "audio_analyser.py");
            if (!File.Exists(pythonScript))
            {
                MessageBox.Show($"Python script not found at: {pythonScript}");
                return;
            }

            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    // Use the virtual environment's Python executable
                    FileName = @"C:\Users\ahmed\source\repos\Dusty_Jukebox\Python\.venv\Scripts\python.exe", // Replace with actual path
                    Arguments = $"\"{pythonScript}\" \"{filePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = System.Diagnostics.Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        MessageBox.Show($"Error:\n{error}\nOutput:\n{output}");
                        return;
                    }

                    string bpm = "", key = "";
                    foreach (var line in output.Split('\n'))
                    {
                        if (line.StartsWith("BPM:")) bpm = line.Replace("BPM:", "").Trim();
                        if (line.StartsWith("KEY:")) key = line.Replace("KEY:", "").Trim();
                    }

                    if (string.IsNullOrWhiteSpace(bpm) && string.IsNullOrWhiteSpace(key))
                    {
                        MessageBox.Show($"No analysis results found.\nOutput:\n{output}");
                        return;
                    }

                    _bpm_display.Text = $"BPM: {bpm}";
                    _key_display.Text = $"Key: {key}";

                    //MessageBox.Show($"🎵 Analysis Complete\n\nBPM: {bpm}\nKey: {key}", "Analysis");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception: {ex.Message}\nStack Trace: {ex.StackTrace}");
            }
        }

    }
    
}
