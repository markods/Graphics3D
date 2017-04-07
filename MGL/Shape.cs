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

      //mogao je i shallow-copy, ali je ipak bolje ovako
      public Shape(List<Triangle> _triangles, Matrix4D _transf = null)
      {
         //converter lambda function ( X=>Y ) must be specified for function Array<>.ConvertAll()
         triangles = _triangles.ConvertAll(Triangle => new Triangle(Triangle));

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


      #region Common 3D shapes
      /*
      public static Shape quboid(double length, double width, double hei)   //x-length, z-width, y-height
      {
         get
         {
            return ;
         }
      }
      public static Shape tetrahedron(double a)
      {
         get
         {
            return ;
         }
      }
      public static Shape sphere(double radius)
      {
         get
         {
            return ;
         }
      }
      */
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
      {
         triangles.AddRange(S.get_triangles());
      }

      public bool remove    (Triangle item)             => triangles.Remove(item);
      public int  remove_all(Predicate<Triangle> match) => triangles.RemoveAll(match);
      public void clear()
      { triangles.Clear(); }
      
      public void transform(Matrix4D _transf)
      { transf = _transf * transf; }
      #endregion


      #region Getters
      public List<Triangle> get_triangles() => triangles.ConvertAll( Triangle => new Triangle(Triangle) );
      public Matrix4D get_transf() => new Matrix4D(transf);
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
         Triangle t1 = new Triangle(v11, v12, v13, c1);

         Vector3D v21 = new Vector3D(50, 10, 20);
         Vector3D v22 = new Vector3D(0, 50, 30);
         Vector3D v23 = new Vector3D(40, 50, 70);
         Color c2 = Color.Orange;
         Triangle t2 = new Triangle(v21, v22, v23, c2);

         Vector3D v31 = new Vector3D(20, 50, 0);
         Vector3D v32 = new Vector3D(0, 50, 0);
         Vector3D v33 = new Vector3D(20, 50, 70);
         Color c3 = Color.Violet;
         Triangle t3 = new Triangle(v31, v32, v33, c3);

         Shape s = new Shape();
         s.add(t1);
         s.add(t2);
         s.add(t3);



         Console.WriteLine("Shape s       = {0}", s            );
         Console.WriteLine();
         Console.WriteLine("s.write_all() = {0}", s.write_all());

         Console.WriteLine("-----------------");

      }
      #endregion


   }
}
