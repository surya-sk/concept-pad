using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConceptPad.Models
{
    /// <summary>
    /// A model of a game or app concept
    /// </summary>
    public class Concept : INotifyPropertyChanged
    {
        // Attributes
        private string name;
        private string type;
        private string description;
        private DateTime dateCreated;
        private bool isInProduction;

        public string Name
        {
            get => name;
            set
            {
                name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        public string Type
        {
            get => type;
            set
            {
                type = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Type)));
            }
        }

        public string Description
        {
            get => description;
            set
            {
                description = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Description)));
            }
        }

        public DateTime DateCreated
        {
            get => dateCreated;
            set
            {
                dateCreated = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DateCreated)));
            }
        }

        public bool IsInProduction
        {
            get => isInProduction;
            set
            {
                isInProduction = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsInProduction)));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
