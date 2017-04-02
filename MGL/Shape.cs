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


      #region Special shapes
      //---------------------------------------!!!!!!!!!!!!!! tetrahedron, qube, kvadar?, sphere
      #endregion


      #region Operations
      public void add(Triangle T)
      {
         if( T != null )
            triangles.Add(T);
      }
//      public void add(List<Triangle> L)   //---------------------------------------!!!!!!!!!!!!!!
//      public bool remove() { }            //---------------------------------------!!!!!!!!!!!!!!
//      public bool remove_all() { }        //---------------------------------------!!!!!!!!!!!!!! prema navedenom kriterijumu

      public void clear()
      {
         triangles.Clear();
      }
      public void transform(Matrix4D _transf)
      {
         transf = _transf * transf;
      }
      #endregion


      #region Getters
      public List<Triangle> get_triangles() => triangles.ConvertAll( Triangle => new Triangle(Triangle) );
      public Matrix4D get_transf() => new Matrix4D(transf);
      #endregion


      #region Output
//      public override string ToString()  => ;   //---------------------------------------!!!!!!!!!!!!!!
//      public          string write_all() => ;   //---------------------------------------!!!!!!!!!!!!!!
      #endregion


      #region Testing
      public static void test1()   //---------------------------!!!!!!!!!!!!!!!!!!!!
      {
         Console.WriteLine("----------------- <<<<<<<< Shape test 1");

         Vector3D v11 = new Vector3D(1, 2, 3);
         Vector3D v12 = new Vector3D(4, 5, 6);
         Vector3D v13 = new Vector3D(7, 8, 9);
         Color c1 = Color.Red;

         Triangle T1 = new Triangle(v11, v12, v13, c1);


         Vector3D v21 = new Vector3D(11, 12, 13);
         Vector3D v22 = new Vector3D(14, 15, 16);
         Vector3D v23 = new Vector3D(17, 18, 19);
         Color c2 = Color.Red;

         Triangle T2 = new Triangle(v21, v22, v23, c2);


         Shape S = new Shape();



//         Console.WriteLine("Shape = {0}", S            );
//         Console.WriteLine("Shape = {0}", S.write_all());

//         Console.WriteLine("----------------");

      }
      #endregion


   }
}
