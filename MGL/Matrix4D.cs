using System;

namespace MGL
{
   public class Matrix4D
   {
      private double[,] x;
      public const int dimm = 4;


      #region Constructors
      private Matrix4D()
      {
         x = new double[dimm, dimm];

         for( int i = 0; i < dimm; i++ )
            for( int j = 0; j < dimm; j++ )
               x[i, j] = 0;
      }

      public Matrix4D(double x11, double x12, double x13, double x14,   //constructor using 16 input arguments as fields
                      double x21, double x22, double x23, double x24,
                      double x31, double x32, double x33, double x34,
                      double x41, double x42, double x43, double x44)
      {
         x = new double[dimm, dimm];

         x[0,0] = x11;   x[0,1] = x12;   x[0,2] = x13;   x[0,3] = x14;
         x[1,0] = x21;   x[1,1] = x22;   x[1,2] = x23;   x[1,3] = x24;
         x[2,0] = x31;   x[2,1] = x32;   x[2,2] = x33;   x[2,3] = x34;
         x[3,0] = x41;   x[3,1] = x42;   x[3,2] = x43;   x[3,3] = x44;
      }

      public Matrix4D(double[] _x)    //constructor using an input array of doubles length 16
      {
         if( _x.GetLength(0) != dimm*dimm )
            throw new ArgumentException( String.Format("Input array dimensions must be equal to {0}x{0}", dimm) );
         
         
         x = new double[dimm, dimm];

         for( int i = 0; i < dimm; i++ )
            for( int j = 0; j < dimm; j++ )
               x[i, j] = _x[i*dimm + j];
      }

      public Matrix4D(double[,] _x)   //constructor using an input two-dimensional array of dimensions 4 by 4
      {
         if( _x.GetLength(0) != dimm || _x.GetLength(1) != dimm )
            throw new ArgumentException( String.Format("Input array dimensions must be equal to {0}x{0}", dimm) );
         
         
         x = new double[dimm, dimm];

         for( int i = 0; i < dimm; i++ )
            for( int j = 0; j < dimm; j++ )
               x[i, j] = _x[i, j];
      }

      public Matrix4D(Matrix4D A)     //copy constructor
      {
         x = new double[dimm, dimm];
         
         for( int i = 0; i < dimm; i++ )
            for( int j = 0; j < dimm; j++ )
               x[i, j] = A.x[i, j];
      }
      #endregion


      #region Special matrices
      public static Matrix4D zero   //matrix whose fields are all zero
      {
         get { return new Matrix4D(); }
      }
      public static Matrix4D I      //identity matrix (diagonal fields are one, all others are zero)
      {
         get {
            Matrix4D A = new Matrix4D();

            for( int i = 0; i < dimm; i++ )
               A.x[i, i] = 1;
                
            return A;
         }
      }
      #endregion


      #region Transformation matrices
      public static Matrix4D rotateX(double tx)   //roll  <=> counter-clockwise rotation about X-axis in radians
      {
         tx = Cmath.frac_part(tx, 2*Cmath.PI);
         
         if( Cmath.approx(tx, 0, Cmath.mip) )
            return I;
         
         Matrix4D A = I;
         
         //   X        Y        Z        W
         //[  1,       0,       0,       0  ]
         //[  0,     cos(t), -sin(t),    0  ]
         //[  0,     sin(t),  cos(t),    0  ]
         //[  0,       0,       0,       1  ]

         
         A.x[1,1] = Cmath.cos(tx);   A.x[1,2] = -Cmath.sin(tx);
         A.x[2,1] = Cmath.sin(tx);   A.x[2,2] =  Cmath.cos(tx);


         return A;
      }
      public static Matrix4D rotateY(double ty)   //pitch <=> counter-clockwise rotation about Y-axis in radians
      {
         ty = Cmath.frac_part(ty, 2*Cmath.PI);
         
         if( Cmath.approx(ty, 0, Cmath.mip) )
            return I;
         
         Matrix4D A = I;

         //   X        Y        Z        W
         //[ cos(t),   0,     sin(t),    0  ]
         //[  0,       1,       0,       0  ]
         //[-sin(t),   0,     cos(t),    0  ]
         //[  0,       0,       0,       1  ]


         A.x[0,0] =  Cmath.cos(ty);   A.x[0,2] = Cmath.sin(ty);
         A.x[2,0] = -Cmath.sin(ty);   A.x[2,2] = Cmath.cos(ty);


         return A;
      }
      public static Matrix4D rotateZ(double tz)   //yaw   <=> counter-clockwise rotation about Z-axis in radians
      {
         tz = Cmath.frac_part(tz, 2*Cmath.PI);
         
         if( Cmath.approx(tz, 0, Cmath.mip) )
            return I;
         
         Matrix4D A = I;

         //   X        Y        Z        W
         //[cos(t), -sin(t),    0,       0  ]
         //[sin(t),  cos(t),    0,       0  ]
         //[  0,       0,       1,       0  ]
         //[  0,       0,       0,       1  ]


         A.x[0,0] = Cmath.cos(tz);   A.x[0,1] = -Cmath.sin(tz);
         A.x[1,0] = Cmath.sin(tz);   A.x[1,1] =  Cmath.cos(tz);


         return A;
      }
      

