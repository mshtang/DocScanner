using System;

namespace DocScanner.WPF.Extenstions
{
    public static class DoubleEx
    {
        public static double Clamp(this double val, double low, double high)
        {
            return Math.Clamp(val, low, high);
        }
    }
}
