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
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Core;

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
        bool edited = false;
        private int conceptIndex;
        public ConceptPage()
        {
            this.InitializeComponent();
            ProgRing.IsActive = true;
            Task.Run(async () => { await Profile.GetInstance().ReadProfileAsync(); }).Wait();
            ProgRing.IsActive = false;
            concepts = Profile.GetInstance().GetConcepts();
            var view = SystemNavigationManager.GetForCurrentView();
            view.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            view.BackRequested += View_BackRequested;

        }

        private void View_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if(Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
            e.Handled = true;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
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
            base.OnNavigatedTo(e);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ProgRing.IsActive = true;
            if(edited)
            {
                foreach(Concept c in concepts)
                {
                    if(c.Id == selectedId)
                    {
                        c.Name = concept.Name;
                        c.Description = concept.Description;
                        c.Tools = concept.Tools;
                    }
                }
                Profile.GetInstance().SaveSettings(concepts);
                Task.Run(async () => { await Profile.GetInstance().WriteProfileAsync(); }).Wait();
            }
            ProgRing.IsActive = false;
            Frame.Navigate(typeof(MainPage), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft });
        }

        private void TitleEditBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            edited = true;
        }

        private void ToolsEditBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            edited = true;
        }

        private void DescriptionEditBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            edited = true;
        }

        private void DeleteConfirmation_Click(object sender, RoutedEventArgs e)
        {
            ProgRing.IsActive = true;
            concepts.RemoveAt(conceptIndex);
            Profile.GetInstance().SaveSettings(concepts);
            Task.Run(async () => { await Profile.GetInstance().WriteProfileAsync(); }).Wait();
            Frame.Navigate(typeof(MainPage), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft });
        }
    }
}