      public static Matrix4D rotate(double tx, double ty, double tz)   //counter-clockwise rotations about X-axis, Y-axis and Z-axis, in that order
      {
         return rotateZ(tz)*rotateY(ty)*rotateX(tx);   //by convention
      }
      public static Matrix4D scale (double kx, double ky, double kz)   //scaling X, Y and Z-axes (from origin)
      {
         Matrix4D A = I;

         //   X        Y        Z        W
         //[  kx,      0,       0,       0  ]
         //[  0,       ky,      0,       0  ]
         //[  0,       0,       kz,      0  ]
         //[  0,       0,       0,       1  ]

         A.x[0,0] = kx;
         A.x[1,1] = ky;
         A.x[2,2] = kz;


         return A;
      }
      public static Matrix4D transl(double lx, double ly, double lz)   //translation on X, Y and Z-axes from origin
      {
         Matrix4D A = I;
         
         //   X        Y        Z        W
         //[  1,       0,       0,      lx  ]
         //[  0,       1,       0,      ly  ]
         //[  0,       0,       1,      lz  ]
         //[  0,       0,       0,       1  ]


         A.x[0,3] = lx;
         A.x[1,3] = ly;
         A.x[2,3] = lz;


         return A;
      }
      public static Matrix4D transl(Vector4D u)                        //translation by given vector from origin
      {
         return Matrix4D.transl(u.getnormx(), u.getnormy(), u.getnormz());
      }


      public static Matrix4D projXY(double d)   //perspective projection matrix (useful for drawing to monitor)
      {
         if( Cmath.approx(d, 0, Cmath.ulp) )
            throw new ArgumentException("Viewing plane cannot be singular");
         
         
         Matrix4D A = I;

         //   X        Y        Z        W
         //[  1,       0,       0,       0  ]      [ x ]       [  x  ]       [ x/z*d ]
         //[  0,       1,       0,       0  ]   *  [ y ]   =   [  y  ]   =   [ y/z*d ]
         //[  0,       0,       1,       0  ]      [ z ]       [  z  ]       [   d   ]
         //[  0,       0,      1/d,      0  ]      [ w ]       [ z/d ]       [   1   ]

         A.x[3,3] = 0;
         A.x[3,2] = 1/d;

         return A;
      }
      #endregion


      #region Getters
      public double[,] getx() => x;
      public double    getx(int row_index, int col_index)
      {
         if( row_index < 0 || row_index > dimm )
            throw new ArgumentException("Accessed element does not exist ( column index not in set [4] )");
         else if( col_index < 0 || col_index > dimm )
            throw new ArgumentException("Accessed element does not exist ( row index not in set [4] )");
         
         
         return x[row_index, col_index];
      }
      #endregion


      #region Matrix operators
      public static Matrix4D operator +(Matrix4D A, Matrix4D B) => new Matrix4D( Cmath.add(A.x, B.x) );
      public static Matrix4D operator -(Matrix4D A, Matrix4D B) => new Matrix4D( Cmath.sub(A.x, B.x) );
      

      public static Matrix4D operator +(Matrix4D A) => A;
      public static Matrix4D operator -(Matrix4D A) => new Matrix4D( Cmath.neg(A.x) );


      public static Matrix4D operator *(Matrix4D A, double k  ) => new Matrix4D( Cmath.mult(A.x, k) );
      public static Matrix4D operator *(double k,   Matrix4D A) => new Matrix4D( Cmath.mult(A.x, k) );
      public static Matrix4D operator *(Matrix4D A, Matrix4D B)
      {
         double[,] a = new double[dimm, dimm];

         for( int i = 0; i < dimm; i++ )
            for(int j = 0; j < dimm; j++ )
            {
               a[i, j] = 0;

               for (int k = 0; k < dimm; k++)
                  a[i, j] += A.x[i, k] * B.x[k, j];
            }

         //                      [b11, b12, b13, b14]
         //                      [b21, b22, b23, b24]
         //                      [b31, b32, b33, b34]
         //                      [b41, b42, b43, b44]
         //
         //[a11, a12, a13, a14]  [ ? ,   ,   ,   ,  ]    ? = a11*b11 + a12*b21 + a13*b31 + a14*b41
         //[a21, a22, a23, a24]  [   ,   ,   ,   ,  ]  - produkt mnozenja dve matrice
         //[a31, a32, a33, a34]  [   ,   ,   ,   ,  ]
         //[a41, a42, a43, a44]  [   ,   ,   ,   ,  ]

         return new Matrix4D(a);
      }
      
      
      public static Matrix4D operator /(Matrix4D A, double k)
      {
         if( Cmath.approx(k, 0, Cmath.ulp) )
            throw new ArgumentException("Matrix4D division by zero");
         
         
         return new Matrix4D( Cmath.div(A.x, k) );
      }


