using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMath
{
    public class Matrix
    {
        // Matrix's data
        protected double[,] data;
        protected int n, m;

        // Constructors
        public Matrix(int n, int m)
        {
            data = new double[n, m];
            this.n = n;
            this.m = m;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    data[i, j] = 0;
        }
        public Matrix(int n, int m, Matrix M, int I, int J)
        {
            data = new double[n, m];
            this.n = n;
            this.m = m;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    data[i, j] = M[i + I, j + J];
        }
        public Matrix(int n, int m, double[] indat)
        {
            data = new double[n, m];
            this.n = n;
            this.m = m;
            if (indat.Length < n * m) throw new MatrixException("Initialization failed because input array is too small");
            // Horizontal data filling
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    data[i, j] = indat[i * m + j];
        }

        // Referencing properties
        public double this[int i, int j]
        {
            get { return data[i, j]; }
            set { data[i, j] = value; }
        }
        public int N { get { return n; } }
        public int M { get { return m; } }

        // Operator overloads
        public static Matrix operator *(Matrix a, Matrix b)
        {
            if (a.M != b.N) throw new MatrixException("Matrix multiplication failed because invalid inputs");
            Matrix c = new Matrix(a.N, b.M);
            for (int i = 0; i < c.N; i++)
                for (int j = 0; j < c.M; j++)
                    for (int k = 0; k < a.M; k++)
                        c[i, j] += a[i, k] * b[k, j];
            return c;
        }
        public static Matrix operator *(Matrix a, double b)
        {
            Matrix c = new Matrix(a.N, a.M);
            for (int i = 0; i < c.N; i++)
                for (int j = 0; j < c.M; j++)
                        c[i, j] += a[i, j] * b;
            return c;
        }
        public static Matrix operator *(double a, Matrix b)
        {
            return b * a;
        }
        public static Matrix operator /(Matrix a, double b)
        {
            return a * (1 / b);
        }
        public static Matrix operator +(Matrix a, Matrix b)
        {
            if (a.N != b.N || a.M != b.M) throw new MatrixException("Matrix addition failed because invalid inputs");
            Matrix c = new Matrix(a.N, a.M);
            for (int i = 0; i < c.N; i++)
                for (int j = 0; j < c.M; j++)
                    c[i, j] = a[i, j] + b[i, j];
            return c;
        }
        public static Matrix operator -(Matrix a, Matrix b)
        {
            if (a.N != b.N || a.M != b.M) throw new MatrixException("Matrix substraction failed because invalid inputs");
            Matrix c = new Matrix(a.N, a.M);
            for (int i = 0; i < c.N; i++)
                for (int j = 0; j < c.M; j++)
                    c[i, j] = a[i, j] - b[i, j];
            return c;
        }

        // Useful functions
        public Matrix Transpose
        {
            get
            {
                Matrix c = new Matrix(m, n);
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < m; j++)
                        c[j, i] = data[i, j];
                return c;
            }
        }
        public Matrix Inverse
        {
            get
            {
                if (n != m) throw new MatrixException("Matrix inversion failed bacause matrix is not square");
                Matrix a = new Matrix(n, 2 * n);
                a.CopyFrom(this, 0, 0);
                a.CopyFrom(Identity(n), 0, n);

                // Lower left triangle
                for (int k = 0; k < a.N; k++)
                {
                    // Find pivot for column k
                    int i_max = 0; double max = 0;
                    for (int i = k; i < a.N; i++)
                        if (Math.Abs(a[i, k]) > max) {
                            i_max = i;
                            max = Math.Abs(a[i, k]);
                        }
                    if (max == 0) throw new MatrixException("Matrix inversion failed because matrix is singular");
                    // Swap rows
                    for (int j = k; j < a.M; j++) {
                        double tmp = a[k, j];
                        a[k, j] = a[i_max, j];
                        a[i_max, j] = tmp;
                    }
                    // Do for all rows below pivot
                    for (int i = k + 1; i < a.N; i++)
                    {
                        // Do for all remaining elements in current row
                        for (int j = k + 1; j < a.M; j++)
                            a[i, j] = a[i, j] - a[k, j] * a[i, k] / a[k, k];
                        // Fill lower triangular matrix with zeros
                        a[i, k] = 0;
                    }
                }

                // Upper right triangle
                for (int k = a.N - 1; k >= 0; k--)
                {
                    // Normalize
                    for (int j = a.N; j < a.M; j++)
                        a[k, j] /= a[k, k];
                    a[k, k] = 1;

                    // Do for all rows above pivot
                    for (int i = 0; i < k; i++)
                    {
                        // Do for all remaining elements in current row
                        for (int j = a.N; j < a.M; j++)
                            a[i, j] = a[i, j] - a[k, j] * a[i, k];
                        // Fill upper triangular matrix with zeros
                        a[i, k] = 0;
                    }
                }

                return new Matrix(n, n, a, 0, n);
            }
        }
        public static Matrix Identity(int n)
        {
            Matrix c = new Matrix(n, n);
            for (int i = 0; i < n; i++)
                c[i, i] = 1;
            return c;
        }
        public void CopyFrom(Matrix m, int I, int J)
        {
            for (int i = 0; i < m.N; i++)
                for (int j = 0; j < m.M; j++)
                    data[i + I, j + J] = m[i, j];
        }
    }

    class MatrixException : Exception
    {
        public MatrixException(String emesg) : base(emesg) { }
    }
}
