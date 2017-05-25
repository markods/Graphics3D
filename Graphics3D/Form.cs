using MGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Drawing.Imaging;   //zbog BitmapData


public struct ColorARGB
{
    public byte B, G, R, A;   //ne menjati redosled!!!

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

      //globalne promenljive za crtanje
      private Bitmap bitmap;            //graficki bafer koji se prosledjuje PictureBox-u
      private bool crate_new_bitmap;    //da li treba menjati dimenzije grafickog bafera
      private double[][] zbuf;          //Z-buffer

      private int draw_wid, draw_hei;   //prostor u PictureBox-u za crtanje
      List<Mesh> scene;                 //skup svih mesh-ova na sceni

      double Zvp;    //Z koordinata VP  (      viewporta     ) u koordinatnom sistemu posmatraca
      double Zncp;   //Z koordinata NCP (near  clipping plane)                -||-
      double Zfcp;   //Z koordinata FCP (far       -||-      )                -||-
      double Xrcp;   //X koordinata RCP (right     -||-      )                -||-
      double Xlcp;   //X koordinata LCP (left      -||-      )                -||-
      double Ytcp;   //Y koordinata TCP (top       -||-      )                -||-
      double Ybcp;   //Y koordinata BCP (bottom    -||-      )                -||-
      Matrix4D pp;   //perspective projection matrix
      Matrix4D vt;   //viewport transformation matrix

      //moze jer je aplikacija single-threaded
      SolidBrush brush;
      Pen        pen;
      Point[]    trianglepoints;

      const int front_side =  1;
      const int both_sides =  0;
      const int back_side  = -1;

      bool clip_x;
      bool clip_y;

      bool draw_normals;
      bool EkvidistantneOrbitale;
      bool Resized;  //da li su tela preuvelicana
      bool Orbiting; //da li tela treba da rotiraju oko Sunca
      int scene_num;
      const int scene_cnt          = 5;

      int time_mode;
      const int time_manual        = 0;
      const int time_auto          = 1;
      const int time_mode_cnt      = 2;

      int surface_mode;
      const int no_surface         = 0;
      const int back_surface       = 1;
      const int both_surfaces      = 2;
      const int front_surface      = 3;
      const int surface_mode_cnt   = 4;

      int wireframe_mode;
      const int no_wireframe       = 0;
      const int back_wireframe     = 1;
      const int both_wireframes    = 2;
      const int front_wireframe    = 3;
      const int wireframe_mode_cnt = 4;

      int depth_mode;
      const int depth_unordered     = 0;
      const int depth_back2front    = 1;
      const int depth_zbuf          = 2;
      const int depth_mode_cnt      = 3;

      int shading_mode;
      const int no_shading          = 0;
      const int flat1_shading       = 1;
      const int flat2_shading       = 2;
      const int shading_mode_cnt    = 3;

      int background_mode;
      const int no_background       = 0;
      const int background_grid     = 1;
      const int background_all      = 2;
      const int background_lines    = 3;
      const int background_mode_cnt = 4;

      const double FaktorOrbitalnogSazimanja    = 100;  //faktor skaliranja orbita (toliko puta su tela bliza Suncu nego u stvarnosti)
      const double FaktorOrbitalnogUbrzanja     = 10;   //faktor ubrzanja vremena rotacije oko Sunca (toliko puta ce se tela brze vrteti oko Sunca nego u stvarnosti)
      const double FaktorSazimanjaSunca         = 1;    //faktor skaliranja poluprecnika Sunca (toliko puta ce Sunce biti manje uvećano u odnosu na druge planete)
      const double SimulPoluprecnikZemlje       = 5;    //broj jedinica u simuliranom sistemu koji predstavlja poluprecnik Zemlje
      const double SimulPoluprecnikOrbiteZemlje = SimulPoluprecnikZemlje * 149597890/6378 / FaktorOrbitalnogSazimanja; 

      private int detail;        //level of detail (useful for spheres, etc.)


      //potrepstine za osnovne rotacije
      private double radx;       //counter-clockwise ugao u radijanima oko Ox-ose
      private double rady;       //counter-clockwise ugao u radijanima oko Oy-ose
      private double radz;       //counter-clockwise ugao u radijanima oko Oz-ose

      private double radx_inc;   //increment ugla radx
      private double rady_inc;   //increment ugla rady
      private double radz_inc;   //increment ugla radz

      private double Tsim;       //simulirano vreme
      private double Tsim_orb;   //simulirano vreme za kretanje po orbiti (da bi se moglo pauzirati)
      private double Tsim_inc;   //inkrement simuliranog vremena


