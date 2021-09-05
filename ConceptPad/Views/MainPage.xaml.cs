using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using ConceptPad.Models;
using muxc = Microsoft.UI.Xaml.Controls;
using ConceptPad.Saving;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Animation;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using Windows.Storage;
using Windows.UI.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.System.Profile;
using System.Diagnostics;
using Microsoft.Graph;
using System.IO;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using System.Net.NetworkInformation;
using Newtonsoft.Json;
using System.Threading;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ConceptPad.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>

    public sealed partial class MainPage : Page
    {
        private ObservableCollection<Concept> concepts { get; set; }
        private string type;
        StorageFolder roamingFolder = ApplicationData.Current.RoamingFolder;
        string fileName = "concepts.txt";
        GraphServiceClient graphServiceClient;
        bool isNetworkAvailable = false;

        public MainPage()
        {
            this.InitializeComponent();
            isNetworkAvailable = NetworkInterface.GetIsNetworkAvailable();
            Task.Run(async () => { await Profile.GetInstance().ReadProfileAsync(); }).Wait();
            ObservableCollection<Concept> readConcepts = Profile.GetInstance().GetConcepts();
            concepts= new ObservableCollection<Concept>(readConcepts.OrderByDescending(c => c.DateCreated)); // sort by last created
            InitUIPrefs();

            UpdateNotificationQueue();
        }

        /// <summary>
        /// Downloads concepts from OneDrive and updates the concepts list
        /// </summary>
        /// <param name="_sender"></param>
        /// <param name="e"></param>
        protected async  override void OnNavigatedTo(NavigationEventArgs e)
        {
            string signedIn = ApplicationData.Current.LocalSettings.Values["SignedIn"]?.ToString();
            if (isNetworkAvailable && signedIn == "Yes")
            {
                SignInButton.Visibility = Visibility.Collapsed;
                ProfileButton.Visibility = Visibility.Visible;
                graphServiceClient = await Profile.GetInstance().GetGraphServiceClient();
                ProgBar.Visibility = Visibility.Visible;
                await SyncConceptsAsync();
                await Profile.GetInstance().ReadProfileAsync();
                ObservableCollection<Concept> readConcepts = Profile.GetInstance().GetConcepts();
                var _concepts = new ObservableCollection<Concept>(readConcepts.OrderByDescending(c => c.DateCreated)); // sort by last created
                concepts.Clear();
                EmtpyListText.Visibility = Visibility.Collapsed;
                foreach (var c in _concepts)
                {
                    concepts.Add(c);
                }
                SetImagePath();
            }
            ProgBar.Visibility = Visibility.Collapsed;
            base.OnNavigatedTo(e);
        }

        private void SyncButton_Click(object sender, RoutedEventArgs e)
        {
            ProgBar.Visibility = Visibility.Visible;
            Frame.Navigate(typeof(MainPage));
        }

        /// <summary>
        /// Sync concepts with the cloud 
        /// </summary>
        /// <returns></returns>
        private async Task SyncConceptsAsync()
        {
            ObservableCollection<Concept> localConcepts = Profile.GetInstance().GetConcepts();
            ObservableCollection<Concept> cloudConcepts = await DownloadConceptsAsync();
            if (localConcepts.Count == cloudConcepts.Count)
            {
                string changeStatus = ApplicationData.Current.LocalSettings.Values["ChangeStatus"]?.ToString();
                if (changeStatus != null && changeStatus == "changed")
                {
                    concepts = localConcepts;
                    Profile.GetInstance().SaveSettings(concepts);
                    Task.Run(async () => { await Profile.GetInstance().WriteProfileAsync(); }).Wait();
                    await UploadConceptsAsync();
                    ApplicationData.Current.LocalSettings.Values["ChangeStatus"] = "none";
                }
                else
                    concepts = cloudConcepts;
            }
            else
            {
                concepts = cloudConcepts;
                if (localConcepts.Count != 0)
                {
                    string[] createdConceptsIDs = ApplicationData.Current.LocalSettings.Values["CreatedConcepts"]?.ToString().Split(',');
                    var extraLocalConcepts = localConcepts.Except(cloudConcepts);
                    if (extraLocalConcepts != null && extraLocalConcepts.Count() > 0)
                    {
                        foreach (Concept c in extraLocalConcepts)
                        {
                            if (createdConceptsIDs.Contains(c.Id.ToString()))
                            {
                                concepts.Add(c);
                            }
                        }
                    }
                    ApplicationData.Current.LocalSettings.Values["CreatedConcepts"] = string.Empty;
                }
                Profile.GetInstance().SaveSettings(concepts);
                Task.Run(async () => { await Profile.GetInstance().WriteProfileAsync(); }).Wait();
                await UploadConceptsAsync();
            }
        }

        /// <summary>
        /// Download concepts and save them locally
        /// </summary>
        /// <returns></returns>
        private async Task<ObservableCollection<Concept>> DownloadConceptsAsync()
        {
            var search = await graphServiceClient.Me.Drive.Root.Search(fileName).Request().GetAsync();
            if (search.Count == 0)
            {
                return null;
            }
            using (Stream stream = await graphServiceClient.Me.Drive.Root.ItemWithPath(fileName).Content.Request().GetAsync())
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    string json = sr.ReadToEnd();
                    return JsonConvert.DeserializeObject<ObservableCollection<Concept>>(json);
                }
            }
        }

        /// <summary>
        /// Upload concepts to OneDrive
        /// </summary>
        /// <returns></returns>
        public async Task UploadConceptsAsync()
        {
            if (graphServiceClient is null)
            {
                return;
            }
            StorageFile storageFile = await roamingFolder.GetFileAsync(fileName);
            using (var stream = await storageFile.OpenStreamForWriteAsync())
            {
                await graphServiceClient.Me.Drive.Root.ItemWithPath(fileName).Content.Request().PutAsync<DriveItem>(stream);
            }
        }

        private void UpdateNotificationQueue()
        {
            // Set tile notification queue
            TileUpdateManager.CreateTileUpdaterForApplication().EnableNotificationQueue(true);
            string showLiveTile = ApplicationData.Current.LocalSettings.Values["LiveTileOn"]?.ToString();
            if (showLiveTile == null || showLiveTile == "True")
            {
                foreach (Concept c in concepts)
                {
                    UpdateLiveTile(c);
                }
            }
        }

        private void InitUIPrefs()
        {
            SetImagePath();

            string cmdLabelPref = (string)ApplicationData.Current.LocalSettings.Values["CmdBarLabels"];
            if (cmdLabelPref == null || cmdLabelPref == "No")
            {
                CmdBar.DefaultLabelPosition = CommandBarDefaultLabelPosition.Bottom;
            }
            else
            {
                CmdBar.DefaultLabelPosition = CommandBarDefaultLabelPosition.Right;
            }

            // disable back button
            var view = SystemNavigationManager.GetForCurrentView();
            view.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Disabled;


            if (concepts.Count == 0)
            {
                EmtpyListText.Visibility = Visibility.Visible;
            }
        }

        private void SetImagePath()
        {
            string theme = GetTheme();
            foreach (Concept c in concepts)
            {
                c.ImagePath = $@"ms-appx:///Assets/{c.Type.ToLower()}-{theme}.png";
            }
        }


        /// <summary>
        /// Get the current app theme. Used for choosing the right icons
        /// </summary>
        /// <returns>a string representing current app theme</returns>
        private static string GetTheme()
        {
            string savedTheme = ApplicationData.Current.LocalSettings.Values["SelectedAppTheme"]?.ToString();
            string theme = string.Empty;
            if (savedTheme == null || savedTheme == "Default")
            {
                var defautlTheme = new Windows.UI.ViewManagement.UISettings();
                var uiTheme = defautlTheme.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background).ToString();
                if (uiTheme.Equals("#FF000000"))
                {
                    savedTheme = "Dark";
                }
                else
                {
                    savedTheme = "Light";
                }
            }
            theme = savedTheme;

            return theme.ToLower();
        }

        private void RadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(TypeButtons != null && sender is muxc.RadioButtons rb)
            {
                type = rb.SelectedItem as string;
                switch(type)
                {
                    case "Game":
                        ToolsText.Text = "Engine";
                        break;
                    case "App":
                        ToolsText.Text = "Framework";
                        break;
                }
            }
        }

        private async void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(NameInput.Text) || string.IsNullOrEmpty(DescriptionInput.Text) || string.IsNullOrEmpty(ToolsInput.Text) || string.IsNullOrEmpty(GenresInput.Text) || string.IsNullOrEmpty(PlatformsInput.Text))
            {
                ShowInvalidParamDialog("Insufficient Paramenters", "Please fill in all the fields");
            }
            else
            {
                await CreateAndAddConcept();
                Frame.Navigate(typeof(MainPage));
            }
        }

        private async void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            string signedIn = ApplicationData.Current.LocalSettings.Values["SignedIn"]?.ToString();
            if (isNetworkAvailable)
            {
                if(signedIn != "Yes")
                {
                    graphServiceClient = await Profile.GetInstance().GetGraphServiceClient();
                    ApplicationData.Current.LocalSettings.Values["SignedIn"] = "Yes";
                    Frame.Navigate(typeof(MainPage));
                }
            }
            else
            {
                ContentDialog contentDialog = new ContentDialog
                {
                    Title = "No Internet",
                    Content = "You need to be connected to sign-in",
                    CloseButtonText = "Ok"
                };
                ContentDialogResult result = await contentDialog.ShowAsync();
            }
        }

        /// <summary>
        /// Create a concept and add it to the list, then write it to the save file
        /// </summary>
        private async Task CreateAndAddConcept()
        {
            Concept concept = new Concept()
            {
                Id = Guid.NewGuid(),
                Name = NameInput.Text,
                Summary = SummaryInput.Text,
                Description = DescriptionInput.Text,
                Type = type,
                Tools = ToolsInput.Text,
                Genres = GenresInput.Text,
                Platforms = PlatformsInput.Text,
                DateCreated = DateTime.Now.ToString("D")
            };
            concepts.Add(concept);
            Profile.GetInstance().SaveSettings(concepts);
            ProgBar.Visibility = Visibility.Visible;
            await Profile.GetInstance().WriteProfileAsync();
            if (isNetworkAvailable)
                await UploadConceptsAsync();
            else
                ApplicationData.Current.LocalSettings.Values["CreatedConcepts"] += $"{concept.Id},";
            ProgBar.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Show content dialog if all the fields are not filled in
        /// </summary>
        private async void ShowInvalidParamDialog(string title, string content)
        {
            ContentDialog contentDialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "Ok"
            };
            ContentDialogResult result = await contentDialog.ShowAsync();
        }

        private void ConceptView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var selectedConcept = (Concept)e.ClickedItem;
            if(AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
            {
                Frame.Navigate(typeof(ConceptPage), selectedConcept.Id, new DrillInNavigationTransitionInfo());
            }
            else
            {
                Frame.Navigate(typeof(ConceptPage), selectedConcept.Id, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        { 
            Frame.Navigate(typeof(SettingsPage), null, new DrillInNavigationTransitionInfo());
        }

        /// <summary>
        /// Update the live tile to show concepts based on the notification queue
        /// </summary>
        /// <param name="c"></param>
        private void UpdateLiveTile(Concept c)
        {
            var tileContent = new TileContent()
            {
                Visual = new TileVisual()
                {

                    TileMedium = new TileBinding()
                    {
                        Branding = TileBranding.Name,
                        DisplayName = "Concept Pad",
                        Content = new TileBindingContentAdaptive()
                        {
                            BackgroundImage = new TileBackgroundImage()
                            {
                                Source = $@"ms-appx:///Assets/{c.Type.ToLower()}-light.png",
                                HintOverlay = 80
                            },
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = c.Name,
                                    HintWrap = true,
                                    HintMaxLines = 2
                                },
                                new AdaptiveText()
                                {
                                    Text = c.Type,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                },
                                new AdaptiveText()
                                {
                                    Text = c.Genres,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                }
                            }
                        }
                    },
                    TileWide = new TileBinding()
                    {
                        Branding = TileBranding.NameAndLogo,
                        DisplayName = "Concept Pad",
                        Content = new TileBindingContentAdaptive()
                        {
                            BackgroundImage = new TileBackgroundImage()
                            {
                                Source = $@"ms-appx:///Assets/{c.Type.ToLower()}-light.png",
                                HintOverlay = 80
                            },
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = c.Name
                                },
                                new AdaptiveText()
                                {
                                    Text = $"{c.Tools} - {c.Genres}",
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle,
                                    HintWrap = true
                                },
                                new AdaptiveText()
                                {
                                    Text = $"{c.Summary}",
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle,
                                    HintWrap = true
                                }
                            }
                        }
                    },
                }
            };

            // Create the tile notification
            var tileNotif = new TileNotification(tileContent.GetXml());

            // And send the notification to the primary tile
            TileUpdateManager.CreateTileUpdaterForApplication().Update(tileNotif);
        }

        private async void RateButton_Click(object sender, RoutedEventArgs e)
        {
            var ratingUri = new Uri(@"ms-windows-store://review/?ProductId=9N9CV4TS3VB1");
            await Windows.System.Launcher.LaunchUriAsync(ratingUri);
        }
    }
}
