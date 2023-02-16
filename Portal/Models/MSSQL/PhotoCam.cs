using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Models.MSSQL
{
    public class PhotoCam
    {
        public int Id { get; set; }
        public int TTCode { get; set; }
        public string TTName { get; set; }
        public DateTime dateTime { get; set; }
        public int camId { get; set; }
        public string camName { get; set; }
        public int groupId { get; set; }
        public string groupName { get; set; }
        public byte[] Image { get; set; }
    }
}
