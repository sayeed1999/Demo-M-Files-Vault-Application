using MFilesAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo_M_Files_Application
{
    public static class VaultHelper
    {
        public static string DownloadFileIntoLocal(Vault vault, ObjectFile objectFile)
        {
            string filename = objectFile.GetNameForFileSystem();
            string tempFilePath = $"C:\\Development\\M-FILES\\Demo M-Files Application\\Demo M-Files Application\\assets\\{filename}";
            vault.ObjectFileOperations.DownloadFile(objectFile.ID, objectFile.Version, tempFilePath);
            return tempFilePath;
        }

        public static void DeleteFileFromLocal(string tempFilePath)
        {
            File.Delete(tempFilePath);
            return;
        }

        public static string GetFileIDFromFileLocatedInDrive(Vault vault, ObjVer objectVer)
        {
            try
            {
                int propertyDefIDOfDriveFileId = 1026;
                PropertyValue propertyValue = vault.ObjectPropertyOperations.GetProperty(objectVer, propertyDefIDOfDriveFileId);
                PropertyDef propertyDef = vault.PropertyDefOperations.GetPropertyDef(propertyDefIDOfDriveFileId);

                // Get the property value
                TypedValue typedValue = propertyValue.TypedValue;
                //string propertyName = propertyDef.Name;
                string displayValue = typedValue.DisplayValue;
                return displayValue;
            }
            catch (Exception ex)
            {
                // IF NOT FOUND
                return "";
            }
        }

        public static byte[] GetFileInBytes(Vault vault, ObjectFile objectFile)
        {
            var fileName = objectFile.GetNameForFileSystem();
            var fileVersion = objectFile.Version <= 1 ? 1 : objectFile.Version - 1;
            var fileSession = vault.ObjectFileOperations.DownloadFileInBlocks_Begin(objectFile.ID, fileVersion);
            var fileBytes = vault.ObjectFileOperations.DownloadFileInBlocks_ReadBlock(fileSession.DownloadID, fileSession.FileSize32, 0);
            return fileBytes;
        }

    }
}
