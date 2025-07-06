@echo off
echo Installing libraries for Dusty_Jukebox WPF app...

:: Install NAudio - WAV Playback
dotnet add package NAudio

:: Install Newtonsoft.Json - Serialization
dotnet add package Newtonsoft.Json

:: Install MVVM Community Toolkit
dotnet add package CommunityToolkit.Mvvm

:: Install Fody + PropertyChanged.Fody
dotnet add package Fody
dotnet add package PropertyChanged.Fody

:: Install Material Design in XAML Toolkit (with themes)
dotnet add package MaterialDesignThemes
dotnet add package MaterialDesignColors

:: Install Python.NET for C#-Python bridge
dotnet add package Python.Runtime

:: Restore packages
dotnet restore

echo.
echo ----------------------------------
echo ✅ All packages installed successfully.
echo 🧠 Now install Python + librosa manually:
echo     pip install librosa soundfile numpy
echo ----------------------------------
pause
