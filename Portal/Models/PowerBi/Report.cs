using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Models.PowerBi
{
    // Парметры для подключения к конкретному отчету в конкретной рабочей области
    public class Report
    {
        public Guid WorkspaceId { get; set; }
        public Guid ReportId { get; set; }
        public string DataSet { get; set; }
    }
}
