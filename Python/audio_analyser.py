import sys
import librosa
import numpy as np
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import librosa.display

# Define pitch class names
pitch_classes = ['C', 'C#', 'D', 'D#', 'E', 'F',
                 'F#', 'G', 'G#', 'A', 'A#', 'B']

# Krumhansl-Schmuckler major and minor key profiles
major_profile = np.array([6.35, 2.23, 3.48, 2.33, 4.38,
                          4.09, 2.52, 5.19, 2.39, 3.66,
                          2.29, 2.88])

minor_profile = np.array([6.33, 2.68, 3.52, 5.38, 2.60,
                          3.53, 2.54, 4.75, 3.98, 2.69,
                          3.34, 3.17])

# Create a dictionary with 24 keys (12 major + 12 minor)
key_profiles = {}
for i, pitch in enumerate(pitch_classes):
    key_profiles[f"{pitch} Major"] = np.roll(major_profile, i)
    key_profiles[f"{pitch} Minor"] = np.roll(minor_profile, i)

def detect_key(chroma_vector):
    similarities = {
        key: np.dot(chroma_vector, profile)
        for key, profile in key_profiles.items()
    }
    return max(similarities, key=similarities.get)

def estimate_bpm_and_key(filepath):
    try:
        y, sr = librosa.load(filepath)
        tempo, _ = librosa.beat.beat_track(y=y, sr=sr)

        chroma = librosa.feature.chroma_stft(y=y, sr=sr)
        chroma_avg = np.mean(chroma, axis=1)

        # Normalize the chroma vector for cosine comparison
        chroma_norm = chroma_avg / np.linalg.norm(chroma_avg)
        key = detect_key(chroma_norm)

        tempo_val = tempo.item() if hasattr(tempo, "item") else tempo
        print(f"BPM:{round(tempo_val)}")
        print(f"KEY:{key}")

        plt.figure(figsize=(10, 4))
        librosa.display.waveshow(y, sr=sr)
        plt.title(f"Waveform of {filepath}")
        plt.tight_layout()
        plt.savefig("waveform.png")

    except Exception as e:
        print(f"ERROR:{str(e)}")

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("ERROR:No file path provided")
        sys.exit(1)
    estimate_bpm_and_key(sys.argv[1])
