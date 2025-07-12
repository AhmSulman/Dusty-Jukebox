import sys
import librosa
import numpy as np
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import librosa.display

def estimate_bpm_and_key(filepath):
    try:
        y, sr = librosa.load(filepath)
        tempo, _ = librosa.beat.beat_track(y=y, sr=sr)

        chroma = librosa.feature.chroma_stft(y=y, sr=sr)
        chroma_avg = np.mean(chroma, axis=1)
        key_idx = np.argmax(chroma_avg)

        pitch_classes = ['C', 'C#', 'D', 'D#', 'E', 'F',
                         'F#', 'G', 'G#', 'A', 'A#', 'B']
        key = pitch_classes[key_idx]

        tempo_val = tempo.item() if hasattr(tempo, "item") else tempo
        print(f"BPM:{round(tempo_val)}")
        print(f"KEY:{key}")
    
        plt.figure(figsize=(10, 4))
        librosa.display.waveshow(y, sr=sr)
        plt.title(f"Waveform of {filepath}")
        plt.savefig("waveform.png")  # fixed output path
        
    except Exception as e:
        print(f"ERROR:{str(e)}")

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("ERROR:No file path provided")
        sys.exit(1)
    estimate_bpm_and_key(sys.argv[1])
