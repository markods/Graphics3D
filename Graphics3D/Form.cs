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
      List<Mesh> scene;                 //skup svih shape-ova na sceni



      //===================Testing=====================
      Mesh shape2D;
      Mesh shape3D;
      private int detail;        //level of detail (useful for spheres, etc.)


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

         scene  = new List<Mesh>();
         detail = 0;
         UpdateModels();


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
      private void DrawShape( Graphics g, Mesh S, Matrix4D M = null )
      {
         if( g == null )   return;
         if( S == null )   return;
         if( M == null )   M = Matrix4D.I;


         Point[]  points = new Point[Triangle.ver_cnt];
         Vector3D vertex;
         Triangle T;



         for( int i = 0; i < S.triangle_cnt(); i++ )
         {
            T = M * S.get_triangles()[i];   //applies shape transformations to each triangle of the specific shape

            for( int k = 0; k < Triangle.ver_cnt; k++ )
            {
               vertex    = T.getv(k);
               points[k] = new Point( (int) vertex.getx(), (int) vertex.gety() );
            }


         // g.FillPolygon( new SolidBrush( T.get_color()    ), points );
            g.DrawPolygon( new Pen       ( T.get_color(), 1 ), points );
         }

      }


      private void UpdateModels()
      {
         //===================Testing=====================
      // shape2D = Mesh.circle   ( 50,            Color.Orange, 3 * detail );
         shape2D = Mesh.rectangle( 100, 100,      Color.Orange, detail     );   //-------------------popraviti


      // shape3D = Mesh.quboid   ( 100, 100, 100, Color.Green,  detail     );   //-------------------popraviti

      // shape3D = Mesh.psphere  ( 50,            Color.Green,  detail     );   //-------------------popraviti
         shape3D = Mesh.uvsphere ( 50,            Color.Green,  detail     );
      // shape3D = Mesh.icosphere( 50,            Color.Green,  detail     );
      
      // shape3D = Mesh.cyllinder( 50,  100,      Color.Green,  detail     );   //-------------------popraviti
      // shape3D = Mesh.cone     ( 50,  100,      Color.Green,  detail     );
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



         //Iscrtavanje koordinatnog sistema viewporta
         Pen AxisPen    = new Pen(Color.Gray,      1);
         Pen Grid10Pen  = new Pen(Color.White,     1);
         Pen Grid100Pen = new Pen(Color.LightGray, 1);


         g.DrawLine(AxisPen, -draw_wid/2, 0, draw_wid/2, 0);   //X-osa
         int m10  = 1;
         int m100 = 3;
         for( int i = -(draw_wid/2/10)*10; i < draw_wid/2; i += 10 )
         {
            if( i == 0 ) continue;
            g.DrawLine( (i%100 == 0 ? Grid100Pen : Grid10Pen),  i, -draw_hei/2,                  i,  draw_hei/2 );
            g.DrawLine(                            AxisPen,     i, (i%100 == 0 ? -m100 : -m10),  i, (i%100 == 0 ? +m100 : m10) );   //podeoci na X-osi
         }
         g.DrawLine(AxisPen, 0, -draw_hei/2, 0, draw_hei/2);   //Y-osa
         for( int i = -(draw_hei/2/10)*10; i < draw_hei/2; i += 10 )
         {
            if( i == 0 ) continue;
            g.DrawLine( (i%100 == 0 ? Grid100Pen : Grid10Pen), -draw_wid/2,                  i, draw_wid/2,                   i );
            g.DrawLine(                            AxisPen,    (i%100 == 0 ? -m100 : -m10),  i, (i%100 == 0 ? +m100 : +m10),  i );   //podeoci na Y-osi
         }




         //===================Testing=====================

         //perspective projection matrix
         Matrix4D pers = Matrix4D.projXY(200);  // 200 znaci da se viewport nalazi na udaljenosti 200 od posmatraca


         Matrix4D transf2D = pers * Matrix4D.transl(   0,   0, -200 ) * Matrix4D.rotateZ( Cmath.PI / 2 - radz )
                                  * Matrix4D.transl( 150,   0,    0 ) * Matrix4D.rotateX( Cmath.PI / 2 - radx );

         Matrix4D transf3D = pers * Matrix4D.transl(   0,   0, -200 ) * Matrix4D.rotate ( radx, 0, radz );


         DrawShape( g, shape2D, transf2D );
         DrawShape( g, shape3D, transf3D );


         //viewing frustrum culling matrix
         //
         //      X           Y              Z             W
         //[  2n/(r-l),      0,        (r+l)/(r-l),       0       ]
         //[     0,       2n/(t-b),    (t+b)/(t-b),       0       ]
         //[     0,          0,       -(f+n)/(f-n),   -2fn/(f-n)  ]
         //[     0,          0,            -1,            0       ]

         //===============================================




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
         switch( e.KeyCode )
         {
            case Keys.Add:
               if(detail < 10)
               {
                  detail++;
                  UpdateModels();
               }
               break;
            case Keys.Subtract:
               if(detail > 0)
               {
                  detail--;
                  UpdateModels();
               }
               break;
            
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
