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
        private static Profile instance = new Profile();
        private ObservableCollection<Concept> Concepts = null;
        ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
        StorageFolder roamingFolder = ApplicationData.Current.RoamingFolder;
        string fileName = "concepts.txt";

        private Profile()
        {
            Windows.Storage.ApplicationData.Current.DataChanged += Current_DataChanged;
        }

        private void Current_DataChanged(ApplicationData sender, object args)
        {
            Task.Run(async () => { await GetInstance().ReadProfileAsync(); }).Wait();
        }

        public static Profile GetInstance()
        {
            return instance;
        }

        /// <summary>
        /// Write the concept list in JSON format
        /// </summary>
        /// <returns></returns>
        public async Task WriteProfileAsync()
        {
            string json = JsonConvert.SerializeObject(Concepts);
            StorageFile storageFile = await roamingFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(storageFile, json);
        }

        public void SaveSettings(ObservableCollection<Concept> concepts)
        {
            Concepts = concepts;
        }

        /// <summary>
        /// Read the concept list in JSON and deserialze it 
        /// </summary>
        /// <returns></returns>
        public async Task ReadProfileAsync()
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
    }
}
