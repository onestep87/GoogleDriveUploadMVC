using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
// ...
namespace GoogleDriveUploadMVC.Models
{
    public class GoogleDriveApiHelper2
    {
        // тут дублируется авторизация потому что метод получить файлы с папки работает
        // через гугл драйв версию v2 и если оставить в одном методе то вижуалка ругается и будут ошибки
        public static string[] Scopes = { Google.Apis.Drive.v2.DriveService.Scope.Drive };
        public static Google.Apis.Drive.v2.DriveService GetService_v2()
        {
            UserCredential credential;
            using (var stream = new FileStream(@"C:\Users\Даниил\Desktop\papka\client_secret.json", FileMode.Open, FileAccess.Read))
            {
                String FolderPath = @"C:\Users\Даниил\Desktop\papka\";
                String FilePath = Path.Combine(FolderPath, "DriveServiceCredentials.json");
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(FilePath, true)).Result;
            }

            //Create Drive API service.
            Google.Apis.Drive.v2.DriveService service = new Google.Apis.Drive.v2.DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "GoogleDriveRestAPI-v2",
            });
            return service;
        }
        
        public static List<GoogleDriveFile> GetContainsInFolder(String folderId)
        {
            List<string> ChildList = new List<string>();
            Google.Apis.Drive.v2.DriveService ServiceV2 = GetService_v2();
            ChildrenResource.ListRequest ChildrenIDsRequest = ServiceV2.Children.List(folderId);
            do
            {
                ChildList children = ChildrenIDsRequest.Execute();

                if (children.Items != null && children.Items.Count > 0)
                {
                    foreach (var file in children.Items)
                    {
                        ChildList.Add(file.Id);
                    }
                }
                ChildrenIDsRequest.PageToken = children.NextPageToken;

            } while (!String.IsNullOrEmpty(ChildrenIDsRequest.PageToken));

            //Get All File List
            List<GoogleDriveFile> AllFileList = GoogleDriveAPIHelper.GetDriveFiles();
            List<GoogleDriveFile> Filter_FileList = new List<GoogleDriveFile>();

            foreach (string Id in ChildList)
            {
                Filter_FileList.Add(AllFileList.Where(x => x.Id == Id).FirstOrDefault());
            }
            return Filter_FileList;
        }

        // ...
    }
}