using Lib.Core;
using System.Collections.Generic;
using Windows.ApplicationModel.Resources;
using Windows.Storage;

namespace UWP.Services
{
    public class Settings : ObservableObject
    {
        private ApplicationDataContainer LocalSettings = ApplicationData.Current.LocalSettings;

        private ResourceLoader ResourceLoader = ResourceLoader.GetForCurrentView();

        public bool AutoMinimize
        {
            get { return ReadSettings(nameof(AutoMinimize), false); }
            set { SaveSettings(nameof(AutoMinimize), value); OnPropertyChanged(); }
        }

        public bool Sound
        {
            get { return ReadSettings(nameof(Sound), false); }
            set { SaveSettings(nameof(Sound), value); OnPropertyChanged(); }
        }

        public string Path
        {
            get { return ReadSettings(nameof(Path), ""); }
            set { SaveSettings(nameof(Path), value); OnPropertyChanged(); }
        }

        public double Framerate
        {
            get { return ReadSettings(nameof(Framerate), 30.00); }
            set { SaveSettings(nameof(Framerate), value); OnPropertyChanged(); }
        }

        public string Encoder
        {
            get { return ReadSettings(nameof(Encoder), "software"); }
            set { SaveSettings(nameof(Encoder), value); OnPropertyChanged(); }
        }

        public string Profile
        {
            get { return ReadSettings(nameof(Profile), "screenshare"); }
            set { SaveSettings(nameof(Profile), value); OnPropertyChanged(); }
        }

        public int Threads
        {
            get { return ReadSettings(nameof(Threads), 2); }
            set { SaveSettings(nameof(Threads), value); OnPropertyChanged(); }
        }

        public Dictionary<int, string> ThreadsSelection { get; } = new Dictionary<int, string>
        {
            { 2, "2" },
            { 4, "4" },
        };

        public Dictionary<double, string> FramerateSelection { get; } = new Dictionary<double, string>
        {
            { 30.00, "30" },
            { 60.00, "60" },
        };

        public Dictionary<string, string> EncoderSelection { get { return GetEncoderSelection(); } }

        public Dictionary<string, string> ProfileSelection { get { return GetProfileSelection(); } }

        public Settings()
        {
            LocalSettings = ApplicationData.Current.LocalSettings;
        }

        private Dictionary<string, string> GetProfileSelection()
        {
            return new Dictionary<string, string>
            {
                { "screenshare", ResourceLoader.GetString("Settings-ProfileSelectionScreenshare") },
                { "movie", ResourceLoader.GetString("Settings-ProfileSelectionMovie") },
                { "slideshow", ResourceLoader.GetString("Settings-ProfileSelectionSlideshow") },
            };
        }

        private Dictionary<string, string> GetEncoderSelection()
        {
            return new Dictionary<string, string>
            {
                {"software", ResourceLoader.GetString("Software") },
                { "quicksync", "Quick Sync" },
                { "nvidia", "Nvidia" },
            };
        }

        private void SaveSettings(string key, object value)
        {
            LocalSettings.Values[key] = value;
        }

        private T ReadSettings<T>(string key, T defaultValue)
        {
            if (LocalSettings.Values.ContainsKey(key))
            {
                return (T)LocalSettings.Values[key];
            }

            if (null != defaultValue)
            {
                return defaultValue;
            }

            return default(T);
        }
    }
}
