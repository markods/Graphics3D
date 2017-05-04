using MGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging; //zbog BitmapData

/*
public class GDI
{
    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    internal static extern bool SetPixel(IntPtr hdc, int X, int Y, uint crColor);
}
*/



public struct ColorARGB
{
    public byte B, G, R, A;

    public ColorARGB(Color color)
    {
        A = color.A;  R = color.R;  G = color.G;  B = color.B;
    }

    public ColorARGB(byte a, byte r, byte g, byte b)
    {
        A = a;  R = r;  G = g;  B = b;
    }

    public void SetColor(Color color)
    {
        A = color.A;  R = color.R;  G = color.G;  B = color.B;
    }
    public void SetColor(byte a, byte r, byte g, byte b)
    {
        A = a;  R = r;  G = g;  B = b;
    }

    public Color GetColor()
    {
        return Color.FromArgb(A, R, G, B);
    }
    public void GetColor(ref byte a, ref byte r, ref byte g, ref byte b)
    {
        a = A;  r = R;  g = G;  b = B;
    }
}





namespace Graphics3D
{
   public partial class Form : System.Windows.Forms.Form
   {
      private static int refresh_cnt = 0;

      //Globalne promenljive za crtanje
      private Bitmap bitmap;            //graficki bafer koji se prosledjuje PictureBox-u
      private bool crate_new_bitmap;    //da li treba menjati dimenzije grafickog bafera
      private double[][] zbuf;          //Z-buffer

      private int draw_wid, draw_hei;   //prostor u PictureBox-u za crtanje
      List<Mesh> scene;                 //skup svih shape-ova na sceni

      double Zvp;    //Z koordinata VP (viewporta) u koordinatnom sistemu posmatraca
      double Zncp;   //Z koordinata NCP (near clipping plane) u koordinatnom sistemu posmatraca
      double Zfcp;   //Z koordinata FCP (far clipping plane) u koordinatnom sistemu posmatraca
      double Xrcp;   //X koordinata RCP (right clipping plane) u koordinatnom sistemu posmatraca
      double Xlcp;   //X koordinata LCP (left clipping plane) u koordinatnom sistemu posmatraca
      double Ytcp;   //Y koordinata TCP (top clipping plane) u koordinatnom sistemu posmatraca
      double Ybcp;   //Y koordinata BCP (bottom clipping plane) u koordinatnom sistemu posmatraca
      Matrix4D pp;   //perspective projection matrix
      Matrix4D vt;   //viewport transformation matrix

      SolidBrush brush;
      Pen        pen;
      Point[] trianglepoints;

      bool draw_clipped_xy;
      bool draw_wireframe;
      bool draw_surfaces;
      bool draw_normals;

      const int front_side =  1;
      const int both_sides =  0;
      const int back_side  = -1;

      int depth_algorithm;
      const int depth_unordered     = 0;
      const int depth_back2front    = 1;
      const int depth_zbuf          = 2;
      const int depth_algorithm_cnt = 3;


      //===================Testing=====================
      Mesh mesh1;
      Mesh mesh2;
      Mesh mesh3;
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

         bitmap           = null;
         crate_new_bitmap = true;

         scene  = new List<Mesh>();
         detail = 1;
         UpdateModels();

         radx = 0;
         rady = 0;
         radz = 0;
         
         radx_inc  = 2*Math.PI/100;
         rady_inc  = 2*Math.PI/100;
         radz_inc  = 2*Math.PI/100;


         //===================Testing=====================
         Zvp  = -200;                      //-200 znaci da se viewport nalazi na udaljenosti 200 od posmatraca

         Zncp = -5;                        //ako je Zncp = Zvp to znaci da se clipping radi na viewportu, a vrednosti Zncp treba da budu negativne
         Zfcp = -1000;                     //ako je Zncp = Zvp to znaci da se clipping radi na viewportu, a vrednosti Zncp treba da budu negativne

         double phi = (1+Math.Sqrt(5))/2;
         Xrcp =  200 * phi;
         Xlcp = -200 * phi;
         Ytcp =  200;
         Ybcp = -200;
         
         pp = Matrix4D.projXY(Zvp);        //perspective projection matrix
         vt = Matrix4D.transl(0, 0, Zvp);  //viewport transformation matrix

         draw_clipped_xy = true;
         draw_wireframe = true;
         draw_surfaces = true;
         draw_normals = false;

         depth_algorithm = depth_back2front;

         brush = new SolidBrush(Color.Empty);
         pen   = new Pen(Color.Empty, 1);

         trianglepoints = new Point[Triangle.ver_cnt];
         for( int i = 0; i < Triangle.ver_cnt; i++ )
            trianglepoints[i] = new Point();
      }


      private void Form_Load(object sender, EventArgs e)
      {
         //postavlja stil Form-e da se pri resize ponovo iscrtava
         this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
      }

