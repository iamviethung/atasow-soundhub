using System;
using System.Collections.Generic;
using System.Text;

using System.Numerics;

namespace AMath
{
    public class DiscreteFourierTransform
    {
        public static Complex[] Transform(Complex[] input)
        {
            int N = input.Length;
            Complex[] output = new Complex[N];

            Complex[] expTable = new Complex[N];
            for (int i = 0; i < N; i++)
                expTable[i] = Complex.Exp(new Complex(0, - 2 * Math.PI * i / N));

            for (int k = 0; k < N; k++) {
                output[k] = new Complex();
                for (int n = 0; n < N; n++)
                    output[k] += input[n] * expTable[(k * n) % N];
            }

            return output;
        }
    }
}
