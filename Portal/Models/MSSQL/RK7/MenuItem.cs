using Microsoft.EntityFrameworkCore;
using System;

namespace Portal.Models.MSSQL.RK7
{
    [Keyless]
    public class MenuItem
    {
        public int Code { get; set; }
        public string Name { get; set; }
        public Int16 Status { get; set; }
    }
}
