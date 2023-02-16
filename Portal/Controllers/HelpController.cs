using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.IO;

namespace Portal.Controllers
{
    public class HelpController : Controller
    {
        DB.SQLiteDBContext db;

        public HelpController(DB.SQLiteDBContext sqliteContext)
        {
            db = sqliteContext;
        }

        public IActionResult Index()
        {
            var rootPath = db.RootFolders.FirstOrDefault(r => r.Id == 14).Path;            
            return RedirectToAction("Folder", new { path = rootPath });
        }

        // Папка
        public IActionResult Folder(string path)
        {
            if (path == "Help")
            {
                return Redirect("/Help/Index");
            }

            var folderView = new ViewModels.Library.FolderView();
            
            // разэкранирование "плюс" и "пробел"
            path = path.Replace("plustoreplace", "+");
            path = path.Replace("backspacetoreplace", " ");

            // текущий каталог
            folderView.curDirectory = new DirectoryInfo(path);

            // корневой каталог раздела
            var rootItem = db.RootFolders.FirstOrDefault(r => path.Contains(r.Path));
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
                folderView.prevPath = "Help";
            }

            return PartialView("Index", folderView);
        }
    }
}
