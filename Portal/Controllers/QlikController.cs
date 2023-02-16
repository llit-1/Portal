using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Net;
using System.IO;
using System.Xml.Linq;
using Portal.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace Portal.Controllers
{
    public class QlikController : Controller
    {
        // Главная страница
        public IActionResult Index()
        {
            var document = "";
            var qlikUrl = QlikAuthLink(document);
            
            return PartialView("Index", qlikUrl);
        }
        // Ежедневный
        public IActionResult Daily()
        {
            var document = "sales%5Cежедневный.qvw";
            var qlikUrl = QlikAuthLink(document);
            return PartialView("Index", qlikUrl);
        }
        // Еженедельный
        public IActionResult Weekly()
        {
            var document = "sales%5Cеженедельный";
            var qlikUrl = QlikAuthLink(document);
            return PartialView("Index", qlikUrl);
        }
        // Продажи Лайт
        public IActionResult SalesLight()
        {
            var document = "sales%5Csales_light.qvw";
            var qlikUrl = QlikAuthLink(document);
            return PartialView("Index", qlikUrl);
        }
        // Калькулятор для ПК
        public IActionResult CalcDesktop()
        {
            var document = "sales%5Ccalc_desktop.qvw";
            var qlikUrl = QlikAuthLink(document);
            return PartialView("Index", qlikUrl);
        }
        // Калькулятор для планшетов
        public IActionResult CalcMobile()
        {
            var document = "sales%5Ccalc_mobile.qvw";
            var qlikUrl = QlikAuthLink(document);
            return PartialView("Index", qlikUrl);
        }
        // Упаковка
        public IActionResult Pack()
        {
            var document = "sales%5Cупаковка.qvw";
            var qlikUrl = QlikAuthLink(document);
            return PartialView();
        }
        // Промо
        public IActionResult Promo()
        {
            var document = "sales%5Cpromo.qvw";
            var qlikUrl = QlikAuthLink(document);
            return PartialView("Index", qlikUrl);
        }
        // Ошибки
        public IActionResult Errors()
        {
            var document = "sales%5Cошибки.qvw";
            var qlikUrl = QlikAuthLink(document);
            return PartialView("Index", qlikUrl);
        }

        //***************************************************************************************

        // Аутнетификация в Клике и ссылка на ресурс
        private string QlikAuthLink(string document)
        {
            var qlikUrl = "https://qlik.ludilove.ru";

            var ticketinguser = "qlikuser";  //Service account used to ask for a ticket (QV Administrator), this is not the end user
            var ticketingpassword = "djlf-yjuf=55";
            var QlikViewServerURL = qlikUrl + "/QVAJAXZFC/getwebticket.aspx";  //address of the QlikView server


            var username = "";
            var groups = "";

            // получаем логин пользователя
            username = @"SHZHLEB\" + User.Claims.Where(a => (a.Type == ClaimTypes.WindowsAccountName)).First().Value;

            //Get the Ticket
            var ticket = getTicket(QlikViewServerURL, username, groups, ticketinguser, ticketingpassword); // add groups into the empty string if required

            //Build a link to either access point or to a single document
            if (document.Length > 0)
            {//Send to a single document
                qlikUrl += "/qvajaxzfc/authenticate.aspx?type=html&try=/qvajaxzfc/opendoc.htm?document=" + document + "&back=/LoginPage.htm&webticket=" + ticket;
            }
            else
            {//Send to a Access Point
                qlikUrl += "/qvajaxzfc/authenticate.aspx?type=html&try=/qlikview&back=/LoginPage.htm&webticket=" + ticket;
            }

            return qlikUrl;
        }

        // This function is going to take the username and groups and return a web ticket from QV
        // User and groups relate to the user you want to reuqest a ticket for
        // ticketinguser and password are the credentials used to ask for the ticket and needs to be a QV admin
        private string getTicket(string QlikViewServerURL, string user, string usergroups, string ticketinguser, string ticketingpassword)
        {

            StringBuilder groups = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(usergroups))
            {
                groups.Append("<GroupList>");
                foreach (string group in usergroups.Split(new char[] { ',' }))
                {
                    groups.Append("<string>");
                    groups.Append(group);
                    groups.Append("</string>");
                }
                groups.Append("</GroupList>");
                groups.Append("<GroupsIsNames>");
                groups.Append("true");
                groups.Append("</GroupsIsNames>");
            }
            string webTicketXml = string.Format("<Global method=\"GetWebTicket\"><UserId>{0}</UserId></Global>", user);

            HttpWebRequest client = (HttpWebRequest)WebRequest.Create(new Uri(QlikViewServerURL));
            client.PreAuthenticate = true;
            client.Method = "POST";
            client.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            client.Credentials = new NetworkCredential(ticketinguser, ticketingpassword);

            using (StreamWriter sw = new StreamWriter(client.GetRequestStream()))
                sw.WriteLine(webTicketXml);
            StreamReader sr = new StreamReader(client.GetResponse().GetResponseStream());
            string result = sr.ReadToEnd();

            XDocument doc = XDocument.Parse(result);
            return doc.Root.Element("_retval_").Value;
        }
    }
}
