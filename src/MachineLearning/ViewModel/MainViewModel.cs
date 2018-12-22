using System;
using MvvmHelpers;
using System.Windows.Input;
using Xamarin.Forms;
using System.Collections.Generic;
using Plugin.Media;
using Acr.UserDialogs;
using Plugin.Media.Abstractions;
using MachineLearning.DependencyServjce;
using System.Linq;
using Plugin.SimpleAudioPlayer;

namespace MachineLearning.ViewModel
{
    public class MainViewModel : BaseViewModel
    {
        const string ML_MODEL_NAME = "Pokemon";
        const string PIKACHU_AUDIO_NAME = "Pikachu.mp3";

        ImageSource _currentPhoto;
        private string _results;
        readonly IMachineLearningManager _machineLearningManager;

        public MainViewModel()
        {
            DetectCommand = new Command(InvokeDetectCommand);
            _machineLearningManager = DependencyService.Get<IMachineLearningManager>();
        }

        public ICommand DetectCommand { get; }

        public ImageSource CurrentPhoto
        {
            get => _currentPhoto;
            set => SetProperty(ref _currentPhoto, value);
        }

        public string Results
        {
            get => _results;
            set => SetProperty(ref _results, value);
        }

        async void InvokeDetectCommand()
        {
            if(CrossSimpleAudioPlayer.Current.IsPlaying)
            {
                CrossSimpleAudioPlayer.Current.Stop();
            }
            var actions = new List<string>();
            if (CrossMedia.IsSupported)
            {
                if (CrossMedia.Current.IsPickPhotoSupported)
                {
                    actions.Add("Pick Photo");
                }
                if (CrossMedia.Current.IsTakePhotoSupported)
                {
                    actions.Add("Take Photo");
                }
            }
            var result = await UserDialogs.Instance.ActionSheetAsync("Select an option", "Cancel", null, null, actions.ToArray());
            MediaFile file = null;
            if (result == "Pick Photo")
            {
                file = await CrossMedia.Current.PickPhotoAsync();
            }
            else if (result == "Take Photo")
            {
                file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions());
            }

            if (file != null)
            {
                CurrentPhoto = ImageSource.FromStream(() => file.GetStream());
                UserDialogs.Instance.ShowLoading("Running ML Script", MaskType.Gradient);

                var mlResult = await _machineLearningManager.AnalizeImageAsync(ML_MODEL_NAME, file.GetStream());

                UserDialogs.Instance.HideLoading();
                if (mlResult != null)
                {
                    Results = $"Result:{Environment.NewLine}{ string.Join(Environment.NewLine, mlResult.Observations.Select(item => $"{item.Identifier} {item.Confidence}"))}";
                    if (mlResult.Observations.Any(item => item.Identifier?.ToLower() == "pikachu" && item.Confidence > 0.8))
                    {
                        CrossSimpleAudioPlayer.Current.Load(PIKACHU_AUDIO_NAME);
                        CrossSimpleAudioPlayer.Current.Play();
                    }
                    else
                    {
                        UserDialogs.Instance.Alert("No Pikachu Detected :(", null, "Ok");
                    }
                }
                else
                {
                    Results = "No Result";
                }
            }
        }
    }
}
