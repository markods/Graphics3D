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

      private double yaw;       //yaw ugao vektora V
      private double yaw_inc;   //minimalni ugao pomeraja


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
         yaw      = 0;
         yaw_inc  = 2*Math.PI/100;


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

         Vector3D v31 = new Vector3D(20, 50, 0);
         Vector3D v32 = new Vector3D(0, 50, 0);
         Vector3D v33 = new Vector3D(20, 50, 70);
         Color c3 = Color.Violet;
         Triangle t3 = new Triangle(v31, v32, v33, c3);

         Shape s = new Shape();
         s.add(t1);
         s.add(t2);
         s.add(t3);


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
         //inicijalizacija grafickog bafera (koji se koristi za double-buffering)
         if( invalidate_bitmap == true )
         {
            bitmap?.Dispose();

            draw_wid = e.ClipRectangle.Width;
            draw_hei = e.ClipRectangle.Height;

            bitmap = new Bitmap(draw_wid, draw_hei);
         }

         Graphics g = Graphics.FromImage(bitmap);
         g.TranslateTransform( draw_wid/2, draw_hei/2 );  //postavlja koordinatni pocetak PictureBox-a u njegov centar
         g.ScaleTransform( 1, -1 );                       //menja smer y ose tako da y koordinate rastu navise (po defaultu rastu nanize)




         //---------------------------------------!!!!!!!!!!!!!! ovde ce biti vece izmene

         // iscrtavanje koordinatnog sistema viewporta
         Pen AxisPen    = new Pen(Color.Gray,      1);
         Pen Grid10Pen  = new Pen(Color.White,     1);
         Pen Grid100Pen = new Pen(Color.LightGray, 1);
         g.DrawLine(AxisPen, -draw_wid/2, 0, draw_wid/2, 0); // x osa
         int m10  = 1;
         int m100 = 3;
         for( int i = -(draw_wid/2/10)*10; i < draw_wid/2; i += 10 )
         {
            if( i == 0 ) continue;
            g.DrawLine( (i%100 == 0 ? Grid100Pen : Grid10Pen),  i, -draw_hei/2,                  i,  draw_hei/2 );
            g.DrawLine(                            AxisPen,     i, (i%100 == 0 ? -m100 : -m10),  i, (i%100 == 0 ? +m100 : m10) ); // podeoci na x osi
         }
         g.DrawLine(AxisPen, 0, -draw_hei/2, 0, draw_hei/2); // y osa
         for( int i = -(draw_hei/2/10)*10; i < draw_hei/2; i += 10 )
         {
            if( i == 0 ) continue;
            g.DrawLine( (i%100 == 0 ? Grid100Pen : Grid10Pen), -draw_wid/2,                  i, draw_wid/2,                   i );
            g.DrawLine(                            AxisPen,    (i%100 == 0 ? -m100 : -m10),  i, (i%100 == 0 ? +m100 : +m10),  i ); // podeoci na y osi
         }



         
         //test trougao sa temenima u1, u2 i u3
         Vector4D a1 = new Vector4D(    0,  100, 200 );
         Vector4D b1 = new Vector4D(  200,    0, 200 );
         Vector4D c1 = new Vector4D( -100, -100, 200 );
      // Triangle t1 = new Triangle(    0,  100, 200,      200, 0, 200,   -100, -100, 200 );

      /*
         //vektori osa koordinatnog sistema trougla
         Vector4D x1 = new Vector4D( 100,   0,   0 );
         Vector4D y1 = new Vector4D(   0, 100,   0 );
         Vector4D z1 = new Vector4D(   0,   0, 100 );
      // Triangle x1 = new Triangle( 100,   0,   0,        0, 100, 0,        0, 0, 100 );
         Vector4D n1 = new Vector4D(   0,   0,   0 );
      */


         //transformaciona matrica za rotaciju test trougla oko z ose (counter-clockwise)
         Matrix4D R1 = Matrix4D.rotateZ( yaw );  

         //transformacija test trougla rotacijom oko z ose
         Vector4D a2 = R1 * a1;
         Vector4D b2 = R1 * b1;
         Vector4D c2 = R1 * c1;
      // Triangle t2 = R1 * t1;...

      /*
         // transformacija vektora osa koordinatnog sistema test trougla
         Vector4D x2 = R1 * x1;
         Vector4D y2 = R1 * y1;
         Vector4D z2 = R1 * z1;
      // Triangle x2 = R1 * x1;...
         Vector4D n2 = R1 * n1;
      */





         //perspective projection matrix
         Matrix4D P = Matrix4D.projectZ(200);  // 200 znaci da se viewport nalazi na udaljenosti 200 od posmatraca

         //projekcija originalnog test trougla na viewport
         Vector4D pa1 = P * a1;
         Vector4D pb1 = P * b1;
         Vector4D pc1 = P * c1;
      // Triangle pt1 = P * t1; ...

         //projekcija transformisanog test trougla na viewport
         Vector4D pa2 = P * a2;
         Vector4D pb2 = P * b2;
         Vector4D pc2 = P * c2;
      // Triangle pt2 = P * t2; ...

      /*
         //projekcija transformisanog vektora osa koordinatnog sistema test trougla na viewport
         Vector4D px2 = P * x2;
         Vector4D py2 = P * y2;
         Vector4D pz2 = P * z2;
      // Triangle px2 = P * x2; ...
         Vector4D pn2 = P * n2;
      */

         //iscrtavanje originalnog test trougla nakon njegove projekcije na viewport - za crtanje se koriste samo x i y koordinate, ali normalizovane
         pen = new Pen(Color.Blue, 1);
         g.DrawLine( pen, (float) pa1.getnormx(), (float) pa1.getnormy(), (float) pb1.getnormx(), (float) pb1.getnormy() );
         g.DrawLine( pen, (float) pb1.getnormx(), (float) pb1.getnormy(), (float) pc1.getnormx(), (float) pc1.getnormy() );
         g.DrawLine( pen, (float) pc1.getnormx(), (float) pc1.getnormy(), (float) pa1.getnormx(), (float) pa1.getnormy() );
      // pt1.draw(g)...

         //iscrtavanje transformisanog test trougla nakon njegove projekcije na viewport - za crtanje se koriste samo x i y koordinate, ali normalizovane
         pen = new Pen(Color.Red, 1);
         g.DrawLine( pen, (float) pa2.getnormx(), (float) pa2.getnormy(), (float) pb2.getnormx(), (float) pb2.getnormy() );
         g.DrawLine( pen, (float) pb2.getnormx(), (float) pb2.getnormy(), (float) pc2.getnormx(), (float) pc2.getnormy() );
         g.DrawLine( pen, (float) pc2.getnormx(), (float) pc2.getnormy(), (float) pa2.getnormx(), (float) pa2.getnormy() );
      // pt2.draw(g)...
      
      /*
         //iscrtavanje transformisanog vektora osa koordinatnog sistema test trougla nakon njegove projekcije na viewport - za crtanje se koriste samo x i y koordinate, ali normalizovane
         pen = new Pen(Color.Orange, 1);
         g.DrawLine( pen, (float) pn2.getnormx(), (float) pn2.getnormy(), (float) px2.getnormx(), (float) px2.getnormy() );
         pen = new Pen(Color.Cyan, 1);
         g.DrawLine( pen, (float) pn2.getnormx(), (float) pn2.getnormy(), (float) py2.getnormx(), (float) py2.getnormy() );
         pen = new Pen(Color.Magenta, 1);
         g.DrawLine( pen, (float) pn2.getnormx(), (float) pn2.getnormy(), (float) pz2.getnormx(), (float) pz2.getnormy() );
      */



         // prikaz double-buffering bitmape
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
               yaw += yaw_inc;
               break;
            case Keys.Right:
               yaw -= yaw_inc;
               break;
         }
      }


   }
}
