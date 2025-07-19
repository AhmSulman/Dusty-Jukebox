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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NAudio.CoreAudioApi;


namespace Dusty_Jukebox
{
    public class AudioMetadata
    {
        public string FilePath { get; set; }
        public double BPM { get; set; }
        public string Key { get; set; }
        public bool IsMinor { get; set; }
        public DateTime LastModified { get; set; }
    }

    public partial class MainWindow : Window
    {
        private WaveOutEvent outputDevice;
        private AudioFileReader audioFile;
        private DispatcherTimer positionTimer;
        private bool isTrackLoaded = false;
        private bool isPlaying = false;
        private bool isDraggingSlider = false;
        

        private Dictionary<string, AudioMetadata> metadataCache = new();
        private string cacheFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio_metadata_cache.json");
        private string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "last_folder.txt");

        public MainWindow()
        {
            InitializeComponent();

            _play_btn.Click += Play_Click;
            _pause_btn.Click += Pause_Click;
            _analyse_btn.Click += Analyse_Click;
            Volume.ValueChanged += Volume_Changed;
            _open_btn.Click += Open_Click;
            listBox.SelectionChanged += ListBox_SelectionChanged;

            positionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            positionTimer.Tick += PositionTimer_Tick;

            LoadCache();
            LoadLastUsedDirectory();
        }

        public class AudioDevice {
            public string Name { get; set; }
            public int Index { get; set; }

            public override string ToString() => Name;


        }

        private void LoadCache()
        {
            if (File.Exists(cacheFilePath))
            {
                string json = File.ReadAllText(cacheFilePath);
                metadataCache = JsonConvert.DeserializeObject<Dictionary<string, AudioMetadata>>(json) ?? new();
            }
        }

        private void SaveCache()
        {
            string json = JsonConvert.SerializeObject(metadataCache, Formatting.Indented);
            File.WriteAllText(cacheFilePath, json);
        }

        private void SaveLastUsedDirectory(string path)
        {
            File.WriteAllText(configFilePath, path);
        }

        private void LoadLastUsedDirectory()
        {
            if (File.Exists(configFilePath))
            {
                string folderPath = File.ReadAllText(configFilePath);
                if (Directory.Exists(folderPath))
                {
                    LoadAudioFiles(folderPath);
                }
            }
        }

        private void LoadAudioFiles(string folderPath = null)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog();
                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;
                folderPath = dialog.SelectedPath;
            }

            SaveLastUsedDirectory(folderPath);

            var files = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                                 .Where(f => f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                                             f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                                 .ToList();

            listBox.ItemsSource = files.Select(Path.GetFileName).ToList();

            foreach (var file in files)
            {
                string absPath = Path.GetFullPath(file);
                DateTime lastModified = File.GetLastWriteTimeUtc(absPath);

                if (metadataCache.TryGetValue(absPath, out var metadata) && metadata.LastModified == lastModified)
                {
                    Debug.WriteLine($"Cached: {absPath} - BPM: {metadata.BPM} - Key: {metadata.Key}");
                }
                else
                {
                    (double bpm, string key) = AnalyseFileWithPython(absPath);

                    metadataCache[absPath] = new AudioMetadata
                    {
                        FilePath = absPath,
                        BPM = bpm,
                        Key = key,
                        IsMinor = key.ToLower().Contains("minor"),
                        LastModified = lastModified
                    };

                    SaveCache();
                }
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listBox.SelectedItem is string selectedFile)
            {
                string fullPath = Path.Combine(GetCurrentFolder(), selectedFile);
                LoadTrack(fullPath);
                Play_Click(null, null);
            }
        }

        private string GetCurrentFolder()
        {
            return File.Exists(configFilePath) ? File.ReadAllText(configFilePath) : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio");
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            LoadAudioFiles();
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
            string filePath = GetSelectedFilePath();
            if (filePath == null)
            {
                MessageBox.Show("No valid file selected!");
                return;
            }

            if (metadataCache.TryGetValue(filePath, out var metadata))
            {
                _bpm_display.Text = $"BPM: {metadata.BPM}";
                _key_display.Text = metadata.IsMinor ? $"Key: {metadata.Key.Replace(" minor", "m", StringComparison.OrdinalIgnoreCase)}" : $"Key: {metadata.Key}";
                _key_display.Foreground = metadata.IsMinor ? Brushes.MediumPurple : Brushes.DarkCyan;

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
            else
            {
                MessageBox.Show("Metadata not found for the selected file. Please analyze the file first.");
            }
        }

        private string GetSelectedFilePath()
        {
            if (listBox.SelectedItem is string fileName)
            {
                return Path.Combine(GetCurrentFolder(), fileName);
            }
            return null;
        }

        private (double bpm, string key) AnalyseFileWithPython(string filePath)
        {
            string pythonScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Python", "audio_analyser.py");
            if (!File.Exists(pythonScript))
            {
                MessageBox.Show($"Python script not found at: {pythonScript}");
                return (0, "Unknown");
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "C:\\Users\\ahmed\\source\\repos\\Dusty_Jukebox\\Python\\.venv\\Scripts\\python.exe",
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
                    return (0, "Unknown");
                }

                double bpm = 0;
                string key = "Unknown";
                foreach (var line in output.Split('\n'))
                {
                    if (line.StartsWith("BPM:")) bpm = double.Parse(line.Replace("BPM:", "").Trim());
                    if (line.StartsWith("KEY:")) key = line.Replace("KEY:", "").Trim();
                }

                return (bpm, key);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception: {ex.Message}\nStack Trace: {ex.StackTrace}");
                return (0, "Unknown");
            }
        }

        private void output_devices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            List<AudioDevice> devices = new List<AudioDevice>();
            for (int i =0; i < WaveOut.DeviceCount; i++) {                 
                var capabilities = WaveOut.GetCapabilities(i);
                devices.Add(new AudioDevice { Name = capabilities.ProductName, Index = i });
            }
            output_devices.ItemsSource = devices;
            output_devices.SelectedIndex = 0; // Select the first device by default
        }

        private void guitar_windows_btn_Click_1(object sender, RoutedEventArgs e)
        {
            GuitarBud guitarBud = new GuitarBud();
            guitarBud.Show();
            this.Close();
        }
    }
}