      private void Form_Resize(object sender, EventArgs e)
      {
         crate_new_bitmap = true;
         Refresh();
      }

      private void Form_FormClosing(object sender, FormClosingEventArgs e)
      {
         bitmap?.Dispose();   //operator ?. (Elvis operator)
         scene.Clear();
      }


      Color Brighten(Color c1, double factor)
      {
          double r = ((c1.R * factor) > 255) ? 255 : (c1.R * factor);
          double g = ((c1.G * factor) > 255) ? 255 : (c1.G * factor);
          double b = ((c1.B * factor) > 255) ? 255 : (c1.B * factor);

          Color c = Color.FromArgb( c1.A, (int) r, (int) g, (int) b );
          return c;
      }



      private Vector4D Intersect( Vector4D v1, Vector4D v2, Vector4D N, Vector4D L )  
      {
         //returns intersection point of line (between v1 and V2) through plane (N = normal to plane, L = point which belongs to plane)
         return v1 + ((L - v1)*N) / ((v2 - v1)*N) * (v2 - v1);
      }



      private void RasterizeTriangle(Graphics g, Triangle Tpp, Triangle T, Color c)
      {
         //indeks gornjeg temena U trougla (sa najvecim Y)
         int idxu = 0;
         for( int i = 1; i < 3; i++ )
            if( Tpp.getv(i).gety() > Tpp.getv(idxu).gety() )
               idxu = i;

         //indeks donjeg temena D trougla (sa najmanjim Y)
         int idxd = (idxu == 0) ? 1 : 0;
         for( int i = 1; i < 3; i++ )
         {
            if( i == idxu ) continue; //najnize teme trougla ne sme istovremeno biti i najvise
            if( Tpp.getv(i).gety() <= Tpp.getv(idxd).gety() )
               idxd = i;
         } 
         
         //indeks srednjeg temena M trougla (sa Y koje nije ni najvece ni najmanje)
         int idxm = 0;
         for( int i = 0; i < 3; i++ )
         {
            if( i == idxu ) continue;
            if( i == idxd ) continue;
            idxm = i;
            break;
         }     

         int Ux = (int) Tpp.getv(idxu).getx();
         int Uy = (int) Tpp.getv(idxu).gety();

         int My = (int) Tpp.getv(idxm).gety();

         int Dx = (int) Tpp.getv(idxd).getx();
         int Dy = (int) Tpp.getv(idxd).gety();
        
         bool exist_up = ( Uy > My ); //da li postoji gornji deo trougla
         bool exist_dn = ( Dy < My ); //da li postoji donji deo trougla

         //odredjivanje granicne duzi LR do koje treba rasterizovati gornji i donji deo trougla
         Vector4D l = Tpp.getv(idxm);
         Vector4D r = null;   //tacka na duzi UD sa suprotne strane tacke M
              if(  exist_up && !exist_dn )  r = Tpp.getv(idxd);
         else if( !exist_up &&  exist_dn )  r = Tpp.getv(idxu);
         else if(  exist_up &&  exist_dn )  r = Intersect( Tpp.getv(idxu), Tpp.getv(idxd), Vector4D.j, Tpp.getv(idxm) );
         else //if( !exist_up && !exist_dn )
            r = Tpp.getv(idxm);  //umesto ovog staviti da se u ovom slucaju l i r izracunaju kao najvise udaljena temena trougla Tpp!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!


         //rasterizacija horizontalnih linija trougla
         unsafe   //unsafe se navodi zbog upotrebe pointera (a treba ukljuciti i /unsafe u kompajleru)
         {
            BitmapData bitmapData     = bitmap.LockBits( new Rectangle(0, 0, draw_wid, draw_hei), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb );
            ColorARGB* start_position = (ColorARGB*) bitmapData.Scan0;
            ColorARGB* position       = start_position;
            int Px, Py; // koordinate piksela u bitmapi

            int Lx = (int) l.getx();
            int Ly = (int) l.gety();

            int Rx = (int) r.getx();
            int Ry = (int) r.gety();

            int begx = 0;
            int endx = 0;
            int inc = (Lx <= Rx ? 1 : -1);  //kad se rasterizacija horizontalne linije vrsi u pravcu od L ka R treba koristiti sledeci inkrement (jer se ne garantuje da je L levo a R desno)

          //Color color = Color.Red;
            Vector4D Z; //z dubina tacke koja se iscrtava
            Vector4D N = Vector4D.vect_mult( T.getv(1) - T.getv(0), T.getv(2) - T.getv(0) ).unitize(); //normala na trougao koji se iscrtava

            for( int y = Dy; y <= Uy; y++ )
            {
               if( exist_up && y > My && Ly != Uy && Ry != Uy ) //gornji deo trougla
               {
                //begx = Ux + (Lx - Ux)*(y - Uy)/(Ly - Uy);
                //endx = Ux + (Rx - Ux)*(y - Uy)/(Ry - Uy);
                  begx = (int) Math.Round( Ux + (Lx - Ux)*(y - Uy) / (double) (Ly - Uy), 0);
                  endx = (int) Math.Round( Ux + (Rx - Ux)*(y - Uy) / (double) (Ry - Uy), 0);
                //color = Color.Black;
               }
               else if( exist_dn && y < My && Ly != Dy && Ry != Dy ) //donji deo trougla
               {
                //begx = Dx + (Lx - Dx)*(y - Dy)/(Ly - Dy);
                //endx = Dx + (Rx - Dx)*(y - Dy)/(Ry - Dy);
                  begx = (int) Math.Round( Dx + (Lx - Dx)*(y - Dy) / (double) (Ly - Dy), 0);
                  endx = (int) Math.Round( Dx + (Rx - Dx)*(y - Dy) / (double) (Ry - Dy), 0);
                //color = Color.Blue;
               }
               else // linija razdvanjanja trougla na gornji i donji deo
               {
                  begx = Lx;
                  endx = Rx;
                //color = Color.Cyan;
               }

               //iscrtavanje horizontalne linije u zadatim granicama
               for( int x = (int) begx;   (x - endx) * inc <= 0;   x += inc )
               {
                  Px =  draw_wid/2 + x;
                  Py =  draw_hei/2 - y;
                  if(     0 <= Px  &&  Px < draw_wid 
                       && 0 <= Py  &&  Py < draw_hei ) 
                  {
                     // tacno izracunavanje Z dubine tacke
                     Z = Intersect( Vector4D.zero, new Vector4D( x, y, Zvp ), N, T.center() );
                     if( Z.getz() > zbuf[Py][Px] )
                     {
                        zbuf[Py][Px] = Z.getz();

                      //SetPixel je spora funkcija, umesto nje koristi se pointerski pristup bitmapi
                      //bitmap.SetPixel( draw_wid/2 + x, draw_hei/2 - y, color );
                        position = start_position + Px + Py * draw_wid;
                        position->SetColor( c );
                     }
                  }
               }
            }


            bitmap.UnlockBits(bitmapData);
         }
      }



      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      private void ClipLine(ref Vector3D v0, ref Vector3D v1)
      {
         double clip_wid = 2;
         double clip_hei = 2;
         double clip_dep = 1;


         const int reg_n = 2 << 5;   //near   regional code
         const int reg_f = 2 << 4;   //far        -||-
         const int reg_t = 2 << 3;   //top        -||-
         const int reg_b = 2 << 2;   //bottom     -||-
         const int reg_l = 2 << 1;   //left       -||-
         const int reg_r = 2 << 0;   //right      -||-



         Vector3D[] v = new Vector3D[2];
         int[] v_reg = new int[2];   //vectors' regional code

         v[0] = v0;
         v[1] = v1;

         
         for( int i = 0; i < 2; i++ )   //setting regional codes
         {
            v_reg[i] = 0;

            v_reg[i] &= (v[i].getz() > clip_dep  ) ? reg_n : (v[i].getz() < 0         ) ? reg_f : ~0;
            v_reg[i] &= (v[i].gety() > clip_hei/2) ? reg_t : (v[i].getx() < clip_hei/2) ? reg_b : ~0;
            v_reg[i] &= (v[i].getx() > clip_wid/2) ? reg_r : (v[i].getx() < clip_wid/2) ? reg_l : ~0;
         }

         
         if( (v_reg[0] & v_reg[1]) == 0 ) return;   //apparently & doesn't take prescedence over ==
         

         for( int i = 0; i < 2; i++ )
         {
            if     ( (v_reg[i] & reg_n) != 0 )
            {
               
            }
            else if( (v_reg[i] & reg_f) != 0 ) { }
            else if( (v_reg[i] & reg_l) != 0 ) { }
            else if( (v_reg[i] & reg_r) != 0 ) { }
            else if( (v_reg[i] & reg_t) != 0 ) { }
            else if( (v_reg[i] & reg_b) != 0 ) { }
         }

      }



