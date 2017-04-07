using System;

namespace MGL
{
   public class Vector3D
   {
      private double x, y, z;


      #region Constructors
      private Vector3D()
      {
         x = 0;
         y = 0;
         z = 0;
      }

      public Vector3D(double _x, double _y, double _z)
      {
         x = _x;
         y = _y;
         z = _z;
      }
      
      public Vector3D(Vector3D u)
      {
         x = u.x;
         y = u.y;
         z = u.z;
      }
      #endregion


      #region Special vectors
      public static Vector3D zero
      {
         get { return new Vector3D(); }
      }
      public static Vector3D i
      {
         get { return new Vector3D(1, 0, 0); }
      }
      public static Vector3D j
      {
         get { return new Vector3D(0, 1, 0); }
      }
      public static Vector3D k
      {
         get { return new Vector3D(0, 0, 1); }
      }
      #endregion


      #region Casts
      public static implicit operator Vector4D( Vector3D u ) => new Vector4D(u);
      #endregion


      #region Getters
      public double getx() => x;
      public double gety() => y;
      public double getz() => z;
      #endregion


      #region Vector operators
      public          bool Equals(Vector3D u) => Cmath.approx(x, u.getx())
                                              && Cmath.approx(y, u.gety())
                                              && Cmath.approx(z, u.getz());
      public override bool Equals(object obj)
      {
         if( obj == null )   return false;

         Vector3D p = obj as Vector3D;
         if( p   == null )   return false;
         
         return Cmath.approx(x, p.getx())
             && Cmath.approx(y, p.gety())
             && Cmath.approx(z, p.getz());
      }

      public override int GetHashCode() => Convert.ToInt32(x) ^ Convert.ToInt32(y) ^ Convert.ToInt32(z);   //preporuceno od strane kompajlera da se napravi

      public static bool operator ==(Vector3D u, Vector3D v) =>  Cmath.approx(u.x, v.x)
                                                             &&  Cmath.approx(u.y, v.y)
                                                             &&  Cmath.approx(u.z, v.z);
      public static bool operator !=(Vector3D u, Vector3D v) => !Cmath.approx(u.x, v.x)
                                                             || !Cmath.approx(u.y, v.y)
                                                             || !Cmath.approx(u.z, v.z);
      
      
      public static Vector3D operator +(Vector3D u, Vector3D v) => new Vector3D(u.x + v.x,    u.y + v.y,    u.z + v.z);
      public static Vector3D operator -(Vector3D u, Vector3D v) => new Vector3D(u.x - v.x,    u.y - v.y,    u.z - v.z);

      public static Vector3D operator +(Vector3D u) => u;
      public static Vector3D operator -(Vector3D u) => new Vector3D(-u.x, -u.y, -u.z);
      
      
      public static Vector3D operator *(Vector3D u, double k  ) => new Vector3D(k*u.x,   k*u.y,   k*u.z);
      public static Vector3D operator *(double k,   Vector3D u) => new Vector3D(k*u.x,   k*u.y,   k*u.z);
      public static double   operator *(Vector3D u, Vector3D v) => u.x*v.x + u.y*v.y + u.z*v.z;


      public static Vector3D operator /(Vector3D u, double k  )
      {
         if( k == 0 )
            throw new ArgumentException("Vector3D division by zero");
         
         return new Vector3D(u.x/k,   u.y/k,   u.z/k);
      }


      public static Vector3D vect_mult(Vector3D u, Vector3D v)
            => new Vector3D( (u.y*v.z - v.y*u.z), -(u.x*v.z - v.x*u.z), (u.x*v.y - v.x*u.y) );
      
      //|i    j    k  |
      //|u.x  u.y  u.z|
      //|v.x  v.y  v.z|

      #endregion
      

      #region Vector properties
      public        double len()              => Math.Sqrt(x*x     + y*y     + z*z    );   //vraca duzinu vektora
      public static double len(Vector3D u)    => Math.Sqrt(u.x*u.x + u.y*u.y + u.z*u.z);   //      -||-
      public        double len_sq()           => x*x     + y*y     + z*z;       //vraca kvadrat duzine vektora
      public static double len_sq(Vector3D u) => u.x*u.x + u.y*u.y + u.z*u.z;   //             -||-
      #endregion


      #region Output
      public override string ToString()  => String.Format("[{0,3:G4}, {1,3:G4}, {2,3:G4}]", x, y, z);   //ispisuje u string koordinate 3D vektora, na samo prvih par decimala
      public          string write_all() => String.Format("[{0,3   }, {1,3   }, {2,3   }]", x, y, z);   //ispisuje u string koordinate 3D vektora, na sve decimale
      #endregion


