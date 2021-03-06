using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ScottPlot.Avalonia;
using SekoRenderer;

namespace RendererUI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly Renderer _renderer = new();

        public bool UseFFTPositions
        {
            get => _renderer.UseFFTPositions;
            set
            {
                _renderer.UseFFTPositions = value;
                this.RaisePropertyChanged();
            }
        }
        
        public bool UseImaginaryFFTValues
        {
            get => _renderer.UseImaginaryFFTValues;
            set
            {
                if (value)
                {
                    UseImaginaryAndRealAverage = false;
                    UseRealPlusImaginaryAddition = false;
                }
                _renderer.UseImaginaryFFTValues = value;
                this.RaisePropertyChanged();
            }
        }
        
        public bool UseImaginaryAndRealAverage
        {
            get => _renderer.UseImaginaryAndRealAverage;
            set
            {
                if (value)
                {
                    UseImaginaryFFTValues = false;
                    UseRealPlusImaginaryAddition = false;
                }
                _renderer.UseImaginaryAndRealAverage = value;
                this.RaisePropertyChanged();
            }
        }
        
        public bool UsePleaseHelpMe
        {
            get => _renderer.UsePleaseHelpMe;
            set
            {
                _renderer.UsePleaseHelpMe = value;
                this.RaisePropertyChanged();
            }
        }
        
        public bool UseRealPlusImaginaryAddition
        {
            get => _renderer.UseRealPlusImaginaryAddition;
            set
            {
                if (value)
                {
                    UseImaginaryAndRealAverage = false;
                    UseImaginaryFFTValues = false;
                }
                _renderer.UseRealPlusImaginaryAddition = value;
                this.RaisePropertyChanged();
            }
        }

        [Reactive] public ObservableCollection<string> FFTWindows { get; set; } = new();
        [Reactive] public string SelectedFFTWindow { get; set; }
        
        [Reactive] public string OutputPath { get; set; } = Directory.GetCurrentDirectory();
        [Reactive] public string FileToRender { get; set; } = string.Empty;

        [Reactive] public string OutputText { get; set; } = string.Empty;

        [Reactive] public bool ShowDoneText { get; set; } = false;

        [Reactive] public AvaPlot AvaPlot { get; set; } = new();
        
        public MainWindowViewModel()
        {
            var type = typeof(FastFourierTransform);
            //get static methods
            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
            //add to FFTWindows
            foreach (var method in methods)
            {
                if (!method.Name.StartsWith("FFT"))
                {
                    FFTWindows.Add(method.Name);
                }
            }
            SelectedFFTWindow = FFTWindows.First();
        }

        public async Task BrowseForFileAsync()
        {
            var window = ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow;
            var filter = new[] {"mp3", "m4a", "flac", "ogg", ".aac", ".wma", ".alac", ".wav", ".mp4"};
            var openFileDialog = new OpenFileDialog()
            {
                Title = "Select a song file",
                Filters = new List<FileDialogFilter>
                {
                    new()
                    {
                        Extensions = filter.ToList(),
                        Name = "AS2 Supported Files"
                    }
                }
            };
            var result = await openFileDialog.ShowAsync(window);
            if (result != null)
            {
                if (File.Exists(result[0]))
                {
                    FileToRender = result[0];   
                }
            }
        }
        
        public async Task BrowseForOutputPathAsync()
        {
            var window = ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow;
            var openFolderDialog = new OpenFolderDialog()
            {
                Title = "Select a folder to save the rendered file"
            };
            var result = await openFolderDialog.ShowAsync(window);
            if (result != null)
            {
                OutputPath = result;
            }
        }

        public async Task RenderSongAsync()
        {
            ShowDoneText = false;
            await Task.Delay(1000);
            if (!File.Exists(FileToRender) || !Directory.Exists(OutputPath))
            {
                OutputText = "Invalid file or output path";
                return;
            }
            var type = typeof(FastFourierTransform);
            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
            var selectedMethod = methods.First(x => x.Name == SelectedFFTWindow);
            //assign selected method to renderer
            _renderer.FFTWindow = selectedMethod.CreateDelegate(typeof(Renderer._fftWindow)) as Renderer._fftWindow ?? throw new InvalidOperationException();
            
            //var outputHash = _renderer.Md5HashFile(FileToRender);
            var sums = _renderer.DecodeSongSums(FileToRender);
            //_renderer.WriteAshFile(FileToRender, OutputPath, sums);

            //sums = sums.Take(10).ToList();
            
            //List<double> fftPositions = new();

            //for (int i = 0; i < sums.Count; i++)
            //{
            //    fftPositions.Add(i * 0.22f);
            //}
            //
            //double[] dataX = new double[] { 1, 2, 3, 4, 5 };
            //double[] dataY = new double[] { 1, 4, 9, 16, 25 };
            //AvaPlot.Plot.AddScatter(dataX, dataY);
            AvaPlot.Plot.Clear();

            var sumArr = sums.Select(x => (double)x).ToArray();
            for (int i = 0; i < 500; i++)
            {
                smoothArray(sumArr);
            }
            
            AvaPlot.Plot.AddSignal(sumArr);
            AvaPlot.Plot.Title("FFT of " + FileToRender);
            AvaPlot.Refresh();

            //OutputText = outputHash;
            ShowDoneText = true;
        } 
        
        public void smoothArray(double[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                var prevPrev = i > 1 ? arr[i - 2] : 0;
                var prev = i == 0 ? 0 : arr[i - 1];
                var next = i == arr.Length - 1 ? 0 : arr[i + 1];
                var nextNext = i < arr.Length - 2 ? arr[i + 2] : 0;
                var toDivideBy = 5;
                if (prevPrev == 0)
                {
                    toDivideBy--;
                }
                if (prev == 0)
                {
                    toDivideBy--;
                }
                if (next == 0)
                {
                    toDivideBy--;
                }
                if (nextNext == 0)
                {
                    toDivideBy--;
                }
                var avg = (prevPrev + prev + arr[i] + next + nextNext) / toDivideBy;
                arr[i] = avg;
            }
        }
    }
}