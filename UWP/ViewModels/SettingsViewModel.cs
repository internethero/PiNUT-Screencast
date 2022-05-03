using Lib.Core;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace UWP.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        private ResourceLoader ResourceLoader = ResourceLoader.GetForCurrentView();

        //TODO: Uggly hack(?) figure out if there is a better way to do this
        public string Path
        {
            get { return App.Settings.Path; }
            set { App.Settings.Path = value; OnPropertyChanged(); }
        }

        SolidColorBrush DefaultBorderColor = (SolidColorBrush)Application.Current.Resources["TextControlBorderBrush"];

        SolidColorBrush HighlightBorderColor = (SolidColorBrush)Application.Current.Resources["AppRed"];

        public SolidColorBrush _mandatorySettingBorder;
        public SolidColorBrush MandatorySettingBorder
        {
            get { return _mandatorySettingBorder; }
            set { _mandatorySettingBorder = value; OnPropertyChanged(); }
        }

        public SettingsViewModel()
        {
            MandatorySettingBorder = Path == "" ? HighlightBorderColor : DefaultBorderColor;
        }

        public async void OnButtonSelectFolder(object sender, RoutedEventArgs e)
        {
            StorageFolder folder = await FolderPickerDialog();

            if (folder != null)
            {
                if (await IsFilePresent(folder, "ffmpeg.exe"))
                {
                    Path = folder.Path;
                    MandatorySettingBorder = DefaultBorderColor;
                }
                else
                {
                    var messageDialog = new MessageDialog(ResourceLoader.GetString("Settings-DialogWrongPath"));
                    await messageDialog.ShowAsync();
                }
            }
        }

        private async Task<StorageFolder> FolderPickerDialog()
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();

            return folder;
        }

        private async Task<bool> IsFilePresent(StorageFolder folder, string fileName)
        {
            var result = await folder.TryGetItemAsync(fileName);

            return result != null ? true : false;
        }
    }
}
