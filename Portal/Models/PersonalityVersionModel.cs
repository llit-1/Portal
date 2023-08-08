using Microsoft.CodeAnalysis;
using Portal.Models.MSSQL.Personality;
using Portal.Models.MSSQL.PersonalityVersions;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Portal.Models
{
    public class PersonalityVersionModel
    {
        public Personality Personality { get; set; }
        public List<PersonalityVersion> PersonalitiesVersions { get; set; }
        public string NewPerson { get; set; }
        public List<Guid> Errors { get; set; }
    }
}
