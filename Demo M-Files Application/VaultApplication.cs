using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using MFiles.VAF;
using MFiles.VAF.AppTasks;
using MFiles.VAF.Common;
using MFiles.VAF.Configuration;
using MFiles.VAF.Configuration.AdminConfigurations;
using MFiles.VAF.Core;
using MFilesAPI;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Demo_M_Files_Application
{
    /// <summary>
    /// The entry point for this Vault Application Framework application.
    /// </summary>
    /// <remarks>Examples and further information available on the developer portal: http://developer.m-files.com/. </remarks>
    public class VaultApplication
        : ConfigurableVaultApplicationBase<Configuration>
    {
        #region overrides
        protected override void OnConfigurationUpdated(Configuration oldConfiguration, bool isValid, bool updateExternals)
        {
            base.OnConfigurationUpdated(oldConfiguration, isValid, updateExternals);

            // Build up the string to log.
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Configuration changed:");
            stringBuilder.AppendLine($"Old: {JsonConvert.SerializeObject(oldConfiguration, Formatting.Indented)}");
            stringBuilder.AppendLine($"New: {JsonConvert.SerializeObject(this.Configuration, Formatting.Indented)}");

            // Log the string.
            SysUtils.ReportToEventLog(
                stringBuilder.ToString(),
                EventLogEntryType.Information
            );

        }

        protected override IEnumerable<ValidationFinding> CustomValidation(Vault vault, Configuration config)
        {
            // The base implementation should not return any, but handle it in case that changes in the future.
            var validationFindings =
                new List<ValidationFinding>(base.CustomValidation(vault, config) ?? new List<ValidationFinding>());

            // Sanity.
            if (null == config)
                config = new Configuration();

            // Username must be set.
            if (string.IsNullOrWhiteSpace(config.Username))
                validationFindings.Add(new ValidationFinding(
                    ValidationFindingType.Error,
                    nameof(this.Configuration.Username),
                    "Username cannot be empty"));

            // Password must be set.
            if (string.IsNullOrWhiteSpace(config.Password))
                validationFindings.Add(new ValidationFinding(
                    ValidationFindingType.Error,
                    nameof(this.Configuration.Password),
                    "Password cannot be empty"));

            return validationFindings;
        }

        public override string GetDashboardContent(IConfigurationRequestContext context)
        {
            return $"<h3>This is my dashboard.</h3>";
        }
        #endregion
        
        [EventHandler(MFEventHandlerType.MFEventHandlerBeforeCreateNewObjectFinalize)]
        public void MyEventHandler(EventHandlerEnvironment env)
        {
            Vault vault = env.Vault;

            var objectVer = env.ObjVer;
            var objectVerEx = env.ObjVerEx;

            // Get the object's file data.
            var objectFiles = vault.ObjectFileOperations.GetFiles(objectVer);
            var objectFile = objectFiles[1];
            var filename = objectFile.GetNameForFileSystem();
            
            // Download the file.
            string tempFilePath = $"C:\\Development\\Demo M-Files Application\\Demo M-Files Application\\assets\\{filename}";
            vault.ObjectFileOperations.DownloadFile(objectFile.ID, objectFile.Version, tempFilePath);

            string[] Scopes = { DriveService.Scope.Drive };
            string ApplicationName = "M-FILES";

            /// <summary>
            /// When using service account for Drive API,
            /// files are not uploaded into your personal drive rather than a service-to-service comm. drive
            /// in my case it is:- *********.com!
            /// Note:-
            /// You cannot login to the service acc drive like end users, it is only meant for api calls.
            /// </summary>
            /// <param name="env"></param>

            try
            {
                // Authenticate with the Google Drive API.
                GoogleCredential credential = GoogleCredential.FromFile("C:\\Development\\Demo M-Files Application\\Demo M-Files Application\\service-account.json").CreateScoped(Scopes[0]);

                // Create the Drive API service.
                var service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName
                });

                // to restrict timeout
                service.HttpClient.Timeout = TimeSpan.FromMinutes(100);

                // Read files in drive
                var trequest = service.Files.List();

                var result = trequest.Execute();
                foreach (var tfile in result.Files)
                {
                    Console.WriteLine("{0} ({1})", tfile.Name, tfile.Id);
                }

                string folderId = "1FeqgmZdNFe1MwudOgVyjH9ARSOrGIW86"; // Replace with the folder ID where you want to upload the file

                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = filename,
                };

                
                FilesResource.CreateMediaUpload request;
                using (var stream = new FileStream(tempFilePath, FileMode.Open))
                {
                    request = service.Files.Create(fileMetadata, stream, "application/pdf");
                    request.Fields = "id";
                    request.SupportsTeamDrives = true;
                    var res = request.Upload();
                }

                var file = request.ResponseBody;
                Console.WriteLine("File ID: " + file.Id);
                Console.ReadLine();
                

                //var fileName = objectFile.GetNameForFileSystem();
                //var fileVersion = objectFile.Version <= 1 ? 1 : objectFile.Version - 1;
                //var fileSession = vault.ObjectFileOperations.DownloadFileInBlocks_Begin(objectFile.ID, fileVersion);
                //var fileBytes = vault.ObjectFileOperations.DownloadFileInBlocks_ReadBlock(fileSession.DownloadID, fileSession.FileSize32, 0);

                Console.WriteLine("File uploaded to drive!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                // Delete the temporary file.
                File.Delete(tempFilePath);
            }
            
        }
        
    }
}