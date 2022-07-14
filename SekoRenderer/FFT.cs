using System;
using System.Numerics;

namespace SekoRenderer;

public class FastFourierTransform
{

    public static void FFT(bool forward, int m, ref Complex[] data)
    {
        int n, i, i1, j, k, i2, l, l1, l2;
        float c1, c2, tx, ty, t1, t2, u1, u2, z;

        // Calculate the number of points
        n = 1;
        for (i = 0; i < m; i++)
            n *= 2;

        // Do the bit reversal
        i2 = n >> 1;
        j = 0;
        for (i = 0; i < n - 1; i++)
        {
            if (i < j)
            {
                tx = (float)data[i].Real;
                ty = (float)data[i].Imaginary;
                data[i] = new Complex(data[j].Real, data[j].Imaginary);
                data[j] = new Complex(tx, ty);
            }

            k = i2;

            while (k <= j)
            {
                j -= k;
                k >>= 1;
            }

            j += k;
        }

        // Compute the FFT 
        c1 = -1.0f;
        c2 = 0.0f;
        l2 = 1;
        for (l = 0; l < m; l++)
        {
            l1 = l2;
            l2 <<= 1;
            u1 = 1.0f;
            u2 = 0.0f;
            for (j = 0; j < l1; j++)
            {
                for (i = j; i < n; i += l2)
                {
                    i1 = i + l1;
                    t1 = u1 * (float)data[i1].Real - u2 * (float)data[i1].Imaginary;
                    t2 = u1 * (float)data[i1].Imaginary + u2 * (float)data[i1].Real;
                    data[i1] = new Complex(data[i].Real - t1, data[i].Imaginary - t2);
                    data[i] = new Complex(data[i].Real + t1, data[i].Imaginary + t2);
                }

                z = u1 * c1 - u2 * c2;
                u2 = u1 * c2 + u2 * c1;
                u1 = z;
            }

            c2 = (float)Math.Sqrt((1.0f - c1) / 2.0f);
            if (forward)
                c2 = -c2;
            c1 = (float)Math.Sqrt((1.0f + c1) / 2.0f);
        }

        // Scaling for forward transform 
        if (forward)
        {
            for (i = 0; i < n; i++)
            {
                data[i] = new Complex(data[i].Real / n, data[i].Imaginary / n);
            }
        }
    }

    public static double DylanWindow(int n, int frameSize)
    {
        var val = (Convert.ToDouble(n) / Convert.ToDouble(frameSize) + 0.5) * -3.141592653589793;
        return val;
    }

    public static double DylanWindow2(int n, int frameSize)
    {
        var val = (Convert.ToDouble(n) * -6.283185307179586 / Convert.ToDouble(frameSize));
        return val;
    }

    public static double KISSFFTReversedWindow(int n, int frameSize)
    {
        double phase = -2 * 3.141592653589793 * n / frameSize;
        phase *= -1;
        return phase;
    }

    public static double KISSFFTWindow(int n, int frameSize)
    {
        double phase = -2 * 3.141592653589793 * n / frameSize;
        return phase;
    }

    public static double HammingWindow(int n, int frameSize)
    {
        return 0.54 - 0.46 * Math.Cos((2 * Math.PI * n) / (frameSize - 1));
    }

    public static double HannWindow(int n, int frameSize)
    {
        return 0.5 * (1 - Math.Cos((2 * Math.PI * n) / (frameSize - 1)));
    }

    public static double BlackmannHarrisWindow(int n, int frameSize)
    {
        return 0.35875 - (0.48829 * Math.Cos((2 * Math.PI * n) / (frameSize - 1))) +
               (0.14128 * Math.Cos((4 * Math.PI * n) / (frameSize - 1))) -
               (0.01168 * Math.Cos((6 * Math.PI * n) / (frameSize - 1)));
    }
}