﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Portal.Models.MSSQL.Calculator
{
    public class Items
    {
        [Key]
        public int RkCode { get; set; }
        public string Name { get; set; }
        public Guid ItemsGroup { get; set; }
        public double Coefficient { get; set; }
        public int Sequence { get; set; }
        public string DefrostTime { get; set; }
        public string BakingMode { get; set; }
        public int MinShowCase { get; set; }
        public int? SandwichOnBuns { get; set; }
        [Column("ReplacementGroups")]
        public int? ReplacementGroupsId { get; set; }
        [ForeignKey("ReplacementGroupsId")]
        public ReplacementGroups ReplacementGroups { get; set; }

    }
}
