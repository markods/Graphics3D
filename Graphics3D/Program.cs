using System;
using System.Windows.Forms;
using MGL;

namespace Graphics3D
{
   static class Program
   {
      /// <summary>
      /// The main entry point for the application.
      /// </summary>
      [STAThread]
      static void Main()
      {
         
         Application.EnableVisualStyles();
         Application.SetCompatibleTextRenderingDefault(false);
         Application.Run(new Form());


         //testing_harness();
      }


      #region Testing
      public static void testing_harness()
      {
         Console.WriteLine("========================================");

         //Cmath.test1();
         //Cmath.test2();
         //Cmath.test3();
         //Vector3D.test1();
         //Vector4D.test1();
         //Matrix4D.test1();
         //Triangle.test1();
         //Mesh.test1();

         Console.WriteLine("========================================");
      }
      #endregion


   }
}
