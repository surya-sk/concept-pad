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
using Microsoft.Graph;
using System.IO;

namespace ConceptPad.Saving
{
    class Profile
    {
        private static Profile instance = new Profile();
        private ObservableCollection<Concept> Concepts = null;
        StorageFolder roamingFolder = ApplicationData.Current.RoamingFolder;
        string fileName = "concepts.txt";

        private Profile()
        {
        }

        public static Profile GetInstance()
        {
            return instance;
        }

        /// <summary>
        /// Uploads concepts to OneDrive
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <returns></returns>
        public async Task UploadConceptsAsync(GraphServiceClient graphServiceClient)
        {
            StorageFile storageFile = await roamingFolder.GetFileAsync(fileName);
            using(var stream = await storageFile.OpenStreamForWriteAsync())
            {
                await graphServiceClient.Me.Drive.Root.ItemWithPath(fileName).Content.Request().PutAsync<DriveItem>(stream);
            }
        }

        /// <summary>
        /// Downloads concepts from OneDrive and saves them locally
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <returns></returns>
        public async Task DownloadConceptsAsync(GraphServiceClient graphServiceClient)
        {
            var search = graphServiceClient.Me.Drive.Root.Search(fileName).Request().GetAsync().Result;
            if(search.Count == 0)
            {
                return;
            }
            StorageFile storageFile = await roamingFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            DriveItem driveItem = await graphServiceClient.Me.Drive.Root.ItemWithPath(fileName).Request().GetAsync();
            if (driveItem == null)
            {
                return;
            }
            using (Stream stream = driveItem.Content)
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    await FileIO.WriteTextAsync(storageFile, sr.ReadToEnd());
                }
            }
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
