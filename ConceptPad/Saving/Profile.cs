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
using Microsoft.Identity.Client;
using Microsoft.Graph;
using System.Net.Http.Headers;
using System.IO;

namespace ConceptPad.Saving
{
    class Profile
    {
        private static Profile instance = new Profile();
        private ObservableCollection<Concept> Concepts = null;
        StorageFolder roamingFolder = ApplicationData.Current.RoamingFolder;
        string fileName = "concepts.txt";

        private string[] scopes = new string[]
        {
             "user.read",
             "Files.Read",
             "Files.Read.All",
             "Files.ReadWrite",
             "Files.ReadWrite.All"
        };

        private const string ClientId = "fc956e5c-a8e6-47e6-80f5-be24ad571566";
        private const string Tenant = "consumers";
        private const string Authority = "https://login.microsoftonline.com/" + Tenant;

        private static IPublicClientApplication PublicClientApp;

        private static readonly string MSGraphURL = "https://graph.microsoft.com/v1.0/";
        private static AuthenticationResult authResult;
        private static GraphServiceClient graphServiceClient = null;

        private Profile()
        {
        }

        public static Profile GetInstance()
        {
            return instance;
        }

        /// <summary>
        /// Sign the user in and return the access token
        /// </summary>
        /// <param name="scopes"></param>
        /// <returns></returns>
        private static async Task<string> SignInUserAndGetTokenUsingMSAL(string[] scopes)
        {
            PublicClientApp = PublicClientApplicationBuilder.Create(ClientId)
                 .WithAuthority(Authority)
                 .WithUseCorporateNetwork(false)
                 .WithRedirectUri("https://login.microsoftonline.com/common/oauth2/nativeclient")
                 .WithLogging((level, message, containsPii) =>
                 {
                     Debug.WriteLine($"MSAL: {level} {message} ");
                 }, LogLevel.Warning, enablePiiLogging: false, enableDefaultPlatformLogging: true)
                 .Build();

            var accounts = await PublicClientApp.GetAccountsAsync().ConfigureAwait(false);
            var firstAccount = accounts.FirstOrDefault();

            try
            {
                authResult = await PublicClientApp.AcquireTokenSilent(scopes, firstAccount).ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                // A MsalUiRequiredException happened on AcquireTokenSilentAsync. This indicates you need to call AcquireTokenAsync to acquire a token
                Debug.WriteLine($"MsalUiRequiredException: {ex.Message}");
                authResult = await PublicClientApp.AcquireTokenInteractive(scopes).ExecuteAsync().ConfigureAwait(false);

            }
            return authResult.AccessToken;
        }

        /// <summary>
        /// Initialize graph service client 
        /// </summary>
        /// <param name="scopes"></param>
        /// <returns></returns>
        private async static Task<GraphServiceClient> SignInAndInitializeGraphServiceClient(string[] scopes)
        {
            GraphServiceClient graphClient = new GraphServiceClient(MSGraphURL,
            new DelegateAuthenticationProvider(async (requestMessage) => {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", await SignInUserAndGetTokenUsingMSAL(scopes));
            }));

            return await Task.FromResult(graphClient);
        }

        public async Task<GraphServiceClient> GetGraphServiceClient()
        {
            if(graphServiceClient == null)
            {
                await SignInAndInitializeGraphServiceClient(scopes);
            }
            return graphServiceClient;
        }

        /// <summary>
        /// Uploads concepts to OneDrive
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <returns></returns>
        public async Task UploadConceptsAsync()
        {
            if (graphServiceClient is null)
            {
                return;
            }
            StorageFile storageFile = await roamingFolder.GetFileAsync(fileName);
            using (var stream = await storageFile.OpenStreamForWriteAsync())
            {
                await graphServiceClient.Me.Drive.Root.ItemWithPath(fileName).Content.Request().PutAsync<DriveItem>(stream);
            }
        }

        /// <summary>
        /// Downloads concepts from OneDrive and saves them locally
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <returns></returns>
        public async Task DownloadConceptsAsync()
        {
            if(graphServiceClient == null)
            {
                try
                {
                    graphServiceClient = await SignInAndInitializeGraphServiceClient(scopes);
                }
                catch
                {
                    return;
                }
            }
            var search = graphServiceClient.Me.Drive.Root.Search(fileName).Request().GetAsync().Result;
            if (search.Count == 0)
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