      public Form()
      {
         //inicijalizuje Form-u
         InitializeComponent();

         bitmap           = null;
         crate_new_bitmap = true;

         scene  = new List<Mesh>();
         detail = 2;
         UpdateModels();

         radx = 0;
         rady = 0;
         radz = 0;
         
         radx_inc  = 2*Math.PI/100;
         rady_inc  = 2*Math.PI/100;
         radz_inc  = 2*Math.PI/100;

         Tsim      = 0;
         Tsim_orb  = 0;
         Tsim_inc  = 2*Math.PI/365/24; //inkrementira se za 1 sat


         //===================Testing=====================
         Zvp  = -200;                      //-200 znaci da se viewport nalazi na udaljenosti 200 od posmatraca

         Zncp = -5;                        //ako je Zncp = Zvp to znaci da se clipping radi na viewportu, a vrednosti Zncp treba da budu negativne
         Zfcp = -100000;                   //far clipping plane

         double phi = (1+Math.Sqrt(5))/2;
         Xrcp =  200 * phi;
         Xlcp = -200 * phi;
         Ytcp =  200;
         Ybcp = -200;
         
         pp = Matrix4D.projXY(Zvp);        //perspective projection matrix
         vt = Matrix4D.transl(0, 0, Zvp);  //viewport transformation matrix

         clip_x = true;
         clip_y = true;
         scene_num             = 0;
         draw_normals          = false;
         EkvidistantneOrbitale = false;
         Resized               = false;
         Orbiting              = false;
         time_mode             = time_manual;
         wireframe_mode        = both_wireframes;
         surface_mode          = both_surfaces;
         depth_mode            = depth_back2front;
         shading_mode          = flat1_shading;
         background_mode       = background_all;

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


      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      private Vector4D Intersect( Vector4D v1, Vector4D v2, Vector4D N, Vector4D L )  
      {
         if( (v2 - v1)*N == 0 )
            return (v1 + v2) / 2;  //forensic?

         //returns intersection point of line (between v1 and v2) through plane (N = normal to plane, L = point which belongs to plane)
         return v1 + ((L - v1)*N) / ((v2 - v1)*N) * (v2 - v1);
      }


      
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      private void ClipLine(ref Vector3D v0, ref Vector3D v1)
      {
         throw new NotImplementedException("Buggy stuff.");   //forensic?

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
      
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
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


            
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            Color color = Color.Transparent; // boja kojom ce se tacka iscrtati
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
               for( int x = (int) begx;  (x - endx) * inc <= 0;  x += inc )
               {
                  Px =  draw_wid/2 + x;
                  Py =  draw_hei/2 - y;
                  if(     0 <= Px  &&  Px < draw_wid 
                       && 0 <= Py  &&  Py < draw_hei ) 
                  {
                     // tacno izracunavanje Z dubine tacke
                     Z = Intersect( Vector4D.zero, new Vector4D( x, y, Zvp ), N, T.center() );
                     
                     // tacku treba iscrtati ako je bliza posmatracu od one koja je vec nacrtana u tom pikselu
                     if( Z.getz() > zbuf[Py][Px] )
                     {
                        zbuf[Py][Px] = Z.getz();

                        
                        if( shading_mode == flat1_shading )
                           //boja trougla ce biti zatamnjenija sto je tuplji ugao pod kojim je posmatrac vidi povrsinu trougla u toj tacki
                           //tj. bice prikazana neizmenjena ukoliko posmatrac gleda tu tacku trougla pod pravim uglom 
                           color = Brighten( c, 0.75 * (1 - Z.unitize() * N.unitize()) );
                        else if( shading_mode == flat2_shading )
                           color = Brighten( c, 0.75 * Math.Pow( 1 - Z.unitize() * N.unitize(), 1.5) );
                        else //if( shading_mode == no_shading )
                           color = c;  //ako se ne radi shading, svaka tacka trougla je iste boje


                      //bitmap.SetPixel( draw_wid/2 + x, draw_hei/2 - y, color );   //SetPixel je spora funkcija, umesto nje koristi se pointerski pristup bitmapi
                        position = start_position + Px + Py * draw_wid;
                        position->SetColor( color );
                     }
                  }
               }
            }


            bitmap.UnlockBits(bitmapData);
         }
      }


      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      private void DrawTriangle(Graphics g, Triangle T, int DrawSide = 0)
      {
         //trougao T dat u koordinatnom sistemu posmatraca, ali na njega nije primenjena projekcija perspektive

         //strana na koju je orijentisan trougao u odnosu na posmatraca (side = -1 ako posmatrac vidi spoljnu stranu trougla, side = 1 ako posmatrac vidi unutrasnju stranu trougla, side = 0 ako ne vidi ni jednu)
         double side = Math.Sign( T.center() * Vector4D.vect_mult( T.getv(1) - T.getv(0), T.getv(2) - T.getv(0) ) );

         //ne iscrtava se trougao sa zadnje strane objekta (trougao koji je orijetisan na istu stranu na koju gleda posmatrac)
         if( side * DrawSide > 0 )
            return;

         Vector4D vertex;
         Triangle Tpp;    //trougao nakon projektovanja na viewport



         // clipping po Z osi
         if( ! ClipTriangle(g, ref T,   Vector4D.k, Zncp * Vector4D.k, DrawSide) ) return;
         if( ! ClipTriangle(g, ref T, - Vector4D.k, Zfcp * Vector4D.k, DrawSide) ) return;

         if( clip_x )
         {
            // clipping po levoj i desnoj viewport ravni
            if( ! ClipTriangle(g, ref T,   Vector4D.vect_mult( new Vector4D( Xrcp, Ybcp, Zvp), new Vector4D( Xrcp, Ytcp, Zvp) ), Vector4D.zero, DrawSide) ) return;
            if( ! ClipTriangle(g, ref T,   Vector4D.vect_mult( new Vector4D( Xlcp, Ytcp, Zvp), new Vector4D( Xlcp, Ybcp, Zvp) ), Vector4D.zero, DrawSide) ) return;
         }
         
         if( clip_y )
         {
            // clipping po gornjoj i donjoj viewport ravni
            if( ! ClipTriangle(g, ref T,   Vector4D.vect_mult( new Vector4D( Xrcp, Ytcp, Zvp), new Vector4D( Xlcp, Ytcp, Zvp) ), Vector4D.zero, DrawSide) ) return;
            if( ! ClipTriangle(g, ref T,   Vector4D.vect_mult( new Vector4D( Xlcp, Ybcp, Zvp), new Vector4D( Xrcp, Ybcp, Zvp) ), Vector4D.zero, DrawSide) ) return;
         }

         //projekcija perspektive se radi nakon Near Plane Clipping-a
         Tpp = pp * T;
         Tpp.normalize();


         for( int k = 0; k < Triangle.ver_cnt; k++ )
         {
            vertex = Tpp.getv(k);
            trianglepoints[k].X = (int) vertex.getnormx();
            trianglepoints[k].Y = (int) vertex.getnormy();
         }

       //pen.Color = T.get_color();
       //brush.Color = Color.Transparent;

         

         if(    (surface_mode == front_surface && side <= 0)
             || (surface_mode == back_surface  && side >= 0)
             ||  surface_mode == both_surfaces )
         {
            if( depth_mode == depth_zbuf )
            {
               Color c = (side < 0 ? Brighten(T.get_color(), 0.5) : Color.Indigo);
               RasterizeTriangle( g, Tpp, T, c );
            }
            else
            {
               if (depth_mode == depth_back2front )
                  brush.Color = (side < 0 ? Brighten(T.get_color(), 0.5) : Color.Indigo);
               else
                  brush.Color = (side < 0 ? Color.Yellow : Color.Indigo);

               g.FillPolygon( brush, trianglepoints );
            }
         }



         
         if( side > 0 )
            pen.Color = Color.DarkGray;  //posmatrac gleda u unutrasnju stranu trougla
         else 
            pen.Color = T.get_color();   //posmatrac gleda u spoljnu stranu trougla


         if(    (wireframe_mode == front_wireframe && side <= 0 )
             || (wireframe_mode == back_wireframe  && side >= 0 )
             ||  wireframe_mode == both_wireframes )
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

         List<Triangle> triangles = S.triangles;
         if(    depth_mode == depth_unordered )
         {
            for( int i = 0; i < S.triangle_cnt(); i++ )
               DrawTriangle( g, M * triangles[i],  both_sides );  //applies shape transformations to each triangle of the specific shape
         }
         else if(    depth_mode == depth_back2front 
                  || depth_mode == depth_zbuf )
         {
            for( int i = 0; i < S.triangle_cnt(); i++ )
               DrawTriangle( g, M * triangles[i], back_side  );  //applies shape transformations to each triangle of the specific shape, and draw only back-side triangles
            for( int i = 0; i < S.triangle_cnt(); i++ )
               DrawTriangle( g, M * triangles[i], front_side );  //applies shape transformations to each triangle of the specific shape, and draw only front-side triangles
         }         
      }



      private void UpdateModels()
      {
         scene.Clear();

         switch( scene_num )
         {
            case 0:
               scene.Add( Mesh.quboid      ( 100, 100, 100, Color.Orange,     detail, "kocka" ) );
               break;

            case 1:
               scene.Add( Mesh.circle      (  30,           Color.Orange, 5 * detail,   "circle"        ) );
               scene.Add( Mesh.psphere     (  30,           Color.Magenta,    detail,   "psphere"       ) );
               scene.Add( Mesh.hollowcircle(  30,  15,      Color.Red,    5 * detail,   "hollowcircle"  ) );
               scene.Add( Mesh.quboid      (  45,  45,  45, Color.Cyan,       detail,   "quboid"        ) );
               scene.Add( Mesh.uvsphere    (  30,           Color.Green,      detail,   "uvsphere"      ) );
               scene.Add( Mesh.rectangle   (  30,  30,      Color.Blue,   3 * detail,   "rectangle"     ) );
               scene.Add( Mesh.icosphere   (  30,           Color.Gray,       detail/3, "icosphere"     ) );
               scene.Add( Mesh.cyllinder   (  15,  30,      Color.Brown,  3 * detail,   "cyllinder"     ) );
               break;

            case 2:
               scene.Add( Mesh.cone        (  75, 150,      Color.Red,    3 * detail,   "kupa1" ) );
               scene.Add( Mesh.cone        (  50, 200,      Color.Blue,   3 * detail,   "kupa2" ) );
               break;

            case 3:
               scene.Add( Mesh.uvsphere    (  50,           Color.Magenta,    detail,   "Saturn" ) );
               scene.Add( Mesh.hollowcircle( 120,  70,      Color.Orange, 3 * detail,   "Saturnovi prstenovi" ) );
               scene.Add( Mesh.cone        (  50, 100,      Color.Red,        detail,   "svemirski brod" ) );
               break;

            case 4:
               scene.Add( Mesh.uvsphere    (                                                         109.125 * SimulPoluprecnikZemlje / FaktorSazimanjaSunca,  Color.Yellow,       10 * detail,   "Sunce"               ) );
               scene.Add( Mesh.uvsphere    ( (Resized ?                              25  : 1) *        0.382 * SimulPoluprecnikZemlje,                         Color.DarkOrange,    3 * detail,   "Merkur"              ) );
               scene.Add( Mesh.uvsphere    ( (Resized ?                              25  : 1) *        0.949 * SimulPoluprecnikZemlje,                         Color.Red,           5 * detail,   "Venera"              ) );
               scene.Add( Mesh.uvsphere    ( (Resized ?                              25  : 1) *        1.000 * SimulPoluprecnikZemlje,                         Color.Blue,          5 * detail,   "Zemlja"              ) );
               scene.Add( Mesh.uvsphere    ( (Resized ?                              25  : 1) *        0.273 * SimulPoluprecnikZemlje,                         Color.Gray,          2 * detail,   "Mesec"               ) );
               scene.Add( Mesh.uvsphere    ( (Resized ?                              25  : 1) *        0.530 * SimulPoluprecnikZemlje,                         Color.Brown,         4 * detail,   "Mars"                ) );
               scene.Add( Mesh.uvsphere    ( (Resized ? (EkvidistantneOrbitale ? 5 : 25) : 1) *       11.200 * SimulPoluprecnikZemlje,                         Color.OrangeRed,     9 * detail,   "Jupiter"             ) );
               scene.Add( Mesh.uvsphere    ( (Resized ? (EkvidistantneOrbitale ? 5 : 25) : 1) *        9.410 * SimulPoluprecnikZemlje,                         Color.Orange,        9 * detail,   "Saturn"              ) );
               scene.Add( Mesh.hollowcircle( (Resized ? (EkvidistantneOrbitale ? 5 : 25) : 1) * 2.00 * 9.410 * SimulPoluprecnikZemlje, 
                                             (Resized ? (EkvidistantneOrbitale ? 5 : 25) : 1) * 1.35 * 9.410 * SimulPoluprecnikZemlje,                         Color.Orange,        9 * detail,   "Saturnovi prstenovi" ) );
               scene.Add( Mesh.uvsphere    ( (Resized ? (EkvidistantneOrbitale ? 5 : 25) : 1) *        3.980 * SimulPoluprecnikZemlje,                         Color.Cyan,          7 * detail,   "Uran"                ) );
               scene.Add( Mesh.uvsphere    ( (Resized ? (EkvidistantneOrbitale ? 5 : 25) : 1) *        3.810 * SimulPoluprecnikZemlje,                         Color.DarkCyan,      7 * detail,   "Neptun"              ) );
               break;
         }
            
         for( int i = 0; i < scene.Count; i++ )
            Console.WriteLine("Mesh '{0}' triangle count: {1}", scene[i].name, scene[i].triangle_cnt());

         UpdatePositions();
      }

      private void UpdatePositions()
      {
         switch( scene_num )
         {
            case 0:
               for( int i = 0; i < scene.Count; i++ )
                  if( scene[i].name == "kocka" )                    scene[i].transf = Matrix4D.rotateZ( Cmath.PI / 2 - radz ) * Matrix4D.rotateY( Cmath.PI / 2 - radx ) * Matrix4D.rotateX( Cmath.PI / 2 - radx );
                  else                                              scene[i].transf = Matrix4D.I;
               break;

            case 1:
               for( int i = 0; i < scene.Count; i++ )
                  if(      scene[i].name == "circle"       )        scene[i].transf = Matrix4D.rotate( radx, 0, 0 ) * Matrix4D.rotate( 0, 0 * Math.PI/4, 0 ) * Matrix4D.transl( 100, 0, 0 ) * Matrix4D.rotate( Math.PI/6, 0, 0 );
                  else if( scene[i].name == "psphere"      )        scene[i].transf = Matrix4D.rotate( radx, 0, 0 ) * Matrix4D.rotate( 0, 1 * Math.PI/4, 0 ) * Matrix4D.transl( 100, 0, 0 );
                  else if( scene[i].name == "hollowcircle" )        scene[i].transf = Matrix4D.rotate( radx, 0, 0 ) * Matrix4D.rotate( 0, 2 * Math.PI/4, 0 ) * Matrix4D.transl( 100, 0, 0 ) * Matrix4D.rotate( Math.PI/6, 0, 0 );
                  else if( scene[i].name == "quboid"       )        scene[i].transf = Matrix4D.rotate( radx, 0, 0 ) * Matrix4D.rotate( 0, 3 * Math.PI/4, 0 ) * Matrix4D.transl( 100, 0, 0 );
                  else if( scene[i].name == "uvsphere"     )        scene[i].transf = Matrix4D.rotate( radx, 0, 0 ) * Matrix4D.rotate( 0, 4 * Math.PI/4, 0 ) * Matrix4D.transl( 100, 0, 0 );
                  else if( scene[i].name == "rectangle"    )        scene[i].transf = Matrix4D.rotate( radx, 0, 0 ) * Matrix4D.rotate( 0, 5 * Math.PI/4, 0 ) * Matrix4D.transl( 100, 0, 0 ) * Matrix4D.rotate( Math.PI/6, 0, 0 );
                  else if( scene[i].name == "icosphere"    )        scene[i].transf = Matrix4D.rotate( radx, 0, 0 ) * Matrix4D.rotate( 0, 6 * Math.PI/4, 0 ) * Matrix4D.transl( 100, 0, 0 );
                  else if( scene[i].name == "cyllinder"    )        scene[i].transf = Matrix4D.rotate( radx, 0, 0 ) * Matrix4D.rotate( 0, 7 * Math.PI/4, 0 ) * Matrix4D.transl( 100, 0, 0 );
                  else                                              scene[i].transf = Matrix4D.I;
               break;

            case 2:
               for( int i = 0; i < scene.Count; i++ )
                  if(      scene[i].name == "kupa1" )               scene[i].transf = Matrix4D.rotate( 0, 0, Math.PI/4 ) * Matrix4D.transl( 0, -150 * Math.Cos(radx), 0 );
                  else if( scene[i].name == "kupa2" )               scene[i].transf =                                      Matrix4D.transl( 0,  100 * Math.Cos(radx), 0 ) * Matrix4D.rotate( Math.PI, 0, 0 );
                  else                                              scene[i].transf = Matrix4D.I;
               break;

            case 3:
               for( int i = 0; i < scene.Count; i++ )
                  if(      scene[i].name == "Saturn"
                        || scene[i].name == "Saturnovi prstenovi" ) scene[i].transf = Matrix4D.rotate( Math.PI/12, rady, 0 ) * Matrix4D.transl( 150, 0, 0 );
                  else if( scene[i].name == "svemirski brod" )      scene[i].transf = Matrix4D.transl( 0, 150 * Math.Sin(radx), 0 ) * Matrix4D.rotate ( radx, 0, radz );
                  else                                              scene[i].transf = Matrix4D.I;
               break;

            case 4:
               for( int i = 0; i < scene.Count; i++ )                               //rotacija oko Sunca                                                      udaljenost od Sunca                                                                            nagib sopstvene ose rotacije na ekliptiku      rotacija oko sopstvene ose                  
                  if(      scene[i].name == "Sunce"               ) scene[i].transf = Matrix4D.rotate( 0,                0      * FaktorOrbitalnogUbrzanja, 0 ) * Matrix4D.transl( 0, 0, (EkvidistantneOrbitale ?   0 :  0.00) * SimulPoluprecnikOrbiteZemlje ) * Matrix4D.rotate(    7.25/180*Math.PI, 0, 0 ) * Matrix4D.rotate( 0, 365.5*Tsim/2/Math.PI/  25.38, 0 );
                  else if( scene[i].name == "Merkur"              ) scene[i].transf = Matrix4D.rotate( 0, Tsim_orb/  0.241      * FaktorOrbitalnogUbrzanja, 0 ) * Matrix4D.transl( 0, 0, (EkvidistantneOrbitale ?   1 :  0.38) * SimulPoluprecnikOrbiteZemlje ) * Matrix4D.rotate(    0.00/180*Math.PI, 0, 0 ) * Matrix4D.rotate( 0, 365.5*Tsim/2/Math.PI/  58.64, 0 );
                  else if( scene[i].name == "Venera"              ) scene[i].transf = Matrix4D.rotate( 0, Tsim_orb/  0.615      * FaktorOrbitalnogUbrzanja, 0 ) * Matrix4D.transl( 0, 0, (EkvidistantneOrbitale ?   2 :  0.72) * SimulPoluprecnikOrbiteZemlje ) * Matrix4D.rotate(  177.30/180*Math.PI, 0, 0 ) * Matrix4D.rotate( 0, 365.5*Tsim/2/Math.PI/-243.02, 0 );
                  else if( scene[i].name == "Zemlja"              ) scene[i].transf = Matrix4D.rotate( 0, Tsim_orb/  1.000      * FaktorOrbitalnogUbrzanja, 0 ) * Matrix4D.transl( 0, 0, (EkvidistantneOrbitale ?   3 :  1.00) * SimulPoluprecnikOrbiteZemlje ) * Matrix4D.rotate(   23.44/180*Math.PI, 0, 0 ) * Matrix4D.rotate( 0, 365.5*Tsim/2/Math.PI/   1.00, 0 );
                  else if( scene[i].name == "Mesec"               ) scene[i].transf = Matrix4D.rotate( 0, Tsim_orb/  1.000      * FaktorOrbitalnogUbrzanja, 0 ) * Matrix4D.transl( 0, 0, (EkvidistantneOrbitale ?   3 :  1.00) * SimulPoluprecnikOrbiteZemlje ) * Matrix4D.rotate(   23.44/180*Math.PI, 0, 0 ) * Matrix4D.rotate( 0, 365.5*Tsim/2/Math.PI/   1.00, 0 )
                                                                                    * Matrix4D.rotate( 0, Tsim    / (365/27.32) * FaktorOrbitalnogUbrzanja, 0 ) * Matrix4D.transl(       (EkvidistantneOrbitale ? 120 : 60.00) * SimulPoluprecnikZemlje, 0, 0 ) * Matrix4D.rotate(    0.00/180*Math.PI, 0, 0 ) * Matrix4D.rotate( 0, 365.5*Tsim/2/Math.PI/  27.32, 0 );
                  else if( scene[i].name == "Mars"                ) scene[i].transf = Matrix4D.rotate( 0, Tsim_orb/  1.881      * FaktorOrbitalnogUbrzanja, 0 ) * Matrix4D.transl( 0, 0, (EkvidistantneOrbitale ?   4 :  1.52) * SimulPoluprecnikOrbiteZemlje ) * Matrix4D.rotate(   25.19/180*Math.PI, 0, 0 ) * Matrix4D.rotate( 0, 365.5*Tsim/2/Math.PI/   1.03, 0 );
                  else if( scene[i].name == "Jupiter"             ) scene[i].transf = Matrix4D.rotate( 0, Tsim_orb/ 11.863      * FaktorOrbitalnogUbrzanja, 0 ) * Matrix4D.transl( 0, 0, (EkvidistantneOrbitale ?   5 :  5.20) * SimulPoluprecnikOrbiteZemlje ) * Matrix4D.rotate(    3.12/180*Math.PI, 0, 0 ) * Matrix4D.rotate( 0, 365.5*Tsim/2/Math.PI/   0.41, 0 );
                  else if( scene[i].name == "Saturn"              ) scene[i].transf = Matrix4D.rotate( 0, Tsim_orb/ 29.447      * FaktorOrbitalnogUbrzanja, 0 ) * Matrix4D.transl( 0, 0, (EkvidistantneOrbitale ?   6 :  9.54) * SimulPoluprecnikOrbiteZemlje ) * Matrix4D.rotate(   26.73/180*Math.PI, 0, 0 ) * Matrix4D.rotate( 0, 365.5*Tsim/2/Math.PI/   0.44, 0 );
                  else if( scene[i].name == "Saturnovi prstenovi" ) scene[i].transf = Matrix4D.rotate( 0, Tsim_orb/ 29.447      * FaktorOrbitalnogUbrzanja, 0 ) * Matrix4D.transl( 0, 0, (EkvidistantneOrbitale ?   6 :  9.54) * SimulPoluprecnikOrbiteZemlje ) * Matrix4D.rotate(   26.73/180*Math.PI, 0, 0 ) * Matrix4D.rotate( 0, 365.5*Tsim/2/Math.PI/   0.44, 0 );
                  else if( scene[i].name == "Uran"                ) scene[i].transf = Matrix4D.rotate( 0, Tsim_orb/ 84.017      * FaktorOrbitalnogUbrzanja, 0 ) * Matrix4D.transl( 0, 0, (EkvidistantneOrbitale ?   7 : 19.22) * SimulPoluprecnikOrbiteZemlje ) * Matrix4D.rotate(   97.86/180*Math.PI, 0, 0 ) * Matrix4D.rotate( 0, 365.5*Tsim/2/Math.PI/  -0.72, 0 );
                  else if( scene[i].name == "Neptun"              ) scene[i].transf = Matrix4D.rotate( 0, Tsim_orb/164.791      * FaktorOrbitalnogUbrzanja, 0 ) * Matrix4D.transl( 0, 0, (EkvidistantneOrbitale ?   8 : 30.06) * SimulPoluprecnikOrbiteZemlje ) * Matrix4D.rotate(   29.58/180*Math.PI, 0, 0 ) * Matrix4D.rotate( 0, 365.5*Tsim/2/Math.PI/   0.67, 0 );
                  else                                              scene[i].transf = Matrix4D.I;
               break;
         }
      }





      private void PictureBox_Paint(object sender, PaintEventArgs e)
      {
         //inicijalizacija grafickog bafera (koji se koristi za double-buffering)
         if( crate_new_bitmap == true )
         {
            bitmap?.Dispose();

            draw_wid = (int) ClientRectangle.Width;
            draw_hei = (int) ClientRectangle.Height;

            bitmap   = new Bitmap(draw_wid, draw_hei);

            //priprema Z-buffera
            zbuf = new double[draw_hei][];
            for( int i = 0; i < draw_hei; i++ )
               zbuf[i] = new double[draw_wid];
         }

         //praznjenje Z buffera
         if( depth_mode == depth_zbuf )         
            for( int y = 0; y < draw_hei; y++ )
               for(int x = 0; x < draw_wid; x++ )
                  zbuf[y][x] = -100000000;



         Graphics g = Graphics.FromImage(bitmap);
         g.TranslateTransform( draw_wid/2, draw_hei/2 );  //postavlja koordinatni pocetak PictureBox-a u njegov centar
         g.ScaleTransform( 1, -1 );                       //menja smer y ose tako da y koordinate rastu navise (po defaultu rastu nanize)


         Pen AxisPen    = new Pen(Color.Gray,      1);
         Pen Grid10Pen  = new Pen(Color.White,     1);
         Pen Grid100Pen = new Pen(Color.LightGray, 1);

         //iscrtavanje grida
         if(    background_mode == background_grid
             || background_mode == background_all )
         {
            //iscrtavanje koordinatnog sistema viewporta

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
         }

         //iscrtavanje background linija
         if(    background_mode == background_lines
             || background_mode == background_all )
         {

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
      

         //iscrtavanje mesh-ova
         for( int i = 0; i < scene.Count; i++ )
            DrawMesh( g, scene[i], vt * scene[i].transf );




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


         Console.WriteLine(refresh_cnt++);
      }

      private void Timer_Draw_Tick(object sender, EventArgs e)
      {
         //Invalidate();
         if( time_mode == time_auto )
         {
            radx += radx_inc;
            rady += rady_inc;
            radz += radz_inc;
            Tsim += Tsim_inc;
            if( Orbiting )
               Tsim_orb += Tsim_inc;
            UpdatePositions();
            Console.WriteLine("Time increased" );
            Refresh();
         }
      }

      private void Form_KeyDown(object sender, KeyEventArgs e)
      {
         switch( e.KeyCode )
         {
            //detalji mesh-eva
            case Keys.Add:       if(detail < 10) {    detail++;   UpdateModels();    Console.WriteLine("Degree of detail increased to {0}", detail );   Refresh();   }   break;
            case Keys.Subtract:  if(detail > 0)  {    detail--;   UpdateModels();    Console.WriteLine("Degree of detail decreased to {0}", detail );   Refresh();   }   break;

            //protok vremena
            case Keys.PageUp:    if( time_mode == time_manual && ModifierKeys == Keys.None)   {  radx +=    radx_inc;    rady +=    rady_inc;    radz +=    radz_inc;    Tsim +=    Tsim_inc;   if( Orbiting ) Tsim_orb +=    Tsim_inc;   UpdatePositions();    Console.WriteLine("Time increased"                 );    Refresh();   }   
                            else if( time_mode == time_manual && ModifierKeys == Keys.Shift)  {  radx += 10*radx_inc;    rady += 10*rady_inc;    radz += 10*radz_inc;    Tsim += 24*Tsim_inc;   if( Orbiting ) Tsim_orb += 24*Tsim_inc;   UpdatePositions();    Console.WriteLine("Time increased (10 increments)" );    Refresh();   }   break;
            case Keys.PageDown:  if( time_mode == time_manual && ModifierKeys == Keys.None )  {  radx -=    radx_inc;    rady -=    rady_inc;    radz -=    radz_inc;    Tsim -=    Tsim_inc;   if( Orbiting ) Tsim_orb -=    Tsim_inc;   UpdatePositions();    Console.WriteLine("Time decreased"                 );    Refresh();   } 
                            else if( time_mode == time_manual && ModifierKeys == Keys.Shift)  {  radx -= 10*radx_inc;    rady -= 10*rady_inc;    radz -= 10*radz_inc;    Tsim -= 24*Tsim_inc;   if( Orbiting ) Tsim_orb -= 24*Tsim_inc;   UpdatePositions();    Console.WriteLine("Time decreased (10 increments)" );    Refresh();   }   break;

            //kretanje kamere 
            case Keys.Home:    if( ModifierKeys == Keys.None    )  {   vt = Matrix4D.transl( 0,  0,   3) * vt;    Console.WriteLine("Camera moved forward" );             Refresh();   }   
                          else if( ModifierKeys == Keys.Shift   )  {   vt = Matrix4D.transl( 0,  0,  30) * vt;    Console.WriteLine("Camera moved fast forward" );        Refresh();   } 
                          else if( ModifierKeys == Keys.Control )  {   vt = Matrix4D.transl( 0,  0, 300) * vt;    Console.WriteLine("Camera moved extra fast forward" );  Refresh();   }   break;
            case Keys.End:     if( ModifierKeys == Keys.None    )  {   vt = Matrix4D.transl( 0,  0,  -3) * vt;    Console.WriteLine("Camera moved backward" );            Refresh();   }   
                          else if( ModifierKeys == Keys.Shift   )  {   vt = Matrix4D.transl( 0,  0, -30) * vt;    Console.WriteLine("Camera moved fast backward" );       Refresh();   } 
                          else if( ModifierKeys == Keys.Control )  {   vt = Matrix4D.transl( 0,  0,-300) * vt;    Console.WriteLine("Camera moved extra fast backward" ); Refresh();   }   break;
            case Keys.Left:    if( ModifierKeys == Keys.Alt     )  {   vt = Matrix4D.transl( 3,  0,   0) * vt;    Console.WriteLine("Camera moved left" );                Refresh();   } 
                          else if( ModifierKeys == Keys.None    )  {   vt = Matrix4D.rotateY(-0.03)      * vt;    Console.WriteLine("Camera oriented toward left" );      Refresh();   }   break;
            case Keys.Right:   if( ModifierKeys == Keys.Alt     )  {   vt = Matrix4D.transl(-3,  0,   0) * vt;    Console.WriteLine("Camera moved right" );               Refresh();   }
                          else if( ModifierKeys == Keys.None    )  {   vt = Matrix4D.rotateY( 0.03)      * vt;    Console.WriteLine("Camera oriented toward right" );     Refresh();   }   break;
            case Keys.Up:      if( ModifierKeys == Keys.Alt     )  {   vt = Matrix4D.transl( 0,  -3,  0) * vt;    Console.WriteLine("Camera moved up" );                  Refresh();   }
                          else if( ModifierKeys == Keys.None    )  {   vt = Matrix4D.rotateX(-0.03)      * vt;    Console.WriteLine("Camera oriented upward" );           Refresh();   }   break;
            case Keys.Down:    if( ModifierKeys == Keys.Alt     )  {   vt = Matrix4D.transl( 0,  3,   0) * vt;    Console.WriteLine("Camera moved down" );                Refresh();   }
                          else if( ModifierKeys == Keys.None    )  {   vt = Matrix4D.rotateX( 0.03)      * vt;    Console.WriteLine("Camera oriented downward" );         Refresh();   }   break;

            //pomeranje near clipping plane-a
            case Keys.Multiply:   if( Zncp > -400 ) {   Zncp -= 2;   Console.WriteLine("Near Clipping Plane moved forward  to Zncp={0}", Zncp );   Refresh();   }   break;
            case Keys.Divide:     if( Zncp <= -4 )  {   Zncp += 2;   Console.WriteLine("Near Clipping Plane moved backward to Zncp={0}", Zncp );   Refresh();   }   break;

            //promena scene
            case Keys.Tab:    if( ModifierKeys == Keys.None )  { scene_num = (            scene_num + 1) % scene_cnt;   UpdateModels();   Console.WriteLine("Current scene num={0}", scene_num );   Refresh();  }
                         else if( ModifierKeys == Keys.Shift ) { scene_num = (scene_cnt + scene_num - 1) % scene_cnt;   UpdateModels();   Console.WriteLine("Current scene num={0}", scene_num );   Refresh();  }  break;

            //promena clipping moda i poravnavanje kamere sa nekom osom
            case Keys.X:       if( ModifierKeys == Keys.Control )  {  clip_x = ! clip_x;                                               Console.WriteLine("X-clipping mode {0}", clip_x );   Refresh();   } 
                          else if( ModifierKeys == Keys.None )     {  vt = Matrix4D.transl(0, 0, Zvp) * Matrix4D.rotateY(-Math.PI/2);  Console.WriteLine("Camera reset to X axis" );        Refresh();   }   break; //posmatra se iz pravca X ose
            case Keys.Y:       if( ModifierKeys == Keys.Control )  {  clip_y = ! clip_y;                                               Console.WriteLine("Y-clipping mode {0}", clip_y );   Refresh();   }
                          else if( ModifierKeys == Keys.None )     {  vt = Matrix4D.transl(0, 0, Zvp) * Matrix4D.rotateX( Math.PI/2);  Console.WriteLine("Camera reset to Y axis" );        Refresh();   }   break; //posmatra se iz pravca Y ose
            case Keys.Z:       if( ModifierKeys == Keys.None )     {  vt = Matrix4D.transl(0, 0, Zvp);                                 Console.WriteLine("Camera reset to Z axis" );        Refresh();   }   break;// posmatra se iz pravca Z ose (inicijalni pravac) 

            //promena modova prikazivanja
            case Keys.T:    time_mode = (time_mode + 1) % time_mode_cnt;                                   Console.WriteLine("Time mode {0}", time_mode );                           Refresh();   break;
            case Keys.D:    depth_mode = (depth_mode + 1) % depth_mode_cnt;                                Console.WriteLine("Depth mode {0}", depth_mode );                         Refresh();   break;
            case Keys.W:    wireframe_mode = (wireframe_mode + 1) % wireframe_mode_cnt;                    Console.WriteLine("Wireframe mode {0}", wireframe_mode );                 Refresh();   break;
            case Keys.S:    surface_mode = (surface_mode + 1) % surface_mode_cnt;                          Console.WriteLine("Surface mode {0}", surface_mode );                     Refresh();   break;
            case Keys.H:    shading_mode = (shading_mode + 1) % shading_mode_cnt;                          Console.WriteLine("Shading mode {0}", shading_mode );                     Refresh();   break;
            case Keys.N:    draw_normals = ! draw_normals;                                                 Console.WriteLine("Draw normals {0}", draw_normals );                     Refresh();   break;
            case Keys.B:    background_mode = (background_mode + 1) % background_mode_cnt;                 Console.WriteLine("Background mode {0}", background_mode );               Refresh();   break;
            case Keys.E:    EkvidistantneOrbitale = ! EkvidistantneOrbitale;               UpdateModels(); Console.WriteLine("Equidistant orbitals {0}", EkvidistantneOrbitale );    Refresh();   break;
            case Keys.R:    Resized = ! Resized;                                           UpdateModels(); Console.WriteLine("Resized {0}", Resized );                               Refresh();   break;
            case Keys.O:    Orbiting = ! Orbiting;                                                         Console.WriteLine("Orbiting {0}", Orbiting );                             Refresh();   break;
         }
      }


   }
}
