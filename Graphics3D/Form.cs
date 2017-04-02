using MGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Graphics3D
{
   public partial class Form : System.Windows.Forms.Form
   {
      //Globalne promenljive za crtanje
      private Bitmap bitmap;   //graficki bafer koji se prosledjuje PictureBox-u
      private Brush  brush;    //brush - za bojenje
      private Pen    pen;      //pen   - za crtanje

      private bool invalidate_bitmap;   //da li treba menjati dimenzije grafickog bafera
      private int draw_wid, draw_hei;   //prostor u PictureBox-u za crtanje


      List<Shape>    scene;       //skup svih shape-ova cini scenu
      List<Triangle> triangles;   //skup trouglova u celoj sceni
      Vector3D origin;            //vektor centra ekrana

      private double yaw;   //yaw ugao vektora V
      private double by;    //minimalni ugao pomeraja


      public Form()
      {
         //inicijalizuje Form-u
         InitializeComponent();
         invalidate_bitmap = true;


         bitmap = null;
         brush  = null;
         pen    = null;


         scene = new List<Shape>();


         //mozda ce se menjati u buducnosti
         yaw = 0;
         by  = 0.1;


         //citace se verovatno iz fajla   //------------------------------!!!!!!!!!!!!!!!!!!!!!!!
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

         Shape s = new Shape();
         s.add(t1);
         s.add(t2);


         scene.Add(s);
         triangles = new List<Triangle>();

      }


      private void Form_Load(object sender, EventArgs e)
      {
         //postavlja stil Form-e da se pri resize ponovo iscrtava
         this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
      }

      private void Form_Resize(object sender, EventArgs e)
      {
         invalidate_bitmap = true;
      }

      private void Form_FormClosing(object sender, FormClosingEventArgs e)
      {
         bitmap?.Dispose();   //operator ?. (Elvis operator)
         brush?.Dispose();    // - oslobadja memoriju ako nije null
         pen?.Dispose();

         scene.Clear();
         triangles.Clear();
      }



      private void PictureBox_Paint(object sender, PaintEventArgs e)
      {
         //inicijalizacija
         if( invalidate_bitmap == true )
         {
            bitmap?.Dispose();

            draw_wid = e.ClipRectangle.Width;
            draw_hei = e.ClipRectangle.Height;

            bitmap = new Bitmap(draw_wid, draw_hei);
            origin = new Vector3D(draw_wid/2, draw_hei/2, 0);
         }

         Graphics g = Graphics.FromImage(bitmap);




         //---------------------------------------!!!!!!!!!!!!!! ovde ce biti vece izmene

         //transformacije
         for( int i = 0; i < scene.Count; i++ )
         {
            scene[i].get_triangles();
//            v1 = (Vector3D) (Matrix4D.yaw(yaw) * (Vector4D) v1);
         }

//         pen = new Pen(Color.Red, 3);
//         g.DrawLine(pen,   (float) origin.getx(),   (float) origin.gety(),   (float) (v1 + origin).getx(),   (float) (v1 + origin).gety());
//         g.DrawLine(pen,   (float) origin.getx(),   (float) origin.gety(),   (float) (v1 + origin).getx(),   (float) (v1 + origin).gety());
//         g.DrawLine(pen,   (float) origin.getx(),   (float) origin.gety(),   (float) (v1 + origin).getx(),   (float) (v1 + origin).gety());



         PictureBox.Image = bitmap;
         g?.Dispose();   //oslobadja memoriju ako nije null

         brush?.Dispose();
         pen?.Dispose();

      }


      private void timer_draw_Tick(object sender, EventArgs e)
      {
         Invalidate();
      }

      private void Form_KeyDown(object sender, KeyEventArgs e)
      {
         switch( e.KeyCode )
         {
            case Keys.Left:
               yaw += by;
               break;
            case Keys.Right:
               yaw -= by;
               break;
         }
      }


   }
}
