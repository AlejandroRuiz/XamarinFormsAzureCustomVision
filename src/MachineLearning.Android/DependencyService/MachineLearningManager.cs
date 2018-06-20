using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Android.Graphics;
using MachineLearning.DependencyServjce;
using MachineLearning.Droid.DependencyService;
using MachineLearning.Model;
using Xamarin.Forms;

[assembly: Dependency(typeof(MachineLearningManager))]
namespace MachineLearning.Droid.DependencyService
{
    public class MachineLearningManager : IMachineLearningManager
    {
        TaskCompletionSource<MachineLearningResult> _machineLearningTask;

        public Task<MachineLearningResult> AnalizeImageAsync(string mlModel, Stream imageStream)
        {
            if (_machineLearningTask != null)
            {
                _machineLearningTask.TrySetCanceled();
            }
            _machineLearningTask = new TaskCompletionSource<MachineLearningResult>();

            Task.Run(() =>
            {
                try
                {
                    mlModel = mlModel.Replace(".pb", "");
                    var imageClassifier = new ImageClassifier(mlModel);
                    BitmapFactory.Options options = new BitmapFactory.Options();
                    options.InMutable = true;
                    var mlResult = new MachineLearningResult();
                    using (var bitmap = BitmapFactory.DecodeStream(imageStream, null, options))
                    {
                        var result = imageClassifier.RecognizeImage(bitmap);
                        if(result != null)
                        {
                            foreach(var item in result)
                            {
                                mlResult.Observations.Add(new Observation { Confidence = item.Item1, Identifier = item.Item2 });
                            }
                        }
                        bitmap.Recycle();
                    }
                    _machineLearningTask.TrySetResult(mlResult);
                }
                catch (Exception error)
                {
                    Debug.WriteLine(error);
                    _machineLearningTask.TrySetResult(null);
                }
            });

            return _machineLearningTask.Task;
        }
    }
}