      private bool ClipTriangle(Graphics g, ref Triangle T, Vector4D N, Vector4D L, int DrawSide)
      {  
         //CP je clipping plane
         //N je normala na CP usmerena ka njegovoj spoljasnosti (tj. delu koji se odbacuje)
         //L je tacka koja pripada CP
         //trougao T dat u koordinatnom sistemu posmatraca, ali na njega nije primenjena projekcija perspektive

         //ispitivanje da li treba raditi clipping
         // s0, s1 i s2 su pozitivni ako je tacka sa spoljne strane CP
         int s0 = Math.Sign(Math.Round( N * (T.getv(0) - L), Cmath.hip )); 
         int s1 = Math.Sign(Math.Round( N * (T.getv(1) - L), Cmath.hip )); 
         int s2 = Math.Sign(Math.Round( N * (T.getv(2) - L), Cmath.hip ));
         int s  = s0+s1+s2;  
         int a  = Math.Abs(s0) + Math.Abs(s1) + Math.Abs(s2); 
   
         if(     s == 3               //ako su sva tri temena trougla spolja
             ||  s == 2               //ako su dva temena trougla spolja, a trece teme lezi u CP
             || (s == 1 && a == 1 ))  //ako je jedno teme spolja, a druga dva leze u CP
         {
            return false;  //ne treba crtati trougao koji je ispred CP u odnosu na posmatraca
         }
         else if(     s == -3               //ako su sva tri temena trougla unutra
                  ||  s == -2               //ako su dva temena trougla unutra, a trece teme lezi u CP
                  || (s == -1 && a == 1 )   //ako je jedno teme unutra, a druga dva leze u CP
                  || (s ==  0 && a == 0 ))  //ako su sva tri temena trougla u CP
         {
            //preskociti CP clipping trougla koji je ceo iza CP u odnosu na posmatraca
         }
         else if( s == 0 && a == 2 )
         {
            //smanjiti trougao (odbacivanjem spoljasnjeg dela) koji je presecen CP-om tako da mu je jedno teme unutra, drugo spolja a trece lezi u CP
            int idxcp  = (s1 ==  0 ? 1 : (s2 ==  0 ? 2 : 0));
            int idxo   = (s1 ==  1 ? 1 : (s2 ==  1 ? 2 : 0));
            int idxi   = (s1 == -1 ? 1 : (s2 == -1 ? 2 : 0));

            Vector4D I1 = Intersect( T.getv(idxi), T.getv(idxo), N, L );

            T.setv( idxo, I1 );  //spoljasnje teme ce biti zamenjeno tackom proboja te duzi kroz CP)

          //pen.Color = Color.LightGreen;
         }
         else if( s ==  1 && a == 3 )
         {
            //smanjiti trougao (odbacivanjem spoljasneg dela) koji je presecen CP-om tako da mu je jedno teme unutra a dva spolja
            int idxi  = (s1 == -1 ? 1 : (s2 == -1 ? 2 : 0));
            int idxo1 = (idxi + 1) % 3;
            int idxo2 = (idxi + 2) % 3;

            Vector4D I1 = Intersect( T.getv(idxi), T.getv(idxo1), N, L );
            Vector4D I2 = Intersect( T.getv(idxi), T.getv(idxo2), N, L );

            T.setv( idxo1, I1 );  //prvo spoljasnje teme ce biti zamenjeno tackom proboja te duzi kroz CP)
            T.setv( idxo2, I2 );  //drugo spoljasnje teme ce biti zamenjeno tackom proboja te duzi kroz CP)

         //pen.Color = Color.LightBlue;
         }
         else if( s == -1 && a == 3 )
         {
            //raseci na dva dela trougao koji je presecen CP-om tako da mu je jedno teme spolja a dva unutra
            int idxo  = (s1 == 1 ? 1 : (s2 == 1 ? 2 : 0));
            int idxi1 = (idxo + 1) % 3;
            int idxi2 = (idxo + 2) % 3;

            Vector4D I1 = Intersect( T.getv(idxi2), T.getv(idxo), N, L );
            Vector4D I2 = Intersect( T.getv(idxi1), T.getv(idxo), N, L );

            DrawTriangle( g, new Triangle( T.getv(idxi1), T.getv(idxi2), I1,     T.get_color() ), DrawSide );
            DrawTriangle( g, new Triangle( T.getv(idxi1),                I1, I2, T.get_color() ), DrawSide );
            return false;
         }
         else
            throw new Exception("Unknown error.");
     
         //funkcija vraca true ako se moze nastaviti sa crtanjem trougla
         return true;
      }



