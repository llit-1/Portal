using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace Portal.ViewModels.Library
{
    public class FolderView
    {
        public DirectoryInfo curDirectory;
        public string prevPath;
        public List<RKNet_Model.Library.RootFolder> navItems;
        public List<FileInfo> newsFiles;

        public FolderView()
        {
            navItems = new List<RKNet_Model.Library.RootFolder>();
            newsFiles = new List<FileInfo>();
        }
    }
}
