using MGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Graphics3D
{
   public partial class Form : System.Windows.Forms.Form
   {
      //Globalne promenljive za crtanje
      private Bitmap bitmap;            //graficki bafer koji se prosledjuje PictureBox-u
      private bool invalidate_bitmap;   //da li treba menjati dimenzije grafickog bafera
      private int draw_wid, draw_hei;   //prostor u PictureBox-u za crtanje

      List<Shape> scene;       //skup svih shape-ova na sceni


      private Pen AxisPen;
      private Pen Grid10Pen;
      private Pen Grid100Pen;



      //------------------------------------------!!!!!!!!!! testiranje
      Shape quboid;
      Shape circle;

      //potrepstine za osnovne rotacije
      private double radx;       //counter-clockwise ugao u radijanima oko Ox-ose
      private double rady;       //counter-clockwise ugao u radijanima oko Oy-ose
      private double radz;       //counter-clockwise ugao u radijanima oko Oz-ose

      private double radx_inc;   //increment ugla radx
      private double rady_inc;   //increment ugla rady
      private double radz_inc;   //increment ugla radz



      public Form()
      {
         //inicijalizuje Form-u
         InitializeComponent();

         bitmap            = null;
         invalidate_bitmap = true;

         scene = new List<Shape>();

         
         AxisPen    = new Pen(Color.Gray,      1);
         Grid10Pen  = new Pen(Color.White,     1);
         Grid100Pen = new Pen(Color.LightGray, 1);


         //citace se verovatno iz fajla   //------------------------------!!!!!!!!!!!!!!!!!!!!!!!
         quboid = Shape.quboid( 100, 100, 100, Color.Green,  5);
         circle = Shape.circle( 50,            Color.Red,   16);


         radx = 0;
         rady = 0;
         radz = 0;
         
         radx_inc  = 2*Math.PI/100;
         rady_inc  = 2*Math.PI/100;
         radz_inc  = 2*Math.PI/100;

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
         scene.Clear();
      }



      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      private void DrawTriangle( Graphics g, Triangle T )
      {
         Point[]  points = new Point[Triangle.ver_cnt];
         Vector3D vertex;

         for( int i = 0; i < Triangle.ver_cnt; i++ )
         {
            vertex    = T.getv(i);
            points[i] = new Point( (int) vertex.getx(), (int) vertex.gety() );
         }

      // g.FillPolygon( new SolidBrush( T.get_color()    ), points );
         g.DrawPolygon( new Pen       ( T.get_color(), 1 ), points );
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      private void DrawShape( Graphics g, Shape S, Matrix4D M = null )
      {
         if( M == null )
            M = Matrix4D.I;
         
         Triangle T;
         Matrix4D transf = M * S.get_transf();


         for( int i = 0; i < S.get_triangle_num(); i++ )
         {
            T = transf * S.get_triangles()[i];
            DrawTriangle( g, T );
         }
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

         //Iscrtavanje koordinatnog sistema viewporta
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

/*
         //Koordinatni sistem sveta
         Vector4D x1 = 100 * Vector4D.i;
         Vector4D y1 = 100 * Vector4D.j;
         Vector4D z1 = 100 * Vector4D.k;
*/



         //----------------------------------------------!!!!!!!!!!!!!!!!!!! testiranje

         //perspective projection matrix
         Matrix4D Pers = Matrix4D.projectZ(200);  // 200 znaci da se viewport nalazi na udaljenosti 200 od posmatraca

         Matrix4D quboid_transf = Pers * Matrix4D.transl( 0,   0, -200 ) * Matrix4D.rotate( radx, rady, 0 );
         Matrix4D circle_transf = Pers * Matrix4D.transl( 0,   0, -200 ) * Matrix4D.rotateZ( Cmath.PI / 2 - radz )
                                       * Matrix4D.transl( 150, 0,    0 ) * Matrix4D.rotateX( Cmath.PI / 2 - radx );

         DrawShape( g, quboid, quboid_transf );
         DrawShape( g, circle, circle_transf );



         //prikaz double-buffering bitmape
         PictureBox.Image = bitmap;
         g?.Dispose();   //oslobadja memoriju ako nije null

      }


      private void timer_draw_Tick(object sender, EventArgs e)
      {
         Invalidate();
      }

      private void Form_KeyDown(object sender, KeyEventArgs e)
      {
         //----------------------------------------------------!!!!!!!!!!!!!!!!!! menjace se
         switch( e.KeyCode )
         {
            case Keys.Up:
               break;
            case Keys.Down:
               break;
            case Keys.Left:
               radx += radx_inc;
               rady += rady_inc;
               radz += radz_inc;
               break;
            case Keys.Right:
               radx -= radx_inc;
               rady -= rady_inc;
               radz -= radz_inc;
               break;
         }
      }


   }
}