      private void DrawTriangle(Graphics g, Triangle T, int DrawSide = 0)
      {
         //trougao T dat u koordinatnom sistemu posmatraca, ali na njega nije primenjena projekcija perspektive

         //ne iscrtava se trougao sa zadnje strane objekta (trougao koji je orijetisan na istu stranu na koju gleda posmatrac)
         if( (T.center() * Vector4D.vect_mult( T.getv(1) - T.getv(0), T.getv(2) - T.getv(0) )) * DrawSide > 0 )
            return;

         Vector4D vertex;
         Triangle Tpp;    //trougao nakon projektovanja na viewport

         pen.Color = T.get_color();
         brush.Color = Color.LightGray;



         //trougao sa zadnje strane objekta
         //trougao koji je orijetisan na istu stranu na koju gleda posmatrac 
         if( T.center() * Vector4D.vect_mult( T.getv(1) - T.getv(0), T.getv(2) - T.getv(0) ) >= 0 )
            pen.Color = Color.Gray;



         // clipping po Z osi
         if( ! ClipTriangle(g, ref T,   Vector4D.k, Zncp * Vector4D.k, DrawSide) ) return;
         if( ! ClipTriangle(g, ref T, - Vector4D.k, Zfcp * Vector4D.k, DrawSide) ) return;

         if( draw_clipped_xy )
         {
            // clipping po levoj i desnoj viewport ravni
            if( ! ClipTriangle(g, ref T,   Vector4D.vect_mult( new Vector4D( Xrcp, Ybcp, Zvp), new Vector4D( Xrcp, Ytcp, Zvp) ), Vector4D.zero, DrawSide) ) return;
            if( ! ClipTriangle(g, ref T,   Vector4D.vect_mult( new Vector4D( Xlcp, Ytcp, Zvp), new Vector4D( Xlcp, Ybcp, Zvp) ), Vector4D.zero, DrawSide) ) return;

            // clipping po gornjoj i donjoj viewport ravni
            if( ! ClipTriangle(g, ref T,   Vector4D.vect_mult( new Vector4D( Xrcp, Ytcp, Zvp), new Vector4D( Xlcp, Ytcp, Zvp) ), Vector4D.zero, DrawSide) ) return;
            if( ! ClipTriangle(g, ref T,   Vector4D.vect_mult( new Vector4D( Xlcp, Ybcp, Zvp), new Vector4D( Xrcp, Ybcp, Zvp) ), Vector4D.zero, DrawSide) ) return;
         }

         //projekcija perspektive se radi nakon Near Plane Clipping-a
         Tpp = pp * T;
         Tpp.norm();


         for( int k = 0; k < Triangle.ver_cnt; k++ )
         {
            vertex = Tpp.getv(k);
            trianglepoints[k].X = (int) vertex.getnormx();
            trianglepoints[k].Y = (int) vertex.getnormy();
         }
         

         if( draw_surfaces )
         {
            Color c = (DrawSide > 0 ? Brighten(T.get_color(), 0.5) /* Color.LightGray */ : Color.DarkGray);

            if( depth_algorithm == depth_zbuf )
               RasterizeTriangle( g, Tpp, T, c );
            else
            {
               if (depth_algorithm == depth_back2front )
                  brush.Color = (DrawSide > 0 ? Color.DarkGray : Color.Indigo);
               else
                  brush.Color = Color.LightGray;

               g.FillPolygon( brush, trianglepoints );
            }
         }

         if( draw_wireframe )
         {
            g.DrawPolygon( pen, trianglepoints );
         }


         if( draw_normals )
         {
            //crtanje normala na trouglove
            Vector4D norm_cent = pp * Tpp.center();
            Vector4D norm_vrh = pp * (Tpp.center() + Vector4D.vect_mult( T.getv(1) - T.getv(0), T.getv(2) - T.getv(0)).unitize() * 25 );  //25 je duzina normale
            g.DrawLine( pen, (float) norm_cent.getnormx(), (float) norm_cent.getnormy(), (float) norm_vrh.getnormx(), (float) norm_vrh.getnormy() );
         }


      }


      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      private void DrawMesh(Graphics g, Mesh S, Matrix4D M = null )
      {
         if( g == null )   return;
         if( S == null )   return;
         if( M == null )   M = Matrix4D.I;

         List<Triangle> triangles = S.get_triangles();
         if(    depth_algorithm == depth_unordered )
         {
            for( int i = 0; i < S.triangle_cnt(); i++ )
               DrawTriangle( g, M * triangles[i],  both_sides );  //applies shape transformations to each triangle of the specific shape
         }
         else if(    depth_algorithm == depth_back2front 
                  || depth_algorithm == depth_zbuf )
         {
            for( int i = 0; i < S.triangle_cnt(); i++ )
               DrawTriangle( g, M * triangles[i], back_side  );  //applies shape transformations to each triangle of the specific shape, and draw only back-side triangles
            for( int i = 0; i < S.triangle_cnt(); i++ )
               DrawTriangle( g, M * triangles[i], front_side );  //applies shape transformations to each triangle of the specific shape, and draw only front-side triangles
         }         
      }


