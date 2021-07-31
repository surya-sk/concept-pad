using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConceptPad.Models;
using ConceptPad.Views;

namespace ConceptPad.Saving
{
    class UpdateEvent
    {
        IConceptsUpdatable MainPage;
        public UpdateEvent(IConceptsUpdatable mainPage)
        {
            this.MainPage = mainPage;
        }

        public void OnEventChanged(ObservableCollection<Concept> concepts)
        {
            this.MainPage.UpdateConcepts(concepts);
        }
    }
}
