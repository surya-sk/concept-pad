using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using ConceptPad.Models;
using System.Collections.ObjectModel;
using ConceptPad.Saving;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System.Profile;
using Microsoft.Graph;
using System.IO;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ConceptPad.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ConceptPage : Page
    {
        private Concept concept;
        private ObservableCollection<Concept> concepts;
        Guid selectedId;
        private int conceptIndex;
        GraphServiceClient graphServiceClient;
        StorageFolder roamingFolder = ApplicationData.Current.RoamingFolder;
        string fileName = "concepts.txt";
        public ConceptPage()
        {
            this.InitializeComponent();
            ProgRing.IsActive = true;
            Task.Run(async () => { await Profile.GetInstance().ReadProfileAsync(); }).Wait();
            ProgRing.IsActive = false;
            concepts = Profile.GetInstance().GetConcepts();
            ShowBackButton();

        }

        private void ShowBackButton()
        {
            // make the back button visible
            var view = SystemNavigationManager.GetForCurrentView();
            view.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            view.BackRequested += View_BackRequested;
            string cmdLabelPref = (string)ApplicationData.Current.LocalSettings.Values["CmdBarLabels"];
            if (cmdLabelPref == null || cmdLabelPref == "No")
            {
                CmdBar.DefaultLabelPosition = CommandBarDefaultLabelPosition.Bottom;
            }
            else
            {
                CmdBar.DefaultLabelPosition = CommandBarDefaultLabelPosition.Right;
            }
        }

        private void View_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if(Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
            e.Handled = true;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            selectedId = (Guid)e.Parameter;
            foreach(Concept c in concepts)
            {
                if(selectedId == c.Id)
                {
                    concept = c;
                    conceptIndex = concepts.IndexOf(c);
                }
            }
            graphServiceClient = await Profile.GetInstance().GetGraphServiceClient();
            base.OnNavigatedTo(e);
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

        /// <summary>
        /// Download concepts and save them locally
        /// </summary>
        /// <returns></returns>
        private async Task DownloadConceptsAsync()
        {
            var search = await graphServiceClient.Me.Drive.Root.Search(fileName).Request().GetAsync();
            if (search.Count == 0)
            {
                return;
            }
            StorageFile storageFile = await roamingFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            using (Stream stream = await graphServiceClient.Me.Drive.Root.ItemWithPath(fileName).Content.Request().GetAsync())
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    await FileIO.WriteTextAsync(storageFile, sr.ReadToEnd());
                }
            }
        }

        /// <summary>
        /// If concept has been edited, save it. Navigate to main page regardless.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ProgRing.IsActive = true;
            foreach(Concept c in concepts)
            {
                if(c.Id == selectedId)
                {
                    c.Name = concept.Name;
                    c.Description = concept.Description;
                    c.Tools = concept.Tools;
                    c.Genres = concept.Genres;
                    c.Platforms = concept.Platforms;
                }
            }
            Profile.GetInstance().SaveSettings(concepts);
             await Profile.GetInstance().WriteProfileAsync();
            await UploadConceptsAsync();
            ProgRing.IsActive = false;
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
            {
                Frame.Navigate(typeof(MainPage), null, new DrillInNavigationTransitionInfo());
            }
            else
            {
                Frame.Navigate(typeof(MainPage),null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft });
            }
        }

        /// <summary>
        /// Delete a concept after confirmation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void DeleteConfirmation_Click(object sender, RoutedEventArgs e)
        {
            ProgRing.IsActive = true;
            concepts.RemoveAt(conceptIndex);
            Profile.GetInstance().SaveSettings(concepts);
            Task.Run(async () => { await Profile.GetInstance().WriteProfileAsync(); }).Wait();
            await UploadConceptsAsync();
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
            {
                Frame.Navigate(typeof(MainPage), null, new DrillInNavigationTransitionInfo());
            }
            else
            {
                Frame.Navigate(typeof(MainPage), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft });
            }
        }

        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += DataTransferManager_DataRequested;
            DataTransferManager.ShowShareUI();
        }

        private void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequest request = args.Request;
            string concpetData = $"{concept.Name}\n\n" +
                $"Summary:\n{concept.Summary}\n\n" +
                $"Description:\n{concept.Description}\n\n" +
                $"Tools:\n{concept.Tools}\n\n" +
                $"Platform(s):\n{concept.Platforms}\n\n" +
                $"Genre(s):\n {concept.Genres}\n\n" +
                $"Concept created and shared via Concept Pad. Get it free from the Microsoft Store: https://www.microsoft.com/store/apps/9N9CV4TS3VB1";
            request.Data.SetText(concpetData);
            request.Data.Properties.Title = $"Share {concept.Name}";
            request.Data.Properties.Description = "Share this concept, its description and tools.";
        }
    }
}
