using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Threading;
using System.IO;
using Google.Apis.Drive.v3;
using Google.Apis.Download;
using GoogleDriveUploadMVC.Models;

namespace GoogleDriveUploadMVC.Models
{
    public class GoogleDriveAPIHelper
    {
        //тут ставим необходимые разрешения
        public static string[] Scopes = { Google.Apis.Drive.v3.DriveService.Scope.Drive };

        //тут сервис\авторизация необходимая почти во всех остальных методах
        public static DriveService GetService()
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

            //тот самый необходимый сервис
            DriveService service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "GoogleDriveRestAPI-v3",
            });

            return service;
        }


        //получаем абсолютно все файлы с гугл драйва включая файлы в папках
        public static List<GoogleDriveFile> GetDriveFiles()
        {
            Google.Apis.Drive.v3.DriveService service = GetService();
            // выделяем необходимые параметры
            Google.Apis.Drive.v3.FilesResource.ListRequest FileListRequest = service.Files.List();

            // for getting folders only.
            //FileListRequest.Q = "mimeType='application/vnd.google-apps.folder'";
            FileListRequest.Fields = "nextPageToken, files(createdTime, id, name, size, version, parents, mimeType)";
            // засовываем в лист
            IList<Google.Apis.Drive.v3.Data.File> files = FileListRequest.Execute().Files;
            List<GoogleDriveFile> FileList = new List<GoogleDriveFile>();
            // For getting only folders
            // files = files.Where(x => x.MimeType == "application/vnd.google-apps.folder").ToList();
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    GoogleDriveFile File = new GoogleDriveFile
                    {
                        Id = file.Id,
                        Name = file.Name,
                        Size = file.Size,
                        Version = file.Version,
                        CreatedTime = file.CreatedTime,
                        Parents = file.Parents,
                        MimeType = file.MimeType
                    };
                    FileList.Add(File);
                }
            }
            return FileList;
        }


        //загрузка файлов в корневую папку гугла(быстрый доступ)
        public static void UplaodFileOnDrive(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                //create service
                DriveService service = GetService();
                string path = Path.Combine(HttpContext.Current.Server.MapPath("~/GoogleDriveFiles"),
                Path.GetFileName(file.FileName));
                file.SaveAs(path);
                var FileMetaData = new Google.Apis.Drive.v3.Data.File();
                FileMetaData.Name = Path.GetFileName(file.FileName);
                FileMetaData.MimeType = MimeMapping.GetMimeMapping(path);
                FilesResource.CreateMediaUpload request;
                using (var stream = new System.IO.FileStream(path, System.IO.FileMode.Open))
                {
                    request = service.Files.Create(FileMetaData, stream, FileMetaData.MimeType);
                    request.Fields = "id";
                    request.Upload();
                }
             


            }
        }

        //скачивание файлов по атрибуту айди
        public static string DownloadGoogleFile(string fileId)
        {
            DriveService service = GetService();

            string FolderPath = HttpContext.Current.Server.MapPath("/GoogleDriveFiles/");
            FilesResource.GetRequest request = service.Files.Get(fileId);

            string FileName = request.Execute().Name;
            string FilePath = Path.Combine(FolderPath, FileName);

            MemoryStream stream1 = new MemoryStream();

            // успешно скачивается\скачивается\скачиe провалилось
            request.MediaDownloader.ProgressChanged += (Google.Apis.Download.IDownloadProgress progress) =>
            {
                switch (progress.Status)
                {
                    case DownloadStatus.Downloading:
                        {
                            Console.WriteLine(progress.BytesDownloaded);
                            break;
                        }
                    case DownloadStatus.Completed:
                        {
                            Console.WriteLine("Download complete.");
                            SaveStream(stream1, FilePath);
                            break;
                        }
                    case DownloadStatus.Failed:
                        {
                            Console.WriteLine("Download failed.");
                            break;
                        }
                }
            };
            request.Download(stream1);
            return FilePath;
        }

        //  вспомогательный метод скачивания, даёт ссылку на локальную прослойку
        private static void SaveStream(MemoryStream stream, string FilePath)
        {
            using (FileStream file = new FileStream(FilePath, FileMode.Create, FileAccess.ReadWrite))
            {
                stream.WriteTo(file);
            }
        }


        // создать папку в корневом разделе
        public static void CreateFolder(string FolderName)
        {
            DriveService service = GetService();
            var FileMetaData = new Google.Apis.Drive.v3.Data.File();
            FileMetaData.Name = FolderName;
            //this mimetype specify that we need folder in google drive
            FileMetaData.MimeType = "application/vnd.google-apps.folder";
           FilesResource.CreateRequest request;
            request = service.Files.Create(FileMetaData);
            request.Fields = "id";
            var file = request.Execute();
            
        }

        // загрузить файл в определённую папку, необходим её айди
        public static void FileUploadInFolder(string folderId, HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                //create service
                DriveService service = GetService();
                //get file path
                string path = Path.Combine(HttpContext.Current.Server.MapPath("~/GoogleDriveFiles"),
                Path.GetFileName(file.FileName));
                file.SaveAs(path);
                //create file metadata
                var FileMetaData = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = Path.GetFileName(file.FileName),
                    MimeType = MimeMapping.GetMimeMapping(path),
                    //id of parent folder 
                    Parents = new List<string>
                    {
                        folderId
                    }
                };
                FilesResource.CreateMediaUpload request;
                //create stream and upload
                using (var stream = new System.IO.FileStream(path, System.IO.FileMode.Open))
                {
                    request = service.Files.Create(FileMetaData, stream, FileMetaData.MimeType);
                    request.Fields = "id";
                    request.Upload();
                }
                var file1 = request.ResponseBody;
            }
        }

        // получить список всех папок
        public static List<GoogleDriveFile> GetDriveFolders()
        {
            DriveService service = GetService();
            List<GoogleDriveFile> FolderList = new List<GoogleDriveFile>();
            FilesResource.ListRequest request = service.Files.List();
            request.Q = "mimeType='application/vnd.google-apps.folder'";
            request.Fields = "files(id, name)";
            Google.Apis.Drive.v3.Data.FileList result = request.Execute();
            foreach (var file in result.Files)
            {
                GoogleDriveFile File = new GoogleDriveFile
                {
                    Id = file.Id,
                    Name = file.Name,
                    Size = file.Size,
                    Version = file.Version,
                    CreatedTime = file.CreatedTime
                };
                FolderList.Add(File);
            }            
            return FolderList;
        }
        // удалить файл
        public static void DeleteFile(GoogleDriveFile files)
        {
            DriveService service = GetService();
            try
            {
                // Initial validation.
                if (service == null)
                    throw new ArgumentNullException("service");

                if (files == null)
                    throw new ArgumentNullException(files.Id);

                // Make the request.
                service.Files.Delete(files.Id).Execute();
            }
            catch (Exception ex)
            {
                throw new Exception("Request Files.Delete failed.", ex);
            }
        }


    }
}