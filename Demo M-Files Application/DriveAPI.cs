using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using MFilesAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Upload;
using Google.Apis.Util;
using HeyRed.Mime;
using Google.Apis.Util.Store;
using static Google.Apis.Drive.v3.DriveService;
using System.Threading;
using Google.Apis.Auth.OAuth2.Flows;
using System.Runtime.InteropServices.ComTypes;

namespace Demo_M_Files_Application
{
    public static class DriveAPI
    {
        static string rootDir = "C:\\Development\\Demo M-Files Application\\Demo M-Files Application";

        static string[] Scopes = { 
            DriveService.Scope.Drive, 
            DriveService.Scope.DriveFile 
        };
        static string ApplicationName = "M-FILES";

        public static DriveService GetService()
        {
            ClientSecrets secrets;
            using (var stream = new FileStream($"{rootDir}\\client-secret.json", FileMode.Open, FileAccess.Read))
            {
                secrets = GoogleClientSecrets.FromStream(stream).Secrets;
            }

            // Set up the authorization code flow
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = secrets,
                Scopes = new[] { DriveService.Scope.Drive },
                DataStore = new FileDataStore("DriveApiSample")
            });

            // Construct the authorization request URL
            var state = Guid.NewGuid().ToString("N");
            var authUrl = flow.CreateAuthorizationCodeRequest("urn:ietf:wg:oauth:2.0:oob").Build().AbsoluteUri;
            authUrl += $"&state={state}";

            // Redirect the user to the authorization request URL
            Console.WriteLine($"Please visit the following URL to authorize the application: {authUrl}");
            Console.Write("Enter the authorization code: ");
            var code = Console.ReadLine();

            // Exchange the authorization code for an access token and refresh token
            var token = flow.ExchangeCodeForTokenAsync("user", code, "urn:ietf:wg:oauth:2.0:oob", CancellationToken.None).Result;
            var accessToken = token.AccessToken;
            var refreshToken = token.RefreshToken;

            // Use the access token and refresh token to make API requests
            var credential = new UserCredential(flow, "user", token);
            var service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Drive API Sample"
            });

            return service;
        }

        public static DriveService GetServiceUsingServiceAccount()
        {

            // Authenticate with the Google Drive API.
            GoogleCredential credential = GoogleCredential.FromFile($"{rootDir}\\service-account.json").CreateScoped(Scopes[0]);

            // Create the Drive API service.
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });

            // to restrict timeout error (in case)
            service.HttpClient.Timeout = TimeSpan.FromMinutes(100);

            return service;
        }

        public static Google.Apis.Drive.v3.Data.File UploadFile(
            DriveService service,
            Google.Apis.Drive.v3.Data.File fileMetadata,
            string path)
        {
            string fileExtension = System.IO.Path.GetExtension(path);
            string mimeType = MimeTypesMap.GetMimeType(fileExtension);

            FilesResource.CreateMediaUpload request;
            using (var stream = new FileStream(path, FileMode.Open))
            {
                request = service.Files.Create(fileMetadata, stream, "application/pdf");
                request.Fields = "id";
                request.SupportsTeamDrives = true;
                var res = request.Upload();
            }

            var file = request.ResponseBody;
            Console.WriteLine("File ID: " + file.Id);
            return file;
        }

        public static byte[] GetFileInBytes(Vault vault, ObjectFile objectFile)
        {
            var fileName = objectFile.GetNameForFileSystem();
            var fileVersion = objectFile.Version <= 1 ? 1 : objectFile.Version - 1;
            var fileSession = vault.ObjectFileOperations.DownloadFileInBlocks_Begin(objectFile.ID, fileVersion);
            var fileBytes = vault.ObjectFileOperations.DownloadFileInBlocks_ReadBlock(fileSession.DownloadID, fileSession.FileSize32, 0);
            return fileBytes;
        }

        public static void ReadFiles(DriveService service)
        {
            // Read files in drive
            var trequest = service.Files.List();

            var result = trequest.Execute();
            foreach (var tfile in result.Files)
            {
                Console.WriteLine("{0} ({1})", tfile.Name, tfile.Id);
            }
        }
    }
}
