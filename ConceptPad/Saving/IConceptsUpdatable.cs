using ConceptPad.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConceptPad.Saving
{
    interface IConceptsUpdatable
    {
        void UpdateConcepts(ObservableCollection<Concept> concepts); 
    }
}
