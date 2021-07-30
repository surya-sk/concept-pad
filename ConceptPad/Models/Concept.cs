using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConceptPad.Models
{
    public class Concept
    {
        // Attributes
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string Framework { get; set; }
        public DateTime TimeCreated { get; set; }
        public bool InProduction { get; set; }

        public Concept()
        {
        }
    }
}
