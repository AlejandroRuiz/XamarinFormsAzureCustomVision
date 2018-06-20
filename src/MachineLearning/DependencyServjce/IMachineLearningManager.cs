using System;
using System.Threading.Tasks;
using System.IO;
using MachineLearning.Model;

namespace MachineLearning.DependencyServjce
{
    public interface IMachineLearningManager
    {
        Task<MachineLearningResult> AnalizeImageAsync(string mlModel, Stream imageStream);
    }
}
