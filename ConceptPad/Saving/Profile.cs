using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConceptPad.Models;
using Windows.Storage;
using Newtonsoft.Json;
using System.Diagnostics;

namespace ConceptPad.Saving
{
    class Profile
    {
        private static Profile instance;
        private ObservableCollection<Concept> Concepts = null;
        ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
        StorageFolder roamingFolder = ApplicationData.Current.RoamingFolder;
        string fileName = "concepts.txt";

        private Profile()
        {
        }

        public static Profile GetInstance()
        {
            if(instance == null)
            {
                instance = new Profile();
            }
            return instance;
        }

        void InitHandlers()
        {
            ApplicationData.Current.DataChanged += new Windows.Foundation.TypedEventHandler<ApplicationData, object>(DataChangeHandler);
        }

        void DataChangeHandler(ApplicationData appData, object e)
        {
            SaveSettings(Concepts);
        }

        public async void WriteProfile()
        {
            string json = JsonConvert.SerializeObject(Concepts);
            StorageFile storageFile = await roamingFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(storageFile, json);
        }

        public void SaveSettings(ObservableCollection<Concept> concepts)
        {
            Concepts = concepts;
        }

        public async void ReadProfile()
        {
            try
            {
                StorageFile storageFile = await roamingFolder.GetFileAsync(fileName);
                string json = await FileIO.ReadTextAsync(storageFile);
                Concepts = JsonConvert.DeserializeObject<ObservableCollection<Concept>>(json);
            }
            catch
            {
                Concepts = new ObservableCollection<Concept>();
            }
        }

        public ObservableCollection<Concept> GetConcepts()
        {
            return Concepts;
        }

        public void AddConcept(Concept concept)
        {
            Concepts.Add(concept);
            WriteProfile();
        }
    }
}
