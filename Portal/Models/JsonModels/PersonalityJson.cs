    using System.ComponentModel.DataAnnotations.Schema;
    using System.ComponentModel.DataAnnotations;
    using System;
    using global::Portal.Models.MSSQL.Personality;

    namespace Portal.Models.JsonModels
    {
        public class PersonalityJson
        {
            public Guid Guid { get; set; }
            public string Name { get; set; }
            public string Surname { get; set; }
            public string Patronymic { get; set; }
            public DateTime BirthDate { get; set; }
            public Guid JobTitle { get; set; }
            public Guid Location { get; set; }
            public DateTime HireDate { get; set; }
            public DateTime? DismissalsDate { get; set; }
            public Guid? Schedule { get; set; }
            public Guid? Entity { get; set; }
            public Guid? EntityCost { get; set; }
            public int Actual { get; set; }
            public DateTime? VersionStartDate { get; set; }
            public DateTime? VersionEndDate { get; set; }
            public string NewPerson { get; set; }
            public string personGUID { get; set; }
            public string Tel { get; set; }
            public string SNILS { get; set; }
            public string INN { get; set; }
            public string Email { get; set; }
            public int PartTimer { get; set; }
    }
    }

