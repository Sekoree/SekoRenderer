using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ManagedBass;

namespace SekoRenderer;

public class Renderer
{
    public bool UseFFTPositions { get; set; }
    public bool UseImaginaryFFTValues { get; set; }
    public bool UseImaginaryAndRealAverage { get; set; }
    public bool UsePleaseHelpMe { get; set; } = false;

    public delegate double _fftWindow(int n, int frameSize);
    public _fftWindow FFTWindow { get; set; }

    private int _fftResolution;

    public Renderer(int fftResolution = 512)
    {
        UseFFTPositions = true;
        UseImaginaryFFTValues = true;
        UseImaginaryAndRealAverage = false;
        FFTWindow = FastFourierTransform.HannWindow;
        _fftResolution = fftResolution;
        var could = Bass.Init();
        Console.WriteLine("Bass.Init returned " + could);
    }

    public Renderer(ref _fftWindow fftWindow, bool useFFTPositions = true, bool useImaginaryFFTValues = true, bool useImaginaryAndRealAverage = false, int fftResolution = 512)
    {
        UseFFTPositions = useFFTPositions;
        UseImaginaryFFTValues = useImaginaryFFTValues;
        UseImaginaryAndRealAverage = useImaginaryAndRealAverage;
        FFTWindow = fftWindow;
        _fftResolution = fftResolution;
        var could = Bass.Init();
        Console.WriteLine("Bass.Init returned " + could);
    }
    
    public string Md5HashFile(string fileName)
    {
        using FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        var mD5CryptoServiceProvider = MD5.Create();
        byte[] retVal = mD5CryptoServiceProvider.ComputeHash(file);

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < retVal.Length; i++)
        {
            sb.Append(retVal[i].ToString("x2"));
        }

        var text2 = Regex.Replace(sb.ToString(), "\\W*", string.Empty);
        int num2 = 40;
        if (text2.Length > num2)
        {
            text2 = text2.Remove(num2, text2.Length - num2 - 1);
        }

        Console.WriteLine(text2);
        return text2 + ".asa";
    }

    public void WriteAshFile(string path, string outputFolder, List<float> sums)
    {
        string ashFullPath = Md5HashFile(path);
        Console.WriteLine("Path is " + Path.Combine(outputFolder, ashFullPath));
        FileStream fileStream = null;

        try
        {
            fileStream = File.Open(Path.Combine(outputFolder, ashFullPath), FileMode.Create,
                FileAccess.Write);
        }
        catch (Exception ex)
        {
            Console.WriteLine("File.Open failed in WriteAshFile");
            Console.WriteLine(ex.ToString());
        }

        sbyte[]? shape = null;
        int numShapeNodes = _fftResolution / 2;
        Console.WriteLine("Creating shape");
        if (fileStream != null)
        {
            BinaryWriter binaryWriter = new BinaryWriter(fileStream);
            if (shape == null)
            {
                shape = new sbyte[numShapeNodes];
            }

            Console.WriteLine("Writing shape");

            if (shape.Length != numShapeNodes)
            {
                Console.WriteLine("track _shape was not length 256 in ashbank.  Reset to defaults.");
                shape = new sbyte[numShapeNodes];
            }

            Console.WriteLine("Writing shapeNodes");

            for (int i = 0; i < numShapeNodes; i++)
            {
                binaryWriter.Write(shape[i]);
            }

            Console.WriteLine("Writing sums");

            binaryWriter.Write(sums.Count);
            foreach (var t in sums)
            {
                binaryWriter.Write(t);
            }

            Console.WriteLine("Done writing");

            binaryWriter.Close();
            fileStream.Dispose();
        }
    }
    
    public List<float> DecodeSongSums(string path)
    {
        var fft = new float[_fftResolution];
        
        var chan = Bass.CreateStream(path, Flags: BassFlags.Decode | BassFlags.Float | BassFlags.Prescan);
        
        Console.WriteLine("Prescan2 complete");
        var decodeSums = new List<float>();
        Console.WriteLine("FFT complete");
        Console.WriteLine("FFT length: " + fft.Length);
        
        while (true)
        {
            var num3 = FastDecodeStepAsync(chan, fft);
            if (num3 < 0f)
            {
                break;
            }
            decodeSums.Add(num3);
        }

        return decodeSums;
    }
    
    private float FastDecodeStepAsync(int chan, float[] ffts)
    {
        int fftPos = 0;
        var defaultData = DataFlags.FFT1024;
        if (_fftResolution == 128)
            defaultData = DataFlags.FFT256;
        else if (_fftResolution == 256)
            defaultData = DataFlags.FFT512;
        else if (_fftResolution == 1024)
            defaultData = DataFlags.FFT2048;
        else if (_fftResolution == 2048)
            defaultData = DataFlags.FFT4096;
        else if (_fftResolution == 4096)
            defaultData = DataFlags.FFT8192;
        else if (_fftResolution == 8192)
            defaultData = DataFlags.FFT16384;
        else if (_fftResolution == 16384)
            defaultData = DataFlags.FFT32768;
        
        var data = Bass.ChannelGetData(chan, ffts, (int)defaultData);

        var complexFFTs = new Complex[ffts.Length];
        for (int i = 0; i < ffts.Length; i++)
        {
            complexFFTs[i] = new Complex(ffts[i] * FFTWindow.Invoke(fftPos, _fftResolution), 0);
            if (UseFFTPositions)
            {
                fftPos++;   
            }
        }
        
        FastFourierTransform.FFT(false, (int)Math.Log(_fftResolution, 2.0), ref complexFFTs);
        
        
        if (UsePleaseHelpMe)
        {
            //get avg of real and imaginary parts
            //var avg = (ulong)complexFFTs[i];
            var the3CThing = new Complex();
            complexFFTs[0] = new Complex(0, 0);
            complexFFTs[1] = new Complex(0, 0);
            for (var i = 0; i < complexFFTs.Length; i++)
            {
                var current = complexFFTs[i];
                var next = new Complex();
                if (i + 1 < complexFFTs.Length)
                {
                    next = complexFFTs[i + 1];
                }

                var sqrt = Complex.Sqrt(current * current + next * next) * 0.0009765625f;
                complexFFTs[i] = sqrt;
                
                if (sqrt.Real <= the3CThing.Real)
                {
                    sqrt = the3CThing;
                }
                the3CThing = sqrt;
            }
        }
        
        int fFT512KISS = data;
        if (fFT512KISS < 1)
        {
            return -1f;
        }

        float num = 0f;
        for (int i = 1; i < _fftResolution; i++)
        {
            if (UseImaginaryAndRealAverage)
            {
                var avg = (complexFFTs[i].Real + complexFFTs[i].Imaginary) / 2;
                num += (float)Math.Sqrt(Math.Max(0f, avg));
            }
            else if (UseImaginaryFFTValues && !UsePleaseHelpMe)
            {
                num += (float)Math.Sqrt(Math.Max(0f, complexFFTs[i].Imaginary));
            }
            else
            {
                num += (float)Math.Sqrt(Math.Max(0f, complexFFTs[i].Real));
            }
        }

        return Math.Max(0f, num);
    }
}