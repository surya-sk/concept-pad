using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using ConceptPad.Models;
using muxc = Microsoft.UI.Xaml.Controls;
using System.Diagnostics;
using ConceptPad.Saving;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Animation;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using Windows.Storage;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ConceptPad.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    
    public sealed partial class MainPage : Page
    {
        private ObservableCollection<Concept> concepts;
        private string type;
        public MainPage()
        {
            Task.Run(async () => { await Profile.GetInstance().ReadProfileAsync(); }).Wait();
            ObservableCollection<Concept> readConcepts = Profile.GetInstance().GetConcepts();
            concepts = new ObservableCollection<Concept>(readConcepts.OrderByDescending(c => c.DateCreated));
            foreach(Concept c in concepts)
            {
                c.ImagePath = $@"ms-appx:///Assets/{c.Type.ToLower()}.png";
            }
            this.InitializeComponent();
            TileUpdateManager.CreateTileUpdaterForApplication().EnableNotificationQueue(true);
            string showLiveTile = ApplicationData.Current.LocalSettings.Values["LiveTileOn"]?.ToString();
            if (showLiveTile==null || showLiveTile == "True")
            {
                foreach (Concept c in concepts)
                {
                    UpdateLiveTile(c);
                }
            }

            if(concepts.Count == 0)
            {
                EmtpyListText.Visibility = Visibility.Visible;
            }
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

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(NameInput.Text) || string.IsNullOrEmpty(DescriptionInput.Text) || string.IsNullOrEmpty(ToolsInput.Text))
            {
                ErrorText.Visibility = Visibility.Visible;
            }
            else
            {
                Concept concept = new Concept()
                {
                    Id = Guid.NewGuid(),
                    Name = NameInput.Text,
                    Description = DescriptionInput.Text,
                    Type = type,
                    Tools = ToolsInput.Text,
                    DateCreated = DateTime.Now.ToString("D")
                };
                concepts.Add(concept);
                Profile.GetInstance().SaveSettings(concepts);
                ProgRing.IsActive = true;
                Task.Run(async () => { await Profile.GetInstance().WriteProfileAsync(); }).Wait();
                ProgRing.IsActive = false;
                Frame.Navigate(typeof(MainPage));
            }
        }


        private void ConceptView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var selectedConcept = (Concept)e.ClickedItem;
            Frame.Navigate(typeof(ConceptPage), selectedConcept.Id, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            ProgRing.IsActive = true;
            Frame.Navigate(typeof(MainPage));
        }

        private void PinButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void UnpinButotn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsPage), null, new DrillInNavigationTransitionInfo());
        }

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
                                    Text = c.Tools,
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
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = c.Name
                                },
                                new AdaptiveText()
                                {
                                    Text = c.Type,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                },
                                new AdaptiveText()
                                {
                                    Text = c.Description,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle,
                                    HintMinLines = 2,
                                    HintMaxLines = 4,
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
    }
}
