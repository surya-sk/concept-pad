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
using System.Threading;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;

namespace ConceptPad.Saving
{
    class Profile
    {
        private static Profile instance = new Profile();
        private ObservableCollection<Concept> Concepts = null;
        StorageFolder localFolder = ApplicationData.Current.RoamingFolder;
        string fileName = "concepts.txt";
        StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
        string accountPicFile = "profile.png";

        private static string[] scopes = new string[]
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

        public async Task SignOut()
        {
            IEnumerable<IAccount> accounts = await PublicClientApp.GetAccountsAsync().ConfigureAwait(false);
            IAccount firstAccount = accounts.FirstOrDefault();
            try
            {
                await PublicClientApp.RemoveAsync(firstAccount).ConfigureAwait(false);
                ApplicationData.Current.LocalSettings.Values["SignedIn"] = "No";
                var file = await localFolder.GetFileAsync(fileName);
                await file.DeleteAsync(StorageDeleteOption.Default);
            }
            catch (Exception ex)
            {
                //await Utils.Logger.WriteLogAsync($"Error signing out user: {ex.Message}");
            }
        }

        public async Task<GraphServiceClient> GetGraphServiceClient()
        {
            if (graphServiceClient == null)
            {
                graphServiceClient = await SignInAndInitializeGraphServiceClient(scopes);
                var user = await graphServiceClient.Me.Request().GetAsync();
                Stream photoresponse = await graphServiceClient.Me.Photo.Content.Request().GetAsync();
                ApplicationData.Current.LocalSettings.Values["UserName"] = user.GivenName;
                if (photoresponse != null)
                {
                    using (var randomAccessStream = photoresponse.AsRandomAccessStream())
                    {
                        BitmapImage image = new BitmapImage();
                        randomAccessStream.Seek(0);
                        await image.SetSourceAsync(randomAccessStream);

                        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
                        SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();
                        var storageFile = await cacheFolder.CreateFileAsync(accountPicFile, CreationCollisionOption.ReplaceExisting);
                        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, await storageFile.OpenAsync(FileAccessMode.ReadWrite));
                        encoder.SetSoftwareBitmap(softwareBitmap);
                        await encoder.FlushAsync();
                    }
                }
            }
            return graphServiceClient;
        }

        /// <summary>
        /// Write the concept list in JSON format
        /// </summary>
        /// <returns></returns>
        public async Task WriteProfileAsync(bool sync=false)
        {
            string json = JsonConvert.SerializeObject(Concepts);
            StorageFile storageFile = await localFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(storageFile, json);
            if (sync)
            {
                if (graphServiceClient is null)
                {
                    return;
                }
                using (var stream = await storageFile.OpenStreamForWriteAsync())
                {
                    await graphServiceClient.Me.Drive.Root.ItemWithPath(fileName).Content.Request().PutAsync<DriveItem>(stream);
                }
            }
        }

        public void SaveSettings(ObservableCollection<Concept> concepts)
        {
            Concepts = concepts;
        }

        /// <summary>
        /// Read the concept list in JSON and deserialze it 
        /// </summary>
        /// <returns></returns>
        public async Task ReadProfileAsync(bool sync = false)
        {
            try
            {
                if (sync)
                {
                    string jsonDownload = await DownloadConceptsJsonAsync();
                    if (jsonDownload != null)
                    {
                        StorageFile file = await localFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                        await FileIO.WriteTextAsync(file, jsonDownload);
                    }
                }
                StorageFile storageFile = await localFolder.GetFileAsync(fileName);
                string json = await FileIO.ReadTextAsync(storageFile);
                Concepts = JsonConvert.DeserializeObject<ObservableCollection<Concept>>(json);
            }
            catch
            {
                Concepts = new ObservableCollection<Concept>();
            }
        }

        private async Task<string> DownloadConceptsJsonAsync()
        {
            var search = await graphServiceClient.Me.Drive.Root.Search(fileName).Request().GetAsync();
            if (search.Count == 0)
            {
                return null;
            }
            using (Stream stream = await graphServiceClient.Me.Drive.Root.ItemWithPath(fileName).Content.Request().GetAsync())
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    string json = sr.ReadToEnd();
                    return json;
                }
            }
        }

        public async Task DeleteLocalFileAsync()
        {
            StorageFile file = await localFolder.GetFileAsync(fileName);
            await file.DeleteAsync(StorageDeleteOption.Default);
        }

        public ObservableCollection<Concept> GetConcepts()
        {
            return Concepts;
        }
    }
}