      public static Matrix4D transpose(Matrix4D A)   //returns a new matrix that is the transpose of the input matrix
      {
         double[,] a = new double[dimm, dimm];

         for( int i = 0; i < dimm; i++ )
            for( int j = 0; j < dimm; j++ )
               a[i, j] = A.x[j, i];

         return new Matrix4D(a);
      }
      #endregion


      #region Output
      public override string ToString()  => Cmath.write(x);       //outputs matrix fields rounded to the first couple of decimals
      public          string write_all() => Cmath.write_all(x);   //outputs matrix fields with maximum precision
      #endregion


      #region Testing
      public static void test1()
      {
         Console.WriteLine("---------------------------------------- <<<<<<<< Matrix4D test 1");

         double[,] arr1 = { { 1,  2,  3,  4}, { 5,  6,  7,  8}, { 9, 10, 11, 12}, {13, 14, 15, 16} };
         double[,] arr2 = { {16, 15, 14, 13}, {12, 11, 10,  9}, { 8,  7,  6,  5}, { 4,  3,  2,  1} };
         
         Matrix4D M1 = new Matrix4D(arr1);
         Matrix4D M2 = new Matrix4D(arr2);
         
         Console.WriteLine("M1 = {0}", M1);
         Console.WriteLine("M2 = {0}", M2);
         Console.WriteLine("write_all(M1) = {0}", M1);
         Console.WriteLine("write_all(M2) = {0}", M2);



         Console.WriteLine("----------------");

         Console.WriteLine();
         Console.WriteLine("M1+M2 = {0}",  M1 + M2);
         Console.WriteLine("M1-M2 = {0}",  M1 - M2);
         Console.WriteLine("+M1   = {0}", +M1     );
         Console.WriteLine("-M1   = {0}", -M1     );

         Console.WriteLine();
         Console.WriteLine("M1 * 2  = {0}", M1*2   );
         Console.WriteLine("2 * M1  = {0}", 2*M1   );
         Console.WriteLine("M1 * M2 = {0}", M1*M2  );
         Console.WriteLine("M1 / 2  = {0}", M1/2   );

         Console.WriteLine();
         Console.WriteLine("M1 * I    = {0}", M1 * Matrix4D.I   );
         Console.WriteLine("M1 * zero = {0}", M1 * Matrix4D.zero);

         Console.WriteLine();
         Console.WriteLine("transpose(M1) = {0}", Matrix4D.transpose(M1));



         Console.WriteLine("----------------");

         double pi = 3.1415;   //approximately PI
         double a  = 3;        //some random distinct values
         double b  = 5;
         double c  = 7;


         Console.WriteLine();
         Console.WriteLine("Matrix4D.rotateX({0}) = {1}", pi, Matrix4D.rotateX(pi));
         Console.WriteLine("Matrix4D.potateY({0}) = {1}", pi, Matrix4D.rotateY(pi));
         Console.WriteLine("Matrix4D.rotateZ({0}) = {1}", pi, Matrix4D.rotateZ(pi));


         Console.WriteLine();
         Console.WriteLine("Matrix4D.rotate({0}, {1}, {2}) = {3}", a, b, c, Matrix4D.rotate(a, b, c));
         Console.WriteLine("Matrix4D.transl({0}, {1}, {2}) = {3}", a, b, c, Matrix4D.transl(a, b, c));
         Console.WriteLine("Matrix4D.transl(new Vector4D({0}, {1}, {2})) = {3}", a, b, c, Matrix4D.transl( new Vector4D(a, b, c) ) );
         Console.WriteLine("Matrix4D.scale ({0}, {1}, {2}) = {3}", a, b, c, Matrix4D.scale (a, b, c));

         Console.WriteLine();
         Console.WriteLine("Matrix4D.projXY({0}) = {1}", b, Matrix4D.projXY(b));

      }
      #endregion


   }
}