      private void UpdateModels()
      {
         //===================Testing=====================
       //mesh1 = null;
       //mesh1 = Mesh.circle      (  50,           Color.Orange, 3 * detail  );
         mesh1 = Mesh.hollowcircle( 120,  70,      Color.Orange, 3 * detail  );
       //mesh1 = Mesh.rectangle   ( 100, 100,      Color.Orange,     detail  );
       //mesh1 = Mesh.psphere     ( 50,            Color.Orange,     detail  );
       //mesh1 = Mesh.quboid      ( 100, 100, 100, Color.Orange,     detail  );
       //mesh1 = Mesh.uvsphere    ( 100,           Color.Orange,     detail  );
       //mesh1 = Mesh.icosphere   ( 100,           Color.Orange,     detail  );
       //mesh1 = Mesh.cyllinder   ( 50,  100,      Color.Orange,     detail  );
                                  
       //mesh2 = null;            
       //mesh2 = Mesh.quboid      ( 100, 100, 100, Color.Red,        detail  );
       //mesh2 = Mesh.psphere     ( 100,           Color.Red,        detail  );
       //mesh2 = Mesh.uvsphere    ( 100,           Color.Red,        detail  );
       //mesh2 = Mesh.icosphere   ( 100,           Color.Red,        detail  );
       //mesh2 = Mesh.cyllinder   (  50, 100,      Color.Red,        detail  );
         mesh2 = Mesh.cone        (  50, 100,      Color.Red,        detail  );
                                  
       //mesh3 = null;
       //mesh3 = Mesh.icosphere   ( 100,           Color.Magenta,    detail  );
       //mesh3 = Mesh.psphere     ( 100,           Color.Magenta,    detail  );
         mesh3 = Mesh.uvsphere    (  50,           Color.Magenta,    detail  );

         if( mesh1 != null ) Console.WriteLine("Mesh1 triangle count: {0}", mesh1.triangle_cnt());
         if( mesh2 != null ) Console.WriteLine("Mesh2 triangle count: {0}", mesh2.triangle_cnt());
         if( mesh3 != null ) Console.WriteLine("Mesh3 triangle count: {0}", mesh3.triangle_cnt());
      }






