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
            string tempFilePath = $"C:\\Development\\Demo M-Files Application\\Demo M-Files Application\\assets\\{filename}";
            vault.ObjectFileOperations.DownloadFile(objectFile.ID, objectFile.Version, tempFilePath);
            return tempFilePath;
        }

        public static void DeleteFileFromLocal(string tempFilePath)
        {
            File.Delete(tempFilePath);
            return;
        }


    }
}
