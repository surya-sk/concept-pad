using ConceptPad.Helpers;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ConceptPad.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        string signedIn;
        public string Version
        {
            get
            {
                var version = Windows.ApplicationModel.Package.Current.Id.Version;
                return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
            }
        }
        public SettingsPage()
        {
            this.InitializeComponent();
            string selectedTheme = (string)ApplicationData.Current.LocalSettings.Values["SelectedAppTheme"];
            if(selectedTheme == null)
            {
                ThemeInput.SelectedIndex = 0;
            }
            else
            {
                switch(selectedTheme)
                {
                    case "Default":
                        ThemeInput.SelectedIndex = 0;
                        break;
                    case "Dark":
                        ThemeInput.SelectedIndex = 1;
                        break;
                    case "Light":
                        ThemeInput.SelectedIndex = 2;
                        break;
                }
            }

            // show back button
            var view = SystemNavigationManager.GetForCurrentView();
            view.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            view.BackRequested += View_BackRequested;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            signedIn = ApplicationData.Current.LocalSettings.Values["SignedIn"]?.ToString();
            if (signedIn == "Yes")
            {
                AccountPanel.Visibility = Visibility.Visible;
                await SetUserPhotoAsync();
                SignInText.Visibility = Visibility.Collapsed;
                WarningText.Visibility = Visibility.Visible;
            }
            base.OnNavigatedTo(e);
        }

        private async Task SetUserPhotoAsync()
        {
            string userName = ApplicationData.Current.LocalSettings.Values["UserName"]?.ToString();
            UserNameText.Text = $"Hey there, {userName}! You are all synced.";
            var cacheFolder = ApplicationData.Current.LocalCacheFolder;
            var accountPicFile = await cacheFolder.GetFileAsync("profile.png");
            using (IRandomAccessStream stream = await accountPicFile.OpenAsync(FileAccessMode.Read))
            {
                BitmapImage image = new BitmapImage();
                stream.Seek(0);
                await image.SetSourceAsync(stream);
                AccountPic.ProfilePicture = image;
            }
        }

        private void View_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if(Frame.CanGoBack)
            {
                Frame.GoBack();
            }

            e.Handled = true;
        }

        // Change app theme on the fly and save it 
        private void ThemeInput_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedTheme = e.AddedItems[0].ToString();
            if(selectedTheme!=null)
            {
                if(selectedTheme == "System")
                {
                    selectedTheme = "Default";
                }
                ThemeHelper.RootTheme = App.GetEnum<ElementTheme>(selectedTheme);
            }
        }

        private void TileToggle_Toggled(object sender, RoutedEventArgs e)
        {
            ApplicationData.Current.LocalSettings.Values["LiveTileOn"] = TileToggle.IsOn.ToString();
        }

        private void SignOutButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
