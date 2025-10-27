using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.Models;
using Portal.Models.MSSQL.Location;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Portal.Controllers
{
    [Authorize(Roles = "library")]
    public class LibraryController : Controller
    {
        private Portal.Services.IStreamVideoService streamService;
        DB.SQLiteDBContext db;
        string[] LibraryExtensions = { ".doc", ".docx", ".xls", ".xlsx", ".xlsb", ".ppt", ".pptx", ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".mov", ".avi" };
        DB.MSSQLDBContext RKNET;

        public LibraryController(DB.SQLiteDBContext sqliteContext, Portal.Services.IStreamVideoService streamServiceContext, DB.MSSQLDBContext mSSQLDB)
        {
            db = sqliteContext;
            streamService = streamServiceContext;
            RKNET= mSSQLDB;
        }

        // Корневые разделы Библиотеки Знаний
        public IActionResult Index(string chapter)
        {
            var result = new RKNet_Model.Result<string>();

            if (chapter != null)
                result.Data = chapter;

            return PartialView(result);
        }

        // Внутренние документы
        [Authorize(Roles = "library_internal")]
        public IActionResult InternalDocs()
        {
            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Библиотека знаний";
            log.Description = "Внутренние документы";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return PartialView();
        }

        // Пекарни Люди Любят
        [Authorize(Roles = "library_ll")]
        public IActionResult LudiLove()
        {
            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Библиотека знаний";
            log.Description = "Пекарни Люди Любят";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return RedirectToAction("RootFolder", new { id = 1 });
        }

        // Справочник Франчайзи
        [Authorize(Roles = "library_franch")]
        public IActionResult FranchBook()
        {
            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Библиотека знаний";
            log.Description = "Справочник Франчайзи";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return RedirectToAction("RootFolder", new { id = 2 });
        }

        // Паспорта качества
        [Authorize(Roles = "library_passports")]
        public IActionResult Passports()
        {
            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Библиотека знаний";
            log.Description = "Паспорта качества";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return RedirectToAction("RootFolder", new { id = 15 });
        }

        [Authorize(Roles = "library_shzhleb")]
        public IActionResult Hleb()
        {
            var log = new LogEvent<string>(User);
            log.Name = "Библиотека знаний";
            log.Description = "Сестрорецкий хлебозавод";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return RedirectToAction("RootFolder", new { id = 17 });
        }

        // ----------------------------------------------------------------------------------------------------------------
        // Служба персонала
        [Authorize(Roles = "library_hr")]
        public IActionResult Hr()
        {
            return RedirectToAction("RootFolder", new { id = 6 });
        }

        // Отдел строительства
        [Authorize(Roles = "library_build")]
        public IActionResult Build()
        {
            return RedirectToAction("RootFolder", new { id = 7 });
        }

        // Служба эксплуатации
        [Authorize(Roles = "library_explotation")]
        public IActionResult Explotation()
        {
            return RedirectToAction("RootFolder", new { id = 8 });
        }

        // Отдел маркетинга
        [Authorize(Roles = "library_marketing")]
        public IActionResult Marketing()
        {
            return RedirectToAction("RootFolder", new { id = 9 });
        }

        // Бухгалтерия
        [Authorize(Roles = "library_buh")]
        public IActionResult Buh()
        {
            return RedirectToAction("RootFolder", new { id = 10 });
        }

        // Отдел бизнесс-планирования и анализа
        [Authorize(Roles = "library_analytic")]
        public IActionResult Analytic()
        {
            return RedirectToAction("RootFolder", new { id = 3 });
        }

        // Отдел закупок
        [Authorize(Roles = "library_shopping")]
        public IActionResult Shopping()
        {
            return RedirectToAction("RootFolder", new { id = 11 });
        }

        // Финансово-экономический отдел
        [Authorize(Roles = "library_finance")]
        public IActionResult Finance()
        {
            return RedirectToAction("RootFolder", new { id = 4 });
        }

        // Отдел системного сопровождения
        [Authorize(Roles = "library_oss")]
        public IActionResult Oss()
        {
            return RedirectToAction("RootFolder", new { id = 5 });
        }

        // Отдел развития
        [Authorize(Roles = "library_rent")]
        public IActionResult Rent()
        {
            return RedirectToAction("RootFolder", new { id = 12 });
        }

        // Реквизиты юл
        [Authorize(Roles = "library_rekul")]
        public IActionResult Rekul()
        {
            return RedirectToAction("RootFolder", new { id = 16 });
        }
        //  [Authorize(Roles = "library_franch")]
        public IActionResult SpravInformation()
        {
            return RedirectToAction("RootFolder", new { id = 18 });

        }



        // ----------------------------------------------------------------------------------------------------------------
        // Корневая папка из БД
        public IActionResult RootFolder(int id)
        {
            var path = db.RootFolders.FirstOrDefault(f => f.Id == id).Path;
            return RedirectToAction("Folder", new { path = path });
        }

        // Папка
        public IActionResult Folder(string path)
        {
            if (path == "Index" || path == "InternalDocs")
                return PartialView(path);

            var folderView = new ViewModels.Library.FolderView();

            // разэкранирование "плюс" и "пробел"
            path = path.Replace("plustoreplace", "+");
            path = path.Replace("backspacetoreplace", " ");

            // текущий каталог
            folderView.curDirectory = new DirectoryInfo(path);

            // корневой каталог раздела
            var rootItem = db.RootFolders.FirstOrDefault(r => folderView.curDirectory.FullName.Contains(r.Path));

            // условный каталог Внутренние документы
            if (rootItem.Id != 1 && rootItem.Id != 2 && rootItem.Id != 14 && rootItem.Id != 15 && rootItem.Id != 17 && rootItem.Id != 18)
            {
                var internalDocsItem = new RKNet_Model.Library.RootFolder();
                internalDocsItem.Name = "Внутренние документы";
                internalDocsItem.Path = "InternalDocs";
                folderView.navItems.Add(internalDocsItem);
            }

            folderView.navItems.Add(rootItem);
            var rootDir = new DirectoryInfo(rootItem.Path);

            // промежуточные каталоги
            var curPath = folderView.curDirectory.FullName.Replace(rootDir.FullName, "");
            string[] dirs = curPath.Split('\\');

            var tempPath = rootDir.FullName;
            foreach (var dir in dirs)
            {
                if (dir.Length > 0)
                {
                    tempPath += "\\" + dir;
                    var item = new RKNet_Model.Library.RootFolder();
                    item.Name = dir;
                    item.Path = tempPath;
                    folderView.navItems.Add(item);
                }
            }

            // назад
            var itemsCount = folderView.navItems.Count;
            if (itemsCount > 1)
            {
                folderView.prevPath = folderView.navItems[itemsCount - 2].Path;
            }
            else
            {
                if (rootItem.Id <= 2 || rootItem.Id == 15 || rootItem.Id == 17 || rootItem.Id == 18)
                    folderView.prevPath = "Index";

                if (rootItem.Id > 2 && rootItem.Id < 15)
                    folderView.prevPath = "InternalDocs";

            }

            // список недавно изменённых файлов
            folderView.newsFiles = GetNewsFiles(rootDir);

            //спецпапка с распределением по тт
            if (rootItem.Id == 18)
            {
                folderView.CastingFolders = true;
                if (User.IsInRole("library_franch_common"))
                {
                    folderView.AllowedDirectories.Add(new DirectoryInfo(rootDir + "\\" + "Общие документы"));
                }
                if (User.IsInRole("library_franch_IP"))
                {
                    string userLogin = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.WindowsAccountName).Value;
                    var user = db.Users.Include(x => x.TTs).FirstOrDefault(x => x.Login == userLogin);
                    List<Models.MSSQL.Location.Location> locations = RKNET.Locations.AsEnumerable().Where(x => user.TTs.Any(y => y.Restaurant_Sifr == x.RKCode)).ToList();
                    List<LocationVersions> locationVersions = RKNET.LocationVersions.Include(x => x.Entity).AsEnumerable().Where(x => locations.Any(y => y.Guid == x.Location.Guid && x.Actual == 1 && x.Entity != null)).ToList();
                    folderView.AllowedDirectories.AddRange(RKNET.Entity.AsEnumerable().Where(x => locationVersions.Any(y => y.Entity.Guid == x.Guid)).Select(x => new DirectoryInfo(rootDir + "\\" + x.Name)).ToList());
                }
                folderView.newsFiles = folderView.newsFiles.Where(x => folderView.AllowedDirectories.Any(y => x.FullName.Contains(y.FullName.TrimEnd('.')))).ToList();
                if (rootItem.Path != folderView.curDirectory.FullName)
                {
                    folderView.CastingFolders = false;
                }
            }
            return PartialView(folderView);
        }

        // Файл        
        [AllowAnonymous]
        public IActionResult File(string filePath)
        {
            var fileView = new ViewModels.Library.FileView();

            // разэкранирование "плюс" и "пробел"
            filePath = filePath.Replace("plustoreplace", "+");
            filePath = filePath.Replace("backspacetoreplace", " ");

            // данные файла
            var fileInfo = new System.IO.FileInfo(filePath);
            fileView.fileName = fileInfo.Name.Replace(fileInfo.Extension, "");
            fileView.fileExt = fileInfo.Extension.ToLower();

            // получаем id каталога и индекс файла
            var rootDir = db.RootFolders.FirstOrDefault(r => filePath.Contains(r.Path));
            var allFiles = Directory.GetFiles(rootDir.Path, "*.*", SearchOption.AllDirectories);

            fileView.rootFolderId = rootDir.Id;
            fileView.fileIndex = Array.IndexOf(allFiles, fileInfo.FullName);
            fileView.filePath = fileInfo.FullName;

            return PartialView(fileView);
        }

        public IActionResult DownloadFile(string path)
        {
            // разэкранирование "плюс" и "пробел"
            path = path.Replace("plustoreplace", "+");
            path = path.Replace("backspacetoreplace", " ");

            // данные файла
            var fileInfo = new FileInfo(path);

            if (!fileInfo.Exists)
            {
                return NotFound(); // Если файл не найден, возвращаем 404
            }

            // Возвращаем файл для скачивания
            return PhysicalFile(fileInfo.FullName, "application/octet-stream", fileInfo.Name);
        }

        // Загрузка файла по пути
        [AllowAnonymous]
        public IActionResult GetFile(string path)
        {
            try
            {
                // разэкранирование "плюс" и "пробел"
                path = path.Replace("plustoreplace", "+");
                path = path.Replace("backspacetoreplace", " ");

                var fileInfo = new System.IO.FileInfo(path);
                var file = System.IO.File.ReadAllBytes(path);

                switch (fileInfo.Extension.ToLower())
                {
                    case ".doc":
                    case ".docx":
                        return new FileContentResult(file, "application/msword");
                    case ".xls":
                    case ".xlsx":
                        return new FileContentResult(file, "application/vnd.ms-excel");
                    case ".ppt":
                    case ".pptx":
                        return new FileContentResult(file, "application/vnd.ms-powerpoint");
                    case ".pdf":
                        return new FileContentResult(file, "application/pdf");
                    case ".jpg":
                    case ".jpeg":
                        return new FileContentResult(file, "image/jpeg");
                    case ".gif":
                        return new FileContentResult(file, "image/gif");
                    case ".png":
                        return new FileContentResult(file, "image/png");
                    case ".avi":
                        return new FileContentResult(file, "video/avi");
                    case ".mov":
                        return new FileContentResult(file, "video/quicktime");
                    case ".mp4":
                        return new FileContentResult(file, "video/mp4");
                    case ".mpeg":
                        return new FileContentResult(file, "video/mpeg");
                    case ".svg":
                        return new FileContentResult(file, "image/svg+xml");
                    default:
                        return new FileContentResult(file, "application/octet-stream");

                }
            }
            catch (Exception e)
            {
                return new ObjectResult(e.Message);
            }


        }

        [AllowAnonymous]
        public IActionResult GetEmailChecksFile(int a)
        {
            try
            {
                string path = "\\\\shzhleb.ru\\ll\\LLWork\\Отчеты\\Чеки РОзницы\\EmailChecks.xlsx";
                var fileInfo = new System.IO.FileInfo(path);
                var file = System.IO.File.ReadAllBytes(path);
                Response.Headers.Add("Access-Control-Allow-Origin", "*");
                Response.Headers.Add("Access-Control-Allow-Methods", "GET");
                Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
                return PhysicalFile(fileInfo.FullName, "application/octet-stream", fileInfo.Name);
            }
            catch (Exception e)
            {
                return new ObjectResult(e.Message);
            }
        }


        // Выдача видеопотока
        public async Task<IActionResult> GetVideo(string url)
        {
            url = "http://test.ludilove.ru" + url;
            //return new ObjectResult(url);
            var stream = await streamService.GetVideoStream(url);
            return new FileStreamResult(stream, "video/mp4");
        }

        // Загрузка файла по имени
        [AllowAnonymous]
        public IActionResult GetFileByName(int rootFolderId, string fileName)
        {
            // разэкранирование "плюс" и "пробел"
            fileName = fileName.Replace("plustoreplace", "+");
            fileName = fileName.Replace("backspacetoreplace", " ");

            var rootDir = db.RootFolders.FirstOrDefault(r => r.Id == rootFolderId);
            var allFiles = Directory.GetFiles(rootDir.Path, "*.*", SearchOption.AllDirectories);
            var filePath = allFiles.FirstOrDefault(f => f.Contains(fileName));

            return RedirectToAction("GetFile", new { path = filePath });
        }

        // Загрузка файла по индексу
        [AllowAnonymous]
        public IActionResult GetFileByIndex(string json, string date)
        {
            var fileView = Newtonsoft.Json.JsonConvert.DeserializeObject<ViewModels.Library.FileView>(json);

            int rootFolderId = fileView.rootFolderId;
            int fileIndex = fileView.fileIndex;

            var rootDir = db.RootFolders.FirstOrDefault(r => r.Id == rootFolderId);
            var allFiles = Directory.GetFiles(rootDir.Path, "*.*", SearchOption.AllDirectories);
            var filePath = allFiles[fileIndex];
            var fileInfo = new FileInfo(filePath);

            return PhysicalFile(fileInfo.FullName, "application/octet-stream", fileInfo.Name);
        }

        // Список последних изменённых файлов
        public List<FileInfo> GetNewsFiles(DirectoryInfo rootDirectory)
        {
            var newsFiles = new List<FileInfo>();

            newsFiles = rootDirectory.GetFiles("*.*", SearchOption.AllDirectories)
                .Where(file => !file.Name.Contains("~") & LibraryExtensions.Contains(file.Extension.ToLower()))
                .OrderByDescending(f => f.LastWriteTime)
                .Take(7).ToList();

            return newsFiles;
        }

        // Поиск
        public IActionResult Search(string searchString, string rootFolderPath)
        {
            var rootDir = new DirectoryInfo(rootFolderPath);
            var files = rootDir.GetFiles("*.*", SearchOption.AllDirectories)
                .Where(f => f.Name.ToLower().Contains(searchString.ToLower()))
                .Where(f => !f.Name.Contains("~") & LibraryExtensions.Contains(f.Extension.ToLower()))
                .OrderBy(f => f.Name)
                .Select(f => new { f.Name, f.FullName, f.Extension })
                .ToList();

            return new ObjectResult(files);
        }
    }
}



