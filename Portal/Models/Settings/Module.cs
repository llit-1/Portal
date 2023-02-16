using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Portal.Models.Settings
{
    public class Module
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Enabled { get; set; }
        public string StartTime { get; set; }
        public string StopTime { get; set; }
        public string Interval { get; set; }
    }
}
