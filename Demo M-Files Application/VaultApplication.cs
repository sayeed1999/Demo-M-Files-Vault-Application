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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

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

            // Download the file to a temporary location.
            string tempFilePath = Path.GetTempFileName();
            vault.ObjectFileOperations.DownloadFile(objectFile.ID, objectFile.Version, tempFilePath);

            // Authenticate with the Google Drive API.
            UserCredential credential;
            using (var stream = new FileStream("C:\\Development\\Demo M-Files Application\\Demo M-Files Application\\client_secret.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials/drive-dotnet-quickstart.json");

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new[] { DriveService.Scope.Drive },
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            // Create the Drive API service.
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "M-FILES"
            });

            // Upload the file to Google Drive.
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = objectFile.Title,
                Parents = new List<string>() { "1FeqgmZdNFe1MwudOgVyjH9ARSOrGIW86" } // Replace "folder-id" with the ID of the folder you want to upload the file to.
            };

            FilesResource.CreateMediaUpload request;
            using (var stream = new FileStream(tempFilePath, FileMode.Open))
            {
                request = service.Files.Create(
                    fileMetadata,
                    stream, 
                    "pdf");
                request.Upload();
            }

            // Delete the temporary file.
            File.Delete(tempFilePath);
        }
    }
}