using Microsoft.Win32;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Dusty_Jukebox
{
    public partial class MainWindow : Window
    {
        private WaveOutEvent outputDevice;
        private AudioFileReader audioFile;
        private DispatcherTimer progressTimer;
        private DispatcherTimer positionTimer;
        private bool isTrackLoaded = false;
        private bool isPlaying = false;
        private bool isDraggingSlider = false;
        private MediaFoundationReader currentReader;

        public MainWindow()
        {
            InitializeComponent();

            _play_btn.Click += Play_Click;
            _pause_btn.Click += Pause_Click;
            _analyse_btn.Click += Analyse_Click;
            Volume.ValueChanged += Volume_Changed;
            _open_btn.Click += Open_Click;
            listBox.SelectionChanged += ListBox_SelectionChanged;
            positionTimer = new DispatcherTimer();
            positionTimer.Interval = TimeSpan.FromMilliseconds(500); // Update every second
            positionTimer.Tick += PositionTimer_Tick;


            LoadAudioFiles();
        }

        private void LoadAudioFiles()
        {
            string audioDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio");
            if (!Directory.Exists(audioDir))
                Directory.CreateDirectory(audioDir);

            var files = Directory.GetFiles(audioDir)
                .Where(f => f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                .ToList();

            listBox.ItemsSource = files.Select(Path.GetFileName).ToList();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listBox.SelectedItem is string selectedFile)
            {
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio", selectedFile);
                LoadTrack(fullPath);
                Play_Click(null, null);
            }
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Audio Files (*.mp3;*.wav)|*.mp3;*.wav",
                Multiselect = true,
                Title = "Select Audio Files"
            };

            if (dialog.ShowDialog() == true)
            {
                string targetDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio");
                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                foreach (var selectedPath in dialog.FileNames)
                {
                    string fileName = Path.GetFileName(selectedPath);
                    string targetPath = Path.Combine(targetDir, fileName);
                    if (!File.Exists(targetPath))
                        File.Copy(selectedPath, targetPath);
                }

                LoadAudioFiles();
            }
        }

        private void LoadTrack(string path)
        {
            try
            {
                outputDevice?.Stop();
                outputDevice?.Dispose();
                audioFile?.Dispose();

                audioFile = new AudioFileReader(path);
                outputDevice = new WaveOutEvent();
                outputDevice.Init(audioFile);

                ProgressSlider.Maximum = audioFile.TotalTime.TotalSeconds;
                ProgressSlider.Value = 0;
                positionTimer.Start();

                outputDevice.Volume = (float)Volume.Value;
                outputDevice.PlaybackStopped += (s, e) =>
                {
                    isPlaying = false;

                };


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
//                    progressTimer.Start();
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

        private void PositionTimer_Tick(object sender, EventArgs e)
        {
            if (audioFile != null && !isDraggingSlider)
            {
                ProgressSlider.Value = audioFile.CurrentTime.TotalSeconds;
            }
        }

        private void ProgressSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (audioFile != null)
            {
                audioFile.CurrentTime = TimeSpan.FromSeconds(ProgressSlider.Value);
            }
        }

        private void ProgressSlider_DragStarted(object sender, DragStartedEventArgs e)
        {
            isDraggingSlider = true;
        }

        private void ProgressSlider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            isDraggingSlider = false;
        }

        private void Volume_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (outputDevice != null)
                outputDevice.Volume = (float)e.NewValue;
        }

        
        private void Analyse_Click(object sender, RoutedEventArgs e)
        {
            if (listBox.SelectedItem is not string selectedFile)
            {
                MessageBox.Show("No valid file selected!");
                return;
            }

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio", selectedFile);
            string pythonScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Python", "audio_analyser.py");

            if (!File.Exists(pythonScript))
            {
                MessageBox.Show($"Python script not found at: {pythonScript}");
                return;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = @"C:\\Users\\ahmed\\source\\repos\\Dusty_Jukebox\\Python\\.venv\\Scripts\\python.exe",
                    Arguments = $"\"{pythonScript}\" \"{filePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
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

                _bpm_display.Text = $"BPM: {bpm}";
                _key_display.Text = $"Key: {key}";

                string imgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "waveform.png");
                if (File.Exists(imgPath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imgPath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    waveformImage.Source = bitmap;
                }
                else
                {
                    MessageBox.Show($"Waveform image not found at: {imgPath}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception: {ex.Message}\nStack Trace: {ex.StackTrace}");
            }
        }
    }
}
