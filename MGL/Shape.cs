using System;
using System.Collections.Generic;
using System.Drawing;

namespace MGL
{
   public class Shape
   {
      private List<Triangle> triangles;   //shape's faces (the keyword faces doesn't sound right)
      private Matrix4D transf;            //4D transformation matrix


      #region Constructors
      public Shape()
      {
         triangles = new List<Triangle>();
         transf    = Matrix4D.I;
      }

      //namerno je shallow-copy (nema smisla praviti novi niz trouglova od starog, pri konstrukciji shape-a)
      public Shape(List<Triangle> _triangles, Matrix4D _transf = null)
      {
         //converter lambda function ( X=>Y ) must be specified for function Array<>.ConvertAll()
         triangles = _triangles;

         transf = (_transf != null) ? _transf : Matrix4D.I;
      }

      //treba deep-copy, jer je copy-konstruktor
      public Shape(Shape S)
      {
         //converter lambda function ( X=>Y ) must be specified for function Array<>.ConvertAll()
         triangles = (S.triangles).ConvertAll( Triangle => new Triangle(Triangle) );

         transf = S.transf;
      }
      #endregion


      #region Common 2D shapes
      /*
      public static Shape rectangle(double ax, double az)   //x-length, z-width
      {
         if( ax <= 0 || az <= 0 )
            throw new ArgumentException("Both dimensions of rectangle must be greater than zero");
         
         Shape S = new Shape();
         


         return S;
      }
      public static Shape circle(double r)
      {
         return ;
      }
      */
      #endregion


      #region Common 3D shapes
      public static Shape quboid(double ax, double az, double ay, Color c)   //x-length, z-width, y-height
      {
         if( ax <= 0 || az <= 0 || ay <= 0 )
            throw new ArgumentException("All three dimensions of quboid must be greater than zero");

         Shape S = new Shape();

         Vector3D A = new Vector3D(-ax/2, -ay/2,  az/2 );
         Vector3D B = new Vector3D( ax/2, -ay/2,  az/2 );
         Vector3D C = new Vector3D( ax/2, -ay/2, -az/2 );
         Vector3D D = new Vector3D(-ax/2, -ay/2, -az/2 );

         Vector3D E = new Vector3D(-ax/2,  ay/2,  az/2 );
         Vector3D F = new Vector3D( ax/2,  ay/2,  az/2 );
         Vector3D G = new Vector3D( ax/2,  ay/2, -az/2 );
         Vector3D H = new Vector3D(-ax/2,  ay/2, -az/2 );



         //NAPOMENA - svi trouglovi su orijentisani counter-clockwise

         //A-B-C-D
         S.add(new Triangle(A, B, C, c));
         S.add(new Triangle(A, C, D, c));

         //E-F-G-H
         S.add(new Triangle(E, F, G, c));
         S.add(new Triangle(E, G, H, c));


         //A-B-F-E
         S.add(new Triangle(A, B, E, c));
         S.add(new Triangle(B, F, E, c));

         //C-D-H-G
         S.add(new Triangle(C, D, G, c));
         S.add(new Triangle(D, H, G, c));


         //B-C-G-F
         S.add(new Triangle(B, C, G, c));
         S.add(new Triangle(B, G, F, c));

         //D-A-E-H
         S.add(new Triangle(D, A, H, c));
         S.add(new Triangle(A, E, H, c));



         return S;
      }
      /*
      public static Shape tetrahedron(double a)
      {
         return ;
      }
      public static Shape sphere(double r)
      {
         return ;
      }
      public static Shape cyllider(double r, double hei)
      {
         return ;
      }
      public static Shape cone(double r, double hei)
      {
         return ;
      }
      */
      #endregion


      #region Getters
      public List<Triangle> get_triangles() => triangles.ConvertAll( Triangle => new Triangle(Triangle) );
      public Matrix4D get_transf() => new Matrix4D(transf);
      public Vector3D get_center()
      {
         Vector3D v = Vector3D.zero;
         for( int i = 0; i < triangles.Count; i++ )
            v += triangles[i].get_center();

         return v / triangles.Count;
      }
      #endregion


      #region Shape operators
      public static Shape operator *(Matrix4D M, Shape S)
         => new Shape( S.get_triangles().ConvertAll(Triangle => new Triangle(M * Triangle)) );
      #endregion


      #region Operations
      public void add(Triangle T)
      {
         if( T != null )
            triangles.Add(T);
      }
      public void add(List<Triangle> L)
      { triangles.AddRange(L); }
      public void add(Shape S)
      { triangles.AddRange(S.get_triangles()); }

      public bool remove    (Triangle item)             => triangles.Remove(item);
      public int  remove_all(Predicate<Triangle> match) => triangles.RemoveAll(match);
      public void clear()
      { triangles.Clear(); }
      
      public void transform(Matrix4D _transf)
      { transf = _transf * transf; }
      #endregion


      #region Output
      public override string ToString()  => String.Join("", triangles);
      public          string write_all() => String.Join("", triangles.ConvertAll( Triangle => Triangle.write_all() ));
      #endregion


      #region Testing
      public static void test1()
      {
         Console.WriteLine("----------------- <<<<<<<< Shape test 1");


         Vector3D v11 = new Vector3D(20, 50, 0);
         Vector3D v12 = new Vector3D(0, 50, 0);
         Vector3D v13 = new Vector3D(20, 50, 70);
         Color c1 = Color.Red;
         Triangle T1 = new Triangle(v11, v12, v13, c1);

         Vector3D v21 = new Vector3D(50, 10, 20);
         Vector3D v22 = new Vector3D(0, 50, 30);
         Vector3D v23 = new Vector3D(40, 50, 70);
         Color c2 = Color.Orange;
         Triangle T2 = new Triangle(v21, v22, v23, c2);

         Vector3D v31 = new Vector3D(20, 50, 0);
         Vector3D v32 = new Vector3D(0, 50, 0);
         Vector3D v33 = new Vector3D(20, 50, 70);
         Color c3 = Color.Violet;
         Triangle T3 = new Triangle(v31, v32, v33, c3);

         Shape S = new Shape();
         S.add(T1);
         S.add(T2);
         S.add(T3);



         Console.WriteLine("Shape S       = {0}", S            );
         Console.WriteLine();
         Console.WriteLine("S.write_all() = {0}", S.write_all());

         Console.WriteLine("----------------");

         Matrix4D M = Matrix4D.scale(2, 3, 4);
         Console.WriteLine("Shape S = {0}", S);
         Console.WriteLine("M = Matrix4D.scale(2, 3, 4) = {0}", M);

         Console.WriteLine("M*S = {0}", M * S);


      }
      #endregion


   }
}