      #region Testing
      public static void test1()
      {
         Console.WriteLine("----------------- <<<<<<<< Vector3D test 1");

         Vector3D v1 = new Vector3D( 2, 3, 4 );   //kreiranje vektora sa int koordinatama
         Vector3D v2 = new Vector3D( 3, 4, 5 );   //                -||-
         Console.WriteLine("v1 = {0}", v1);
         Console.WriteLine("v2 = {0}", v2);

         Console.WriteLine();
         Console.WriteLine("v1 + v2 = {0}", v1 + v2);   //sabiranje  vektora
         Console.WriteLine("v1 - v2 = {0}", v1 - v2);   //oduzimanje vektora

         Console.WriteLine();
         Console.WriteLine("+v1 = {0}", +v1);   //unarna operacija pozitivni znak
         Console.WriteLine("-v1 = {0}", -v1);   //unarna operacija negativni znak

         Console.WriteLine();
         Console.WriteLine("v1 * 5  = {0}", v1 * 5);   //mnozenje vektora skalarom
         Console.WriteLine("5 * v1  = {0}", 5 * v1);   //          -||-
         Console.WriteLine("v1 / 5  = {0}", v1 / 5);   //deljenje vektora skalarom

         Console.WriteLine();
         Console.WriteLine("v1  * v2  = {0}", v1  * v2 );                     //skalarni  proizvod
         Console.WriteLine("5*i * 4*j = {0}", 5*i * 4*j);                     //      -||-
         Console.WriteLine("v1  x v2  = {0}", Vector3D.vect_mult(v1,  v2 ));  //vektorski proizvod
         Console.WriteLine("5*i x 4*j = {0}", Vector3D.vect_mult(5*i, 4*j));  //      -||-



         Console.WriteLine("----------------");

         Console.WriteLine("(Vector4D) v1 = {0}", (Vector4D) v1);   //cast u Vector4D
         Console.WriteLine("v1.x = {0}", v1.getx());   //vrednost x koordinate
         Console.WriteLine("v1.y = {0}", v1.gety());   //vrednost y koordinate
         Console.WriteLine("v1.z = {0}", v1.getz());   //vrednost z koordinate

         Console.WriteLine();
         Console.WriteLine("v1.len()            = {0,3:G4}", v1.len()        );   //duzina vektora
         Console.WriteLine("Vector3D.len(v1)    = {0,3:G4}", Vector3D.len(v1));   //staticki pozvana duzina vektora

         Console.WriteLine();
         Console.WriteLine("v1.len_sq()         = {0}", v1.len_sq()        );   //kvadrat duzine vektora
         Console.WriteLine("Vector3D.len_sq(v1) = {0}", Vector3D.len_sq(v1));   //staticki pozvan kvadrat duzine vektora



         Console.WriteLine("----------------");

         Vector3D v3 = new Vector3D(1.3123123, 50789780.423424, 0.000023423);   //kreiranje vektora sa proizvoljnim float koordinatama
         Vector3D v4 = new Vector3D(Vector3D.i + Vector3D.j + Vector3D.k);      //kreiranje vektora koji se dobije kao zbir jedinicnih vektora i, j, k
         Vector3D v5 = new Vector3D(Vector3D.zero);                             //kreiranje nula vektora

         Console.WriteLine("random vektor = {0}", v3);
         Console.WriteLine("random vektor = {0}", v3.write_all());
         Console.WriteLine("i+j+k = {0}", v4);
         Console.WriteLine("vnull = {0}", v5);



         Console.WriteLine("----------------");

         Vector3D v6 = new Vector3D( 2, 3, 4        );
         Vector3D v7 = new Vector3D( 2, 3, 4.000001 );
         Vector3D v8 = new Vector3D( 2, 3, 4        );
         Vector3D v9 = new Vector3D( 2, 3, 4.0001   );

         Console.WriteLine("{0} == {1} ? {2}", v6.write_all(), v7.write_all(), v6 == v7);
         Console.WriteLine("{0} != {1} ? {2}", v6.write_all(), v7.write_all(), v6 != v7);
         Console.WriteLine("{0} == {1} ? {2}", v8.write_all(), v9.write_all(), v8 == v9);
         Console.WriteLine("{0} != {1} ? {2}", v8.write_all(), v9.write_all(), v8 != v9);

      }
      #endregion


   }
}
