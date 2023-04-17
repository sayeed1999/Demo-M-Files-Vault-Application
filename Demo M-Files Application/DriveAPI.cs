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

namespace Demo_M_Files_Application
{
    public static class DriveAPI
    {
        static string[] Scopes = { 
            DriveService.Scope.Drive, 
            DriveService.Scope.DriveFile 
        };
        static string ApplicationName = "M-FILES";

        public static DriveService GetService()
        {

            // Load the OAuth credentials from the JSON file
            var clientSecrets = GoogleClientSecrets.Load(new MemoryStream(Encoding.UTF8.GetBytes("C:\\Development\\Demo M-Files Application\\Demo M-Files Application\\client-secret.json")));
            var initializer = new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets.Secrets,
                Scopes = Scopes,
                DataStore = new FileDataStore("Drive.Api.Auth.Store")
            };

            var flow = new GoogleAuthorizationCodeFlow(initializer);

            // Obtain an authorization code programmatically
            var codeReceiver = new LocalServerCodeReceiver();
            var authUri = flow.CreateAuthorizationCodeRequest("urn:ietf:wg:oauth:2.0:oob").Build();
            var code = new AuthorizationCodeInstalledApp(flow, codeReceiver).AuthorizeAsync("user", CancellationToken.None).Result;

            // Exchange the authorization code for a token
            var token = flow.ExchangeCodeForTokenAsync("user", code, "urn:ietf:wg:oauth:2.0:oob", CancellationToken.None).Result;

            // Create the Drive API service
            var credential = new UserCredential(flow, "user", token);
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "M-FILES"
            });

            // to restrict timeout error (in case)
            service.HttpClient.Timeout = TimeSpan.FromMinutes(100);

            return service;
        }

        public static DriveService GetServiceUsingServiceAccount()
        {

            // Authenticate with the Google Drive API.
            GoogleCredential credential = GoogleCredential.FromFile("C:\\Development\\Demo M-Files Application\\Demo M-Files Application\\service-account.json").CreateScoped(Scopes[0]);

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
