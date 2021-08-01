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
        public Guid Id { get; set; }
        public string ImagePath { get; set; }
        private string name;
        private string type;
        private string description;
        private string dateCreated;
        private string tools;
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

        public string DateCreated
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
        
        public string Tools
        {
            get => tools;
            set
            {
                tools = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Tools)));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
