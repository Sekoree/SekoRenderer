using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ScottPlot.Avalonia;
using ScottPlot.Drawing.Colorsets;
using SekoRenderer;

namespace RenderGraph.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private int _lookBefore = 0;
        private int _lookAfter = 0;
        private int _smoothIterations = 0;
        private double _uphillScaling = 1;
        private double _downhillScaling = 1;

        [Reactive] public string? FilePath { get; set; }

        public double[] FFTs { get; set; } = Array.Empty<double>();
        public double[] FFTsToUse { get; set; } = Array.Empty<double>();
        public Renderer Renderer { get; set; } = new();

        [Reactive] private AvaPlot AvaloniaPlot { get; set; } = new();

        public int LookBefore
        {
            get => _lookBefore;
            set
            {
                this.RaiseAndSetIfChanged(ref _lookBefore, value);
                UpdatePlot();
            }
        }

        public int LookAfter
        {
            get => _lookAfter;
            set
            {
                this.RaiseAndSetIfChanged(ref _lookAfter, value);
                UpdatePlot();
            }
        }

        public int SmoothIterations
        {
            get => _smoothIterations;
            set
            {
                this.RaiseAndSetIfChanged(ref _smoothIterations, value);
                UpdatePlot();
            }
        }

        public double UphillScaling
        {
            get => _uphillScaling;
            set
            {
                this.RaiseAndSetIfChanged(ref _uphillScaling, value);
                UpdatePlot();
            }
        }

        public double DownhillScaling
        {
            get => _downhillScaling;
            set
            {
                this.RaiseAndSetIfChanged(ref _downhillScaling, value);
                UpdatePlot();
            }
        }

        private Window GetMainWindow()
        {
            var appLifetime = Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            return appLifetime!.MainWindow;
        }

        public async Task SelectFileAsync()
        {
            var filter = new[] { "mp3", "m4a", "flac", "ogg", ".aac", ".wma", ".alac", ".wav", ".mp4" };
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
            var result = await openFileDialog.ShowAsync(GetMainWindow());
            if (result != null)
            {
                if (File.Exists(result[0]))
                {
                    FilePath = result[0];
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }

            AvaloniaPlot.Plot.Title($"Plot for {FilePath}");
            var tempFFTs = Renderer.DecodeSongSums(FilePath!);
            FFTs = tempFFTs.Select(x => (double)x).ToArray();
            UpdatePlot();
        }

        public void UpdatePlot()
        {
            if (FFTs.Length == 0)
            {
                return;
            }

            FFTsToUse = FFTs.ToArray();

            for (int i = 0; i < SmoothIterations; i++)
            {
                SmoothFFTs(FFTsToUse);
            }

            var uphills = new List<(int Start, int End)>();
            var downhills = new List<(int Start, int End)>();
            var between = new List<(int Start, int End)>();
            var j = 0;
            while (j < FFTsToUse.Length)
            {
                var current = FFTsToUse[j];
                var next = j + 1 < FFTsToUse.Length ? FFTsToUse[j + 1] : current;
                if (current == next)
                {
                    j++;
                    continue;
                }
                
                if (current > next)
                {
                    var first = current;
                    var second = next;
                    var start = j;
                    while (j < FFTsToUse.Length && first > second)
                    {
                        j++;
                        first = FFTsToUse[j];
                        second = j + 1 < FFTsToUse.Length ? FFTsToUse[j + 1] : first;
                    }
                    uphills.Add((start, j+ 1));
                }
                else if (current < next)
                {
                    var first = current;
                    var second = next;
                    var start = j;
                    while (j < FFTsToUse.Length && first < second)
                    {
                        j++;
                        first = FFTsToUse[j];
                        second = j + 1 < FFTsToUse.Length ? FFTsToUse[j + 1] : first;
                    }
                    downhills.Add((start, j + 1));
                }
                else
                {
                    var first = current;
                    var second = next;
                    var start = j;
                    while (j < FFTsToUse.Length && first == second)
                    {
                        j++;
                        first = FFTsToUse[j];
                        second = j + 1 < FFTsToUse.Length ? FFTsToUse[j + 1] : first;
                    }
                    between.Add((start, j + 1));
                }
            }

            var merged = new List<(int direction, int Start, int End)>();
            merged.AddRange(uphills.Select(x => (1, x.Start, x.End)));
            merged.AddRange(downhills.Select(x => (-1, x.Start, x.End)));
            merged.AddRange(between.Select(x => (0, x.Start, x.End)));
            merged.Sort((x, y) => x.Start.CompareTo(y.Start));
            
            AvaloniaPlot.Plot.Clear();

            foreach (var values in merged)
            {
                var color = values.direction == 1 ? Color.Blue : values.direction == -1 ? Color.Red : Color.Green;
                var valuesToUse = FFTsToUse.Skip(values.Start).Take(values.End - values.Start).ToArray();
                var plot = AvaloniaPlot.Plot.AddSignal(valuesToUse, color: color);
                plot.OffsetX = values.Start;
            }
            
            AvaloniaPlot.Refresh();
        }

        public (List<(int Start, int End)> Uphill, List<(int Start, int End)> Downhill) GetUphillAndDownhillSections(
            double[] ffts)
        {
            var uphillRanges = new List<(int start, int end)>();
            var downhillRanges = new List<(int start, int end)>();
            var lastDirection = -1; // 0 = uphill, 1 = downhill, -1 initial
            (int start, int end) lastRange = (0, 0);
            for (var i = 0; i < FFTsToUse.Length; i++)
            {
                var current = FFTsToUse[i];
                var next = i + 1 < FFTsToUse.Length ? FFTsToUse[i + 1] : 0;
                //if uphill
                if (current > next)
                {
                    if (lastDirection == 0)
                    {
                        lastRange.end = i;
                    }
                    else
                    {
                        if (lastRange.start != lastRange.end)
                        {
                            uphillRanges.Add(lastRange);   
                        }
                        lastRange = (i, i);
                    }
                    lastDirection = 0;
                }
                //if downhill
                else if (current < next)
                {
                    if (lastDirection == 1)
                    {
                        lastRange.end = i;
                    }
                    else
                    {
                        if (lastRange.start != lastRange.end)
                        {
                            downhillRanges.Add(lastRange);
                        }

                        lastRange = (i, i);
                    }
                    lastDirection = 1;
                }
            }

            //add last range
            switch (lastDirection)
            {
                case 1:
                    lastRange.end = FFTsToUse.Length - 1;
                    uphillRanges.Add(lastRange);
                    break;
                case 0:
                    lastRange.end = FFTsToUse.Length - 1;
                    downhillRanges.Add(lastRange);
                    break;
            }

            return (uphillRanges, downhillRanges);
        }

        private void SmoothFFTs(double[] ffts)
        {
            for (int i = 0; i < ffts.Length; i++)
            {
                var valueToDivide = 0.0;

                //Look Before
                for (int j = 0; j < LookBefore; j++)
                {
                    valueToDivide += (Convert.ToDouble(i - j)) < 0.0 ? 0.0 : ffts[i - j];
                }

                //Look After
                for (int j = 0; j < LookAfter; j++)
                {
                    valueToDivide += (Convert.ToDouble(i + j)) >= ffts.Length ? 0.0 : ffts[i + j];
                }

                var valueToDivideBy = (LookBefore + LookAfter) == 0 ? 1.0 : Convert.ToDouble(LookBefore + LookAfter);
                ffts[i] = (valueToDivide + ffts[i]) / valueToDivideBy;
            }
        }
    }
}