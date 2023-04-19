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
    public partial class VaultApplication
        : ConfigurableVaultApplicationBase<Configuration>
    {
        private Vault vault;

        

        [EventHandler(MFEventHandlerType.MFEventHandlerBeforeCreateNewObjectFinalize)]
        public void DocumentUploadHandler(EventHandlerEnvironment env)
        {
            Vault vault = env.Vault;

            var objectVer = env.ObjVer;
            var objectVerEx = env.ObjVerEx;

            // Get the object's file data.
            var objectFiles = vault.ObjectFileOperations.GetFiles(objectVer);
            var objectFile = objectFiles[1];
            var filename = objectFile.GetNameForFileSystem();

            // Download the file.
            string tempFilePath = VaultHelper.DownloadFileIntoLocal(vault, objectFile);
            
            

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
                // Get drive api service
                DriveService service = DriveAPI.GetServiceUsingServiceAccount();

                // Read files in drive
                DriveAPI.ReadFiles(service);

                // Create metadata
                string folderId = "1FeqgmZdNFe1MwudOgVyjH9ARSOrGIW86"; // folder id of the folder i want to upload to
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = filename,
                    Parents = new string[] { folderId },
                };

                // Upload file
                Google.Apis.Drive.v3.Data.File file = DriveAPI.UploadFile(service, fileMetadata, tempFilePath);

                // Get the property definition ID of the property you want to add.
                int propertyDefID = 1026; // ID for Google Drive File ID

                // Create a property value object for the property.
                PropertyValue propertyValue = new PropertyValue();
                propertyValue.PropertyDef = propertyDefID;
                propertyValue.TypedValue.SetValue(MFDataType.MFDatatypeText, file.Id);

                PropertyValues propertyValues = new PropertyValues();
                propertyValues.Add(1, propertyValue);

                // Add the property to the document.
                vault.ObjectPropertyOperations.SetProperties(objectVer, propertyValues);

                Console.WriteLine("File uploaded to drive!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                // Delete the temporary file.
                VaultHelper.DeleteFileFromLocal(tempFilePath);
            }
            
        }


        [EventHandler(MFEventHandlerType.MFEventHandlerBeforeDestroyObject)]
        [EventHandler(MFEventHandlerType.MFEventHandlerBeforeDeleteObject)]
        public void DocumentDeleteHandler(EventHandlerEnvironment env)
        {
            Vault vault = env.Vault;

            var objectVer = env.ObjVer;
            var objectVerEx = env.ObjVerEx;

            try
            {
                // Get the ID of the file located in drive
                string fileID = DriveAPI.GetFileIDFromFileLocatedInDrive(vault, objectVer);

                // Get drive api service
                DriveService service = DriveAPI.GetServiceUsingServiceAccount();

                // Delete the file
                string result = !String.IsNullOrWhiteSpace(fileID)
                    ? service.Files.Delete(fileID).Execute()
                    : "Not Found in Drive Storage!";
            }
            catch(Exception ex)
            {
                Console.WriteLine("Some error has occurred. Try considering checking in before deleting. Error Details: " + ex.Message);
            }
        }


        //[EventHandler(MFEventHandlerType.MFEventHandlerBeforeCheckOut)]
        //public void DocumentUpdateHandler(EventHandlerEnvironment env)
        //{
        //    this.DocumentDeleteHandler(env);
        //}

        [EventHandler(MFEventHandlerType.MFEventHandlerBeforeCheckInChanges)]
        public void DocumentCheckInHandler(EventHandlerEnvironment env)
        {
            this.DocumentDeleteHandler(env);
            this.DocumentUploadHandler(env);
        }

    }
}