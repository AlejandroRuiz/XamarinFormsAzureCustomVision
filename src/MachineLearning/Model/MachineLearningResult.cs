using System;
using System.Collections.Generic;
namespace MachineLearning.Model
{
    public class MachineLearningResult
    {
        public IList<Observation> Observations { get; } = new List<Observation>();
    }
}
