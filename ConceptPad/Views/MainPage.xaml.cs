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
            this.InitializeComponent();
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
                    Tools = ToolsInput.Text
                };
                concepts.Add(concept);
                Profile.GetInstance().SaveSettings(concepts);
                Profile.GetInstance().WriteProfileAsync();
                ClearInputs();
            }
        }

        private void ClearInputs()
        {
            NameInput.Text = string.Empty;
            DescriptionInput.Text = string.Empty;
            ToolsInput.Text = string.Empty;
        }

        private void ConceptView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var selectedConcept = (Concept)e.ClickedItem;
            Frame.Navigate(typeof(ConceptPage), selectedConcept.Id);
        }
    }
}
