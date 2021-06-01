using GoogleDriveUploadMVC.Models;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Collections.Generic;
using Google.Apis.Drive.v2;

namespace GoogleDriveUploadMVC.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(HttpPostedFileBase file,string text, string fileId)
        {
            GoogleDriveAPIHelper.UplaodFileOnDrive(file);
            ViewBag.Success = "File Uploaded on Google Drive";
            GoogleDriveAPIHelper.CreateFolder(text);
            ViewBag.Success = "Folder Created";
            //GoogleDriveAPIHelper.DownloadGoogleFile(fileId);
            return View();
        }
        
        
        public ActionResult About(string fileId)
        {
            GoogleDriveAPIHelper.GetDriveFiles();
            
            return View();
        }
        [HttpGet]
        public ActionResult GetGoogleDriveFiles()
        {
            return View(GoogleDriveAPIHelper.GetDriveFiles());
        }
        [HttpPost]
        public ActionResult DeleteFile(GoogleDriveFile file)
        {
            GoogleDriveAPIHelper.DeleteFile(file);
            return RedirectToAction("GetGoogleDriveFiles");
        }

        public void DownloadFile(string id)
        {
            string FilePath = GoogleDriveAPIHelper.DownloadGoogleFile(id);
            

            Response.ContentType = "application/zip";
            Response.AddHeader("Content-Disposition", "attachment; filename=" + Path.GetFileName(FilePath));
            Response.WriteFile(System.Web.HttpContext.Current.Server.MapPath("~/GoogleDriveFiles/" + Path.GetFileName(FilePath)));
            Response.End();
            Response.Flush();
        }
        // вид 2 страницы 
        [HttpGet] 
        public ActionResult GetGoogleDriveFiles2()
        {
            return View(GoogleDriveAPIHelper.GetDriveFiles());
        }


        [HttpPost]
        public ActionResult CreateFolder(string FolderName)
        {
            GoogleDriveAPIHelper.CreateFolder(FolderName);
            return RedirectToAction("GetGoogleDriveFiles2");
        }
        [HttpGet]
        public ActionResult GetContainsInFolder(string folderId)
        {            
            return View(GoogleDriveApiHelper2.GetContainsInFolder(folderId));
        }

        [HttpPost]
        public ActionResult UploadFile(HttpPostedFileBase file)
        {
            GoogleDriveAPIHelper.UplaodFileOnDrive(file);
            return RedirectToAction("GetGoogleDriveFiles2");
        }

        [HttpPost]
        public ActionResult FileUploadInFolder(GoogleDriveFile FolderId, HttpPostedFileBase file)
        {
            GoogleDriveAPIHelper.FileUploadInFolder(FolderId.Id, file);
            return RedirectToAction("GetGoogleDriveFiles2");
        }
        //[HttpGet]
        //public ActionResult Service(DriveService service)
        //{
        //    MyClass.PrintAbout(service);
        //    return View();
        //}
    }
}