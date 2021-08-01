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
            concepts = Profile.GetInstance().GetConcepts();
            foreach(Concept c in concepts)
            {
                c.ImagePath = $@"ms-appx:///Assets/{c.Type.ToLower()}.png";
            }
            this.InitializeComponent();
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
            Frame.Navigate(typeof(ConceptPage), selectedConcept.Id, new DrillInNavigationTransitionInfo());
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
            Frame.Navigate(typeof(SettingsPage), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
        }
    }
}
