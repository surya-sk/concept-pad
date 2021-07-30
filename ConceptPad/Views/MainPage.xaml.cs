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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ConceptPad.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    
    public sealed partial class MainPage : Page
    {
        private ObservableCollection<Concept> concepts;
        private Concept concept;
        public MainPage()
        {
            concepts = new ObservableCollection<Concept>();
            concept = new Concept();
            concepts.Add(new Concept { Name = "Name", Type ="Game", Description = "Description", DateCreated = DateTime.Now, IsInProduction = false});
            concepts.Add(new Concept { Name = "Name2", Type ="Game2", Description = "Description", DateCreated = DateTime.Now, IsInProduction = false});
            concepts.Add(new Concept { Name = "Name3", Type ="Game3", Description = "Description", DateCreated = DateTime.Now, IsInProduction = false});
            concepts.Add(new Concept { Name = "Name3", Type ="Game3", Description = "Description", DateCreated = DateTime.Now, IsInProduction = false});
            concepts.Add(new Concept { Name = "Name3", Type ="Game3", Description = "Description", DateCreated = DateTime.Now, IsInProduction = false});
            this.InitializeComponent();
        }

        private void RadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(TypeButtons != null && sender is muxc.RadioButtons rb)
            {
                string type = rb.SelectedItem as string;
                concept.Type = type;
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
                concept.Name = NameInput.Text;
                concept.Description = DescriptionInput.Text;
                concept.Tools = ToolsInput.Text;
                concept.DateCreated = DateTime.Now();
                concepts.Add(concept);
                ClearInputs();
            }
        }

        private void ClearInputs()
        {
            NameInput.Text = string.Empty;
            DescriptionInput.Text = string.Empty;
            ToolsInput.Text = string.Empty;
        }
    }
}
