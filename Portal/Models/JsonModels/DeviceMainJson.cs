    using System.ComponentModel.DataAnnotations.Schema;
    using System.ComponentModel.DataAnnotations;
    using System;
    using global::Portal.Models.MSSQL.Personality;
using Portal.Models.MSSQL;
using System.Collections.Generic;

namespace Portal.Models.JsonModels
{
    public class DeviceMainJson
    {
        public List<VideoDevices> videoDevices { get; set; }
        public List<VideoOrientation> videoOrientation { get; set; }
    }
}

