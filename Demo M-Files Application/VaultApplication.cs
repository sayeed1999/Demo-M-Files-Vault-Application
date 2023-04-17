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
                DriveService service = DriveAPI.GetService();

                // Read files in drive                
                DriveAPI.ReadFiles(service);

                // Create metadata
                //string folderId = "1FeqgmZdNFe1MwudOgVyjH9ARSOrGIW86"; // Replace with the folder ID where you want to upload the file
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = filename,
                };

                // Upload file
                var file = DriveAPI.UploadFile(service, fileMetadata, tempFilePath);

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
        
    }
}