using System;
using System.Drawing;

namespace MGL
{
   public class Triangle
   {
      private Vector3D[] vertex;
      private Color color;

      private const int ver_cnt = 3;


      #region Constructors
      public Triangle(Vector3D vertex1, Vector3D vertex2, Vector3D vertex3, Color _color)   //counter-clockwise order of vertices
      {
         vertex = new Vector3D[3];

         vertex[0] = new Vector3D(vertex1);
         vertex[1] = new Vector3D(vertex2);
         vertex[2] = new Vector3D(vertex3);

         color = _color;
      }

      public Triangle(Triangle t)
      {
         vertex = new Vector3D[3];

         vertex[0] = new Vector3D(t.vertex[0]);
         vertex[1] = new Vector3D(t.vertex[1]);
         vertex[2] = new Vector3D(t.vertex[2]);

         color = t.color;
      }
      #endregion


      #region Getters
      public Vector3D getv(int index)
      {
         if( index < 0 || index > ver_cnt )
            throw new ArgumentException("Index of vertex is not in set [3]");
         
         return new Vector3D(vertex[index]);
      }
      public Color get_color() => color;
      #endregion


      #region Matrix-Triangle operators
      public static Triangle operator *(Matrix4D M, Triangle T)
         => new Triangle(M * (Vector4D) T.getv(0),
                         M * (Vector4D) T.getv(1),
                         M * (Vector4D) T.getv(2), T.get_color());
      #endregion


      #region Output
      public override string ToString()
         => String.Format("\n{0}, {1}, {2}; {3}",
                          vertex[0], vertex[1], vertex[2], color);
      public          string write_all()
         => String.Format("\n{0}, {1}, {2}; {3}",
                          vertex[0].write_all(), vertex[1].write_all(), vertex[2].write_all(), color);
      #endregion
      
      
      #region Testing
      public static void test1()
      {
         Console.WriteLine("----------------- <<<<<<<< Triangle test 1");

         Vector3D v1 = new Vector3D(1, 2, 3);
         Vector3D v2 = new Vector3D(4, 5, 6);
         Vector3D v3 = new Vector3D(7, 8, 9);
         Color c = Color.Red;

         Triangle T = new Triangle(v1, v2, v3, c);


         Console.WriteLine("Triangle T    = {0}", T            );
         Console.WriteLine("T.write_all() = {0}", T.write_all());


         Console.WriteLine("----------------");

         Console.WriteLine("T.getv(0) = {0}", T.getv(0));
         Console.WriteLine("T.getv(1) = {0}", T.getv(1));
         Console.WriteLine("T.getv(2) = {0}", T.getv(2));
         Console.WriteLine("T.get_color() = {0}", T.get_color()  );


         Console.WriteLine("----------------");

         Matrix4D M = Matrix4D.scale(2, 3, 4);
         Console.WriteLine("Triangle T = {0}", T);
         Console.WriteLine("M = Matrix4D.scale(2, 3, 4) = {0}", M);

         Console.WriteLine("M*T = {0}", M*T);
      }
      #endregion


   }
}
