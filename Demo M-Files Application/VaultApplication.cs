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
        private DriveService driveService { get; set; }
        private readonly int documentNamePropertyDefID = 0;
        private readonly int driveFileNamePropertyDefID = 1026;
        private readonly string driveFolderId = "1FeqgmZdNFe1MwudOgVyjH9ARSOrGIW86";
        private bool IsNewDoc { get; set;} = false;

        public VaultApplication()
        {
            this.driveService = DriveAPI.GetServiceUsingServiceAccount();
        }

        private void IterateOverPropertyValues(Vault vault, ObjVer objectVer)
        {
            var propertyValues = vault.ObjectPropertyOperations.GetProperties(objectVer, false);

            // Loop through the properties and do something with each one.
            foreach (PropertyValue propertyValue in propertyValues)
            {
                int propertyDefID = propertyValue.PropertyDef;

                // Get the property definition.
                PropertyDef propertyDef = vault.PropertyDefOperations.GetPropertyDef(propertyDefID);

                // Get the property value.
                TypedValue typedValue = propertyValue.TypedValue;

                // Do something with the property value and/or property definition.
                // For example, you could print the property name and value:
                Console.WriteLine($"{propertyDef.Name}: {typedValue.DisplayValue}");
            }
        }

        private PropertyValue CreatePropertyValueOfString(object value)
        {
            PropertyValue propertyValue = new PropertyValue();
            propertyValue.PropertyDef = driveFileNamePropertyDefID;
            propertyValue.TypedValue.SetValue(MFDataType.MFDatatypeText, value);
            return propertyValue;
        }

        [EventHandler(MFEventHandlerType.MFEventHandlerBeforeCreateNewObjectFinalize)]
        [EventHandler(MFEventHandlerType.MFEventHandlerAfterFileUpload)]
        public void DocumentUploadHandler(EventHandlerEnvironment env)
        {
            this.IsNewDoc = true;

            Vault vault = env.Vault;

            var objectVer = env.ObjVer;
            var objectVerEx = env.ObjVerEx;

            // Get the object's file data.
            var objectFiles = vault.ObjectFileOperations.GetFiles(objectVer);

            // document less document is not uploaded to drive
            if (objectFiles.Count == 0) return; 

            var objectFile = objectFiles[1];
            var filename = objectFile.GetNameForFileSystem();

            // get the drive file ID
            var fileId = VaultHelper.GetFileIDFromFileLocatedInDrive(vault, objectVer);

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
                if (String.IsNullOrEmpty(fileId))
                {
                    // Create metadata
                    var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                    {
                        Name = filename,
                        Parents = new string[] { driveFolderId },
                    };

                    // Upload file
                    Google.Apis.Drive.v3.Data.File file = DriveAPI.UploadFile(driveService, fileMetadata, tempFilePath);

                    // Create a property value object for the property.
                    PropertyValue propertyValue = CreatePropertyValueOfString(file.Id);

                    PropertyValues propertyValues = new PropertyValues
                    {
                        { 1, propertyValue }
                    };

                    // Add the property to the document.
                    vault.ObjectPropertyOperations.SetProperties(objectVer, propertyValues);

                    Console.WriteLine("File uploaded to drive!");
                }
                else
                {
                    // Create metadata
                    var fileMetadata = new Google.Apis.Drive.v3.Data.File();

                    // Update/Replace file
                    Google.Apis.Drive.v3.Data.File file = DriveAPI.UpdateFile(driveService, fileMetadata, fileId, tempFilePath);

                    Console.WriteLine("File updated to drive!");

                }
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
            
            this.IsNewDoc = false;
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
                string fileID = VaultHelper.GetFileIDFromFileLocatedInDrive(vault, objectVer);

                // Delete the file
                string result = !String.IsNullOrWhiteSpace(fileID)
                    ? driveService.Files.Delete(fileID).Execute()
                    : "Not Found in Drive Storage!";
            }
            catch(Exception ex)
            {
                Console.WriteLine("Some error has occurred. Try considering checking in before deleting. Error Details: " + ex.Message);
            }
        }


        [EventHandler(MFEventHandlerType.MFEventHandlerAfterSetProperties)]
        public void DocumentPropertyChangeHandler(EventHandlerEnvironment env)
        {
            if (this.IsNewDoc == true) return; 
            
            Vault vault = env.Vault;

            var objectVer = env.ObjVer;
            var objectVerEx = env.ObjVerEx;

            // get the drive file ID
            var fileId = VaultHelper.GetFileIDFromFileLocatedInDrive(vault, objectVer);

            if (String.IsNullOrEmpty(fileId))
            {
                // object is replaced
                return;
            }

            // get the file
            Google.Apis.Drive.v3.Data.File file = DriveAPI.GetFile(driveService, fileId);

            // match filename change
            PropertyValue propertyValue = objectVerEx.GetProperty(this.documentNamePropertyDefID);
            if (propertyValue == null
                || propertyValue.TypedValue == null
                || String.IsNullOrEmpty(propertyValue.TypedValue.DisplayValue)
            ) return;

            // update name in drive if changed in m-files
            var ext = Path.GetExtension(file.Name);
            var displayValue = propertyValue.TypedValue.DisplayValue + ext;
            if (file.Name != displayValue)
            {
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = propertyValue.TypedValue.DisplayValue + ext,
                };

                var res = DriveAPI.UpdateFileMetadata(driveService, fileMetadata, fileId);
            }

        }


        /*[EventHandler(MFEventHandlerType.MFEventHandlerBeforeCheckOut)]
        public void DocumentUpdateHandler(EventHandlerEnvironment env)
        {
            this.DocumentDeleteHandler(env);
        }*/

        /*[EventHandler(MFEventHandlerType.MFEventHandlerBeforeCheckInChanges)]
        public void DocumentCheckInHandler(EventHandlerEnvironment env)
        {
            this.DocumentDeleteHandler(env);
            this.DocumentUploadHandler(env);
        }*/

    }
}