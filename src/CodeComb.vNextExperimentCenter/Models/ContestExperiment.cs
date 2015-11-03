﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace CodeComb.vNextExperimentCenter.Models
{
    public class ContestExperiment
    {
        [ForeignKey("Experiment")]
        public long ExperimentId { get; set; }

        public Experiment Experiment { get; set; }

        [ForeignKey("Contest")]
        public string ContestId { get; set; }

        public Contest Contest { get; set; }
    }
}
