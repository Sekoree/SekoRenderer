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
                }
                _renderer.UseImaginaryAndRealAverage = value;
                this.RaisePropertyChanged();
            }
        }

        [Reactive] public ObservableCollection<string> FFTWindows { get; set; } = new();
        [Reactive] public string SelectedFFTWindow { get; set; }
        
        [Reactive] public string OutputPath { get; set; } = Directory.GetCurrentDirectory();
        [Reactive] public string FileToRender { get; set; } = string.Empty;

        [Reactive] public string OutputText { get; set; } = string.Empty;

        [Reactive] public bool ShowDoneText { get; set; } = false;
        
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

        public void RenderSongAsync()
        {
            ShowDoneText = false;
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
            
            var outputHash = _renderer.Md5HashFile(FileToRender);
            var sums = _renderer.DecodeSongSums(FileToRender);
            _renderer.WriteAshFile(FileToRender, OutputPath, sums);
            OutputText = outputHash;
            ShowDoneText = true;
        } 
    }
}