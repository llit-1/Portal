using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Encodings.Web;
using System.IO;

namespace Portal.Helpers
{
    public static class RecursiveDirectoryMenu
    {
        public static HtmlString JSTreeMenu(DirectoryInfo dirInfo, string rootName, string ULId, string ULClass, string rootIcon, bool open)
        {
            TagBuilder mainUL = new TagBuilder("ul");
            mainUL.Attributes.Add("class", ULClass);
            mainUL.Attributes.Add("id", ULId);            

            TagBuilder mainLi = new TagBuilder("li");
            mainLi.InnerHtml.Append(rootName);
            mainLi.Attributes.Add("data-jstree", "{\"icon\":\"" + rootIcon + "\"}");
            if(open) mainLi.Attributes.Add("class", "rootTree jstree-open");
            else mainLi.Attributes.Add("class", "rootTree");
            mainLi.InnerHtml.AppendHtml(UL(dirInfo));

            mainUL.InnerHtml.AppendHtml(mainLi);        

            var writer = new StringWriter();
            mainUL.WriteTo(writer, HtmlEncoder.Default);
            return new HtmlString(writer.ToString());
        }


        // Метод для перебора всей структуры каталогов и файлов
        private static TagBuilder UL(DirectoryInfo dirInfo)
        {
            TagBuilder sub_ul = new TagBuilder("ul");
            var Dirs = dirInfo.EnumerateDirectories();

            foreach (var subDir in Dirs)
            {
                TagBuilder li = new TagBuilder("li");

                li.InnerHtml.Append(subDir.Name);
                li.InnerHtml.AppendHtml(UL(subDir));
                li.Attributes.Add("class", "secTree");
                li.Attributes.Add("data-jstree", "{\"icon\":\"glyphicon glyphicon-book\"}");
                li.Attributes.Add("id", subDir.Name);

                sub_ul.InnerHtml.AppendHtml(li);

            }

            // фильтр файлов
            var files = dirInfo.EnumerateFiles().Where(s =>
                    !s.Name.StartsWith("~$") &
                    (
                        s.Extension.ToLower() == ".doc"
                        || s.Extension.ToLower() == ".docx"
                        || s.Extension.ToLower() == ".xls"
                        || s.Extension.ToLower() == ".xlsx"
                        || s.Extension.ToLower() == ".pdf"
                        || s.Extension.ToLower() == ".jpg"
                        || s.Extension.ToLower() == ".jpeg"
                        || s.Extension.ToLower() == ".ppt"
                        || s.Extension.ToLower() == ".pptx"
                        || s.Extension.ToLower() == ".mov"
                        || s.Extension.ToLower() == ".mp4"
                    ));

            foreach (var file in files)
            {
                TagBuilder li = new TagBuilder("li");

                // убираем расширение из имени файла
                var fileName = file.Name.Replace(file.Extension, "");

                li.InnerHtml.Append(fileName);                
                li.Attributes.Add("id", file.Name);
                li.Attributes.Add("onclick", "selectFile('" + file.FullName.Replace("\\", "slashtoreplace") + "', '"+ file.Extension.ToLower() + "')");

                // документы
                if (file.Extension.ToLower() == ".doc" || file.Extension.ToLower() == ".docx")
                {
                    li.Attributes.Add("data-jstree", "{\"icon\":\"glyphicon glyphicon-file\"}");
                    li.Attributes.Add("class", "word");
                }
                // таблицы
                if (file.Extension.ToLower() == ".xls" || file.Extension.ToLower() == ".xlsx")
                {
                    li.Attributes.Add("data-jstree", "{\"icon\":\"glyphicon glyphicon-file\"}");
                    li.Attributes.Add("class", "excell");
                }
                // презентации
                if (file.Extension.ToLower() == ".ppt" || file.Extension.ToLower() == ".pptx")
                {
                    li.Attributes.Add("data-jstree", "{\"icon\":\"glyphicon glyphicon-file\"}");
                    li.Attributes.Add("class", "presentations");
                }
                // pdf
                if (file.Extension.ToLower() == ".pdf")
                {
                    li.Attributes.Add("data-jstree", "{\"icon\":\"glyphicon glyphicon-file\"}");
                    li.Attributes.Add("class", "pdf");
                }
                // изображения
                if (file.Extension.ToLower() == ".jpg" || file.Extension.ToLower() == ".jpeg" || file.Extension.ToLower() == ".png")
                {
                    li.Attributes.Add("data-jstree", "{\"icon\":\"glyphicon glyphicon-picture\"}");
                    li.Attributes.Add("class", "pictures");
                }
                // видео
                if (file.Extension.ToLower() == ".mp4" || file.Extension.ToLower() == ".mov" || file.Extension.ToLower() == ".avi" || file.Extension.ToLower() == ".mkv" || file.Extension.ToLower() == ".mpeg")
                {
                    li.Attributes.Add("data-jstree", "{\"icon\":\"glyphicon glyphicon-film\"}");
                    li.Attributes.Add("class", "videos");
                }

                sub_ul.InnerHtml.AppendHtml(li);
            }

            return sub_ul;
        }
    }
}
