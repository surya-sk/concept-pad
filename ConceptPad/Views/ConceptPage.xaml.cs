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
using System.Net.NetworkInformation;
using System.Diagnostics;

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
        private string signedIn;
        Guid selectedId;
        private int conceptIndex;
        GraphServiceClient graphServiceClient;
        bool isNetworkAvailable = NetworkInterface.GetIsNetworkAvailable();
        public ConceptPage()
        {
            this.InitializeComponent();
            Task.Run(async () => { await Profile.GetInstance().ReadProfileAsync(); }).Wait();
            concepts = Profile.GetInstance().GetConcepts();
            signedIn = ApplicationData.Current.LocalSettings.Values["SignedIn"]?.ToString();
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
            string signedIn = ApplicationData.Current.LocalSettings.Values["SignedIn"]?.ToString();
            if (isNetworkAvailable && signedIn == "Yes")
                graphServiceClient = await Profile.GetInstance().GetGraphServiceClient();
            base.OnNavigatedTo(e);
        }

        /// <summary>
        /// If concept has been edited, save it. Navigate to main page regardless.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ProgBar.Visibility = Visibility.Visible;
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
            if(isNetworkAvailable)
                await Profile.GetInstance().WriteProfileAsync(signedIn=="Yes");
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
            {
                Frame.Navigate(typeof(MainPage), null, new DrillInNavigationTransitionInfo());
            }
            else
            {
                Frame.Navigate(typeof(MainPage),null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft });
            }
        }
        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog deleteDialog = new ContentDialog
            {
                Title = "Delete concept?",
                Content = "Deletion is permanent. This concept cannot be recovered, and will be gone forever.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel"
            };
            ContentDialogResult result = await deleteDialog.ShowAsync();
            if(result == ContentDialogResult.Primary)
            {
                await DeleteConfirmation_Click();
            }
        }

        /// <summary>
        /// Delete a concept after confirmation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task DeleteConfirmation_Click()
        {
            ProgBar.Visibility = Visibility.Visible;
            concepts.RemoveAt(conceptIndex);
            Profile.GetInstance().SaveSettings(concepts);
            Task.Run(async () => { await Profile.GetInstance().WriteProfileAsync(); }).Wait();
            string signedIn = ApplicationData.Current.LocalSettings.Values["SignedIn"]?.ToString();
            if (isNetworkAvailable && signedIn == "Yes")
                await Profile.GetInstance().WriteProfileAsync(signedIn == "Yes");
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

        private void TitleEditBox_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            ApplicationData.Current.LocalSettings.Values["ChangeStatus"] = "changed";
        }
    }
}