      private void PictureBox_Paint(object sender, PaintEventArgs e)
      {
         //inicijalizacija grafickog bafera (koji se koristi za double-buffering)
         if( crate_new_bitmap == true )
         {
            bitmap?.Dispose();
            //h_bitmap?.Dispose();

            draw_wid = (int) ClientRectangle.Width;
            draw_hei = (int) ClientRectangle.Height;

            bitmap   = new Bitmap(draw_wid, draw_hei);
            //h_bitmap = new Bitmap(draw_wid, draw_hei);

            //priprema Z-buffera
            zbuf = new double[draw_hei][];
            for( int i = 0; i < draw_hei; i++ )
               zbuf[i] = new double[draw_wid];
         }

         //praznjenje Z buffera
         for( int y = 0; y < draw_hei; y++ )
            for(int x = 0; x < draw_wid; x++ )
               zbuf[y][x] = -1000000.0;



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





         //===================Testing1=====================

         //"tunel"
         for( int t = -400; t <= -50; t += 50 )
         {
            pen.Color = (t == Zvp ? Color.Black : Color.Silver);
            Vector4D tl = pp * new Vector4D( Xlcp, Ytcp, t );
            Vector4D tr = pp * new Vector4D( Xrcp, Ytcp, t );
            Vector4D br = pp * new Vector4D( Xrcp, Ybcp, t );
            Vector4D bl = pp * new Vector4D( Xlcp, Ybcp, t );    
            g.DrawLine( pen, (float) tl.getnormx(), (float) tl.getnormy(), (float) tr.getnormx(), (float) tr.getnormy() );
            g.DrawLine( pen, (float) tr.getnormx(), (float) tr.getnormy(), (float) br.getnormx(), (float) br.getnormy() );
            g.DrawLine( pen, (float) br.getnormx(), (float) br.getnormy(), (float) bl.getnormx(), (float) bl.getnormy() );
            g.DrawLine( pen, (float) bl.getnormx(), (float) bl.getnormy(), (float) tl.getnormx(), (float) tl.getnormy() );
         }


         {
            //pravougaonik koji lezi u FCP
            pen.Color = Color.Red;
            Vector4D Ftl = pp * new Vector4D( Xlcp, Ytcp, Zfcp );
            Vector4D Ftr = pp * new Vector4D( Xrcp, Ytcp, Zfcp );
            Vector4D Fbr = pp * new Vector4D( Xrcp, Ybcp, Zfcp );
            Vector4D Fbl = pp * new Vector4D( Xlcp, Ybcp, Zfcp );    
            g.DrawLine( pen, (float) Ftl.getnormx(), (float) Ftl.getnormy(), (float) Ftr.getnormx(), (float) Ftr.getnormy() );
            g.DrawLine( pen, (float) Ftr.getnormx(), (float) Ftr.getnormy(), (float) Fbr.getnormx(), (float) Fbr.getnormy() );
            g.DrawLine( pen, (float) Fbr.getnormx(), (float) Fbr.getnormy(), (float) Fbl.getnormx(), (float) Fbl.getnormy() );
            g.DrawLine( pen, (float) Fbl.getnormx(), (float) Fbl.getnormy(), (float) Ftl.getnormx(), (float) Ftl.getnormy() );
         
            //pravougaonik koji lezi u NCP
            Vector4D Ntl = pp * new Vector4D( Xlcp, Ytcp, Zncp );
            Vector4D Ntr = pp * new Vector4D( Xrcp, Ytcp, Zncp );
            Vector4D Nbr = pp * new Vector4D( Xrcp, Ybcp, Zncp );
            Vector4D Nbl = pp * new Vector4D( Xlcp, Ybcp, Zncp );    
            g.DrawLine( pen, (float) Ntl.getnormx(), (float) Ntl.getnormy(), (float) Ntr.getnormx(), (float) Ntr.getnormy() );
            g.DrawLine( pen, (float) Ntr.getnormx(), (float) Ntr.getnormy(), (float) Nbr.getnormx(), (float) Nbr.getnormy() );
            g.DrawLine( pen, (float) Nbr.getnormx(), (float) Nbr.getnormy(), (float) Nbl.getnormx(), (float) Nbl.getnormy() );
            g.DrawLine( pen, (float) Nbl.getnormx(), (float) Nbl.getnormy(), (float) Ntl.getnormx(), (float) Ntl.getnormy() );

            //ivicne linije izmedju FCP i NCP
            g.DrawLine( pen, (float) Ftl.getnormx(), (float) Ftl.getnormy(), (float) Ntl.getnormx(), (float) Ntl.getnormy() );
            g.DrawLine( pen, (float) Ftr.getnormx(), (float) Ftr.getnormy(), (float) Ntr.getnormx(), (float) Ntr.getnormy() );
            g.DrawLine( pen, (float) Fbr.getnormx(), (float) Fbr.getnormy(), (float) Nbr.getnormx(), (float) Nbr.getnormy() );
            g.DrawLine( pen, (float) Fbl.getnormx(), (float) Fbl.getnormy(), (float) Nbl.getnormx(), (float) Nbl.getnormy() );
         }

         /*
         //da li je viewport pravilno orijentisan (pravougaonik i trougao treba da se preklapaju u prvom kvadrantu XY ravni)
         pen.Color = Color.Cyan;
         g.DrawRectangle( pen, 50,100,150,50);
         DrawTriangle( g, new Triangle( new Vector4D(50,100,Zvp), new Vector4D(200,100,Zvp), new Vector4D(50,150,Zvp), Color.DarkCyan ) );
         */

         /*
         //trougao koji lezi u NCP
         DrawTriangle( g, new Triangle( new Vector4D(50,100,Zncp), new Vector4D(200,100,Zncp), new Vector4D(50,150,Zncp), Color.LightGreen ) );
         */

         /*
         //da li matrice rotacije rade ispravno
         Mesh m1 = Mesh.rectangle( 100, 200, Color.Orange, 5 );
       //DrawMesh( g, m1, vt );
         DrawMesh( g, m1, vt * Matrix4D.rotateX( Cmath.PI/6 ) );
       //DrawMesh( g, m1, vt * Matrix4D.rotateZ( Cmath.PI/6 ) );
         */




         //===================Testing2=====================

       //Matrix4D transf1 = vt * Matrix4D.rotateZ( Cmath.PI / 2 - radz ) * Matrix4D.transl( 150, 0, 0 ) * Matrix4D.rotateX( Cmath.PI / 2 - radx );
       //Matrix4D transf1 = vt * Matrix4D.rotateZ( Cmath.PI / 2 - radz );
       //Matrix4D transf1 = vt * Matrix4D.rotateZ( Cmath.PI / 2 - radz ) * Matrix4D.transl( 150, 0, 0 );
       //Matrix4D transf1 = vt * Matrix4D.rotateX( radx );
       //Matrix4D transf1 = vt;
         Matrix4D transf1 = vt * Matrix4D.rotate( Math.PI/12, rady, 0 ) * Matrix4D.transl( 150, 0, 0 );

         Matrix4D transf2 = vt * Matrix4D.transl( 0, 150 * Math.Sin(radx), 0 ) * Matrix4D.rotate ( radx, 0, radz );
       //Matrix4D transf2 = vt * Matrix4D.rotateZ( Cmath.PI / 2 - radz ) * Matrix4D.transl( -100, 0, 0 );
       //Matrix4D transf2 = vt;

       //Matrix4D transf3 = vt * Matrix4D.rotate( 0, rady, 0 ) * Matrix4D.transl( 150, 0, 0 );
         Matrix4D transf3 = vt * Matrix4D.rotate( Math.PI/12, rady, 0 ) * Matrix4D.transl( 150, 0, 0 );

         DrawMesh( g, mesh1, transf1 );
         DrawMesh( g, mesh2, transf2 );
         DrawMesh( g, mesh3, transf3 );


         //GetPixel() i SetPixel() su jako spore!!!!!!!!!!
         /*
         //sporo!!!
         Color c;
         for( int x = 0; x < draw_wid; x++ )
            for( int y = 0; y < draw_hei/2; y++ )
               c = bitmap.GetPixel( x, y );
                        
         //sporo!!!
         for( int x = 0; x < draw_wid; x++ )
            for( int y = 0; y < draw_hei/2; y++ )
               bitmap.SetPixel( x, y, Color.Red );
         */

         //GDI iscrtavanje je za nijansu brza!!!!!!!!!!!!!!
         /*
         IntPtr hdc = e.Graphics.GetHdc();  //zbog GDI SetPixel
         //zbog GDI SetPixel
       //Color pixelColor = GetPixelColor(x, y);
         Color pixelColor = Color.Red;
         uint colorRef = (uint)((pixelColor.B << 16) | (pixelColor.G << 8) | (pixelColor.R));  // GDI colors are BGR, not ARGB.
         for( int x = 0; x < draw_wid; x++ )
            for( int y = 0; y < draw_hei/2; y++ )
               GDI.SetPixel(hdc, x, y, colorRef);
         e.Graphics.ReleaseHdc(hdc);   //zbog GDI SetPixel
         */

         /*           
         //pointersko crtanje po bitmapi u memoriji je najbrze
         unsafe   //unsafe se navodi zbog upotrebe pointera (a treba ukljuciti i /unsafe u kompajleru)
         {
            BitmapData bitmapData = bitmap.LockBits( new Rectangle(0, 0, draw_wid, draw_hei), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb );
            ColorARGB* startingPosition = (ColorARGB*) bitmapData.Scan0;
            ColorARGB* position;
            
            for (int i = 0; i < draw_hei/2; i++)
            {
               for (int j = 0; j < draw_wid; j++)
               {
                  position = startingPosition + j + i * draw_wid;
                  position->SetColor( Color.Red );
               }
            }
            bitmap.UnlockBits(bitmapData);
         }
         */
         
         /*
         //pointersko crtanje po bitmapi u memoriji je najbrze
         unsafe   //unsafe se navodi zbog upotrebe pointera (a treba ukljuciti i /unsafe u kompajleru)
         {
            BitmapData bitmapData = bitmap.LockBits( new Rectangle(0, 0, draw_wid, draw_hei), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb );
            ColorARGB* position = (ColorARGB*) bitmapData.Scan0;
            
            for (int i = 0; i < draw_hei/2; i++)
               for (int j = 0; j < draw_wid; j++)
                  (position++)->SetColor( Color.Red );

            bitmap.UnlockBits(bitmapData);
         }
         */



         //viewing frustrum culling matrix
         //
         //      X           Y              Z             W
         //[  2n/(r-l),      0,        (r+l)/(r-l),       0       ]
         //[     0,       2n/(t-b),    (t+b)/(t-b),       0       ]
         //[     0,          0,       -(f+n)/(f-n),   -2fn/(f-n)  ]
         //[     0,          0,            -1,            0       ]

         //===============================================




         //prikaz double-buffering bitmape
         e.Graphics.DrawImage( bitmap, 0, 0, draw_wid, draw_hei );
         g?.Dispose();   //oslobadja memoriju ako nije null
       //h?.Dispose();


         Console.WriteLine(refresh_cnt++);
      }

