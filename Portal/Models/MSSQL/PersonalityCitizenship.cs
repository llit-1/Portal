using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace Portal.Models.MSSQL
{
    [ComplexType]
    public class PersonalityCitizenship
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }

}
