using System;
using System.Collections.Generic;
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
using System.Diagnostics;
using System.Collections.ObjectModel;
using ConceptPad.Saving;

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
        public ConceptPage()
        {
            Profile.GetInstance().ReadProfile();
            concepts = Profile.GetInstance().GetConcepts();
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Guid selectedId = (Guid)e.Parameter;
            foreach(Concept c in concepts)
            {
                if(selectedId == c.Id)
                {
                    concept = c;
                }
            }
            base.OnNavigatedTo(e);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            SaveButton.Visibility = Visibility.Visible;
            EditButton.Visibility = Visibility.Collapsed;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage));
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
