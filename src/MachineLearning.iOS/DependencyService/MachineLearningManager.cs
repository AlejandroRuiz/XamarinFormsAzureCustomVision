using System;
using System.IO;
using System.Threading.Tasks;
using CoreML;
using Foundation;
using MachineLearning.DependencyServjce;
using MachineLearning.iOS.DependencyService;
using MachineLearning.Model;
using Vision;
using Xamarin.Forms;
using System.Diagnostics;
using CoreFoundation;

[assembly: Dependency(typeof(MachineLearningManager))]
namespace MachineLearning.iOS.DependencyService
{
    public class MachineLearningManager : IMachineLearningManager
    {
        TaskCompletionSource<MachineLearningResult> _machineLearningTask;
        VNImageRequestHandler _classifierRequestHandler;

        public Task<MachineLearningResult> AnalizeImageAsync(string mlModel, Stream imageStream)
        {
            if (_machineLearningTask != null)
            {
                _machineLearningTask.TrySetCanceled();
                _classifierRequestHandler.Dispose();
                _classifierRequestHandler = null;
            }
            _machineLearningTask = new TaskCompletionSource<MachineLearningResult>();

            try
            {
                VNImageOptions options = new VNImageOptions();
                _classifierRequestHandler = new VNImageRequestHandler(NSData.FromStream(imageStream), options);
                _classifierRequestHandler.Perform(new VNRequest[] { GetClassificationRequest(mlModel) }, out var err);
                if (err != null)
                {
                    Debug.WriteLine(err);
                    _machineLearningTask.TrySetResult(null);
                }
            }
            catch (Exception error)
            {
                Debug.WriteLine(error);
                _machineLearningTask.TrySetResult(null);
            }

            return _machineLearningTask.Task;
        }

        VNRequest GetClassificationRequest(string resourceName)
        {
            resourceName = resourceName.Replace(".mlmodel", "").Replace(".mlmodelc", "");
            var modelPath = NSBundle.MainBundle.GetUrlForResource(resourceName, ".mlmodelc");
            NSError createErr, mlErr;
            var mlModel = MLModel.Create(modelPath, out createErr);
            var model = VNCoreMLModel.FromMLModel(mlModel, out mlErr);
            var classificationRequest = new VNCoreMLRequest(model, HandleClassifications);
            return classificationRequest;
        }

        void HandleClassifications(VNRequest request, NSError error)
        {
            DispatchQueue.MainQueue.DispatchAsync(() =>
            {
                var observations = request.GetResults<VNClassificationObservation>();
                if (observations == null)
                {
                    Debug.WriteLine("Error: no results");
                    _machineLearningTask.TrySetResult(null);
                    return;
                }

                var best = observations[0];
                if (best == null)
                {
                    Debug.WriteLine("Error: no observations");
                    _machineLearningTask.TrySetResult(null);
                    return;
                }

                var result = new MachineLearningResult();

                foreach(var observation in observations)
                {
                    result.Observations.Add(new Observation { Confidence = observation.Confidence, Identifier = observation.Identifier });
                }

                _machineLearningTask.TrySetResult(result);
            });
        }
    }
}