      private void timer_draw_Tick(object sender, EventArgs e)
      {
         //Invalidate();
      }

      private void Form_KeyDown(object sender, KeyEventArgs e)
      {
         switch( e.KeyCode )
         {
            case Keys.Add:
               if(detail < 30)
               {
                  detail++;
                  UpdateModels();
                  Refresh();
               }
               break;
            case Keys.Subtract:
               if(detail > 0)
               {
                  detail--;
                  UpdateModels();
                  Refresh();
               }
               break;
            
            case Keys.Left:
               radx += radx_inc;
               rady += rady_inc;
               radz += radz_inc;
               Refresh();
               break;
            case Keys.Right:
               radx -= radx_inc;
               rady -= rady_inc;
               radz -= radz_inc;
               Refresh();
               break;

            case Keys.Multiply:
               if( Zncp > -400 )
               {
                  Zncp -= 2;
                  Refresh();
               }
               break;
            case Keys.Divide:
               if( Zncp <= -4 )
               {
                  Zncp += 2;
                  Refresh();
               }
               break;

            case Keys.C:
               draw_clipped_xy = ! draw_clipped_xy;
               Refresh();
               break;
            case Keys.D:
               depth_algorithm = (depth_algorithm+1) % depth_algorithm_cnt;
               Refresh();
               break;
            case Keys.W:
               draw_wireframe = ! draw_wireframe;
               Refresh();
               break;
            case Keys.S:
               draw_surfaces = ! draw_surfaces;
               Refresh();
               break;
            case Keys.N:
               draw_normals = ! draw_normals;
               Refresh();
               break;
         }
      }


   }
}
