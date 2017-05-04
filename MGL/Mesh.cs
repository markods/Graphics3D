using System;
using System.Collections.Generic;
using System.Drawing;

namespace MGL
{
   public class Mesh
   {
      private List<Triangle> triangles;   //shape's faces (the keyword faces doesn't sound right)


      #region Constructors
      public Mesh()
      {
         triangles = new List<Triangle>();
      }

      //namerno je shallow-copy (nema smisla praviti novi niz trouglova od starog, pri konstrukciji shape-a)
      public Mesh(List<Triangle> _triangles)
      {
         //converter lambda function ( X=>Y ) must be specified for function Array<>.ConvertAll()
         triangles = _triangles;
      }

      //treba deep-copy, jer je copy-konstruktor
      public Mesh(Mesh S)
      {
         //converter lambda function ( X=>Y ) must be specified for function Array<>.ConvertAll()
         triangles = (S.triangles).ConvertAll( Triangle => new Triangle(Triangle) );
      }
      #endregion


      #region Common 2D shapes
      /*
      public static Mesh equitriangle(double a, int level = 0)   //a - side length
      {
         return ;
      }
      */
      public static Mesh rectangle(double ax, double az, Color c, int level = 0)   //ax - length, az - width
      {
         if( ax <= 0 || az <= 0 )
            throw new ArgumentException("Both dimensions of rectangle must be greater than zero");
         if( level < 0 )
            throw new ArgumentException("Level of refinement must be non-negative");

         level += 1;   //magical constant



         Mesh S = new Mesh();
         Vector4D A, B, C, D;


         int    edges = level;   //number of sub-rectangle edges on rectangle edge
         double lenx  = ax / edges;
         double lenz  = az / edges;

         for( int i = 0; i < edges; i++ )
            for( int j = 0; j < edges; j++ )
            {
               A = new Vector4D(-ax/2 +  j    * lenx,   0,   -az/2 + (i+1) * lenz );
               B = new Vector4D(-ax/2 + (j+1) * lenx,   0,   -az/2 + (i+1) * lenz );
               C = new Vector4D(-ax/2 + (j+1) * lenx,   0,   -az/2 +  i    * lenz );
               D = new Vector4D(-ax/2 +  j    * lenx,   0,   -az/2 +  i    * lenz );

               //trouglovi su orijentisani na gore
               S.add(new Triangle(A, B, C, c));
               S.add(new Triangle(A, C, D, c));
            }


         return S;
      }


      public static Mesh circle   (double r,             Color c, int level = 0)   //r - radius
      {
         if( r <= 0 )
            throw new ArgumentException("Radius of circle must be greater than zero");
         if( level < 0 )
            throw new ArgumentException("Level of refinement must be non-negative");

         level += 3;   //magical constant


         Mesh S = new Mesh();
         Vector4D O, A, B;


         int    edges = level;                //number of edges (slices) that approximate the circle
         double angle = 2*Cmath.PI / edges;   //angle of a slice of the circle (like orange slices)


         O = Vector4D.zero;
         for( int k = 0; k < edges; k++ )
         {
            //Matrix4D mnozenje sa skalarom mnozi i w komponentu (donje desno polje matrice) sto ne treba da se desava
            A = Matrix4D.rotateY( angle* k    ) * (r * Vector4D.i);
            B = Matrix4D.rotateY( angle*(k+1) ) * (r * Vector4D.i);

            S.add( new Triangle(A, B, O, c) );
         }

         return S;
      }

      public static Mesh hollowcircle (double rout, double rin, Color c, int level = 0)   //rout - spoljni radius, rin - unutrasnji radius
      {
         if( rout <= 0 || rin <= 0 )
            throw new ArgumentException("Radius of circle must be greater than zero");
         if( level < 0 )
            throw new ArgumentException("Level of refinement must be non-negative");

         level += 3;   //magical constant


         Mesh S = new Mesh();
         Vector4D A, B, C, D;


         int    edges = level;                //number of edges (slices) that approximate the circle
         double angle = 2*Cmath.PI / edges;   //angle of a slice of the circle (like orange slices)


         for( int k = 0; k < edges; k++ )
         {
            //Matrix4D mnozenje sa skalarom mnozi i w komponentu (donje desno polje matrice) sto ne treba da se desava
            A = Matrix4D.rotateY( angle* k    ) * (rout * Vector4D.i);
            B = Matrix4D.rotateY( angle*(k+1) ) * (rout * Vector4D.i);
            C = Matrix4D.rotateY( angle*(k+1) ) * (rin  * Vector4D.i);
            D = Matrix4D.rotateY( angle* k    ) * (rin  * Vector4D.i);

            S.add( new Triangle(A, B, C, c) );
            S.add( new Triangle(A, C, D, c) );
         }

         return S;
      }

      #endregion


      #region Common 3D shapes
      public static Mesh quboid   (double ax, double az, double ay, Color c, int level = 0)   //ax - length, az - width, ay - height
      {
         if( ax <= 0 || az <= 0 || ay <= 0 )
            throw new ArgumentException("All three dimensions of quboid must be greater than zero");
         if( level < 0 )
            throw new ArgumentException("Level of refinement must be non-negative");



         Mesh S = new Mesh();


         //vektori nomala ovih pravougaonika su u smeru trece ose (koja nije navedena u imenu) i sa centrom u koordinatnom pocetku
         Mesh rect_xz =                                 rectangle(ax, az, c, level);
         Mesh rect_xy = Matrix4D.rotateX( Cmath.PI/2) * rectangle(ax, ay, c, level);
         Mesh rect_zy = Matrix4D.rotateZ(-Cmath.PI/2) * rectangle(ay, az, c, level);

         //NAPOMENA - svi trouglovi su orijentisani counter-clockwise, vektori normale su ka spoljasnosti kocke

         S.add( (Matrix4D.transl(0,  ay/2, 0)                             ) * rect_xz );   //E-F-G-H
         S.add( (Matrix4D.transl(0, -ay/2, 0) * Matrix4D.rotateX(Cmath.PI)) * rect_xz );   //A-B-C-D

         S.add( (Matrix4D.transl(0, 0,  az/2)                             ) * rect_xy );   //A-B-F-E
         S.add( (Matrix4D.transl(0, 0, -az/2) * Matrix4D.rotateY(Cmath.PI)) * rect_xy );   //C-D-H-G

         S.add( (Matrix4D.transl( ax/2, 0, 0)                             ) * rect_zy );   //B-C-G-F
         S.add( (Matrix4D.transl(-ax/2, 0, 0) * Matrix4D.rotateZ(Cmath.PI)) * rect_zy );   //D-A-E-H



         return S;
      }
      /*
      public static Mesh tetrahedron(double a, int level = 0)
      {
         return ;
      }
      */



      public static Mesh psphere  (double r, Color c, int level = 0)   //a sphere that is made up of 8 identical pieces, having a specified number of circle slices
      {
         if( r <= 0 )
            throw new ArgumentException("Radius of sphere must be greater than zero");
         if( level < 0 )
            throw new ArgumentException("Level of refinement must be non-negative");

         level += 1;   //magical constant



         Mesh S = new Mesh();
         Vector4D A, B, C;
         double pi = Cmath.PI;



         for( int p = 0; p < level; p++ )  // platforma
         {
            int    np = level - p;                          // broj segmenata na platformi
            double rp = r * Cmath.cos( p * pi/2 / level );  // poluprecnik platforme
            double yp = r * Cmath.sin( p * pi/2 / level );  // relativna visina na kojoj se platforma nalazi

            int    npnext = level - (p+1);                          // broj segmenata na sledecoj platformi
            double rpnext = r * Cmath.cos( (p+1) * pi/2 / level );  // poluprecnik sledece platforme
            double ypnext = r * Cmath.sin( (p+1) * pi/2 / level );  // relativna visina na kojoj se sledeca platforma nalazi

            for( long i = 0; i < np*4; i++ )
            {
               double x1p = rp * Cmath.sin( i*pi/2 / np );
               double z1p = rp * Cmath.cos( i*pi/2 / np );
               double x1pnext = rpnext * (npnext != 0 ? Cmath.sin( (i - (i/np)) * pi/2 / npnext ) : 0);
               double z1pnext = rpnext * (npnext != 0 ? Cmath.cos( (i - (i/np)) * pi/2 / npnext ) : 1);

               double x2p = rp * Cmath.sin( (i+1)*pi/2 / np );
               double z2p = rp * Cmath.cos( (i+1)*pi/2 / np );
               double x2pnext = rpnext * (npnext != 0 ? Cmath.sin( ((i+1) - ((i+1)/np)) * pi/2 / npnext ) : 0);
               double z2pnext = rpnext * (npnext != 0 ? Cmath.cos( ((i+1) - ((i+1)/np)) * pi/2 / npnext ) : 1);


               // gornja hemisfera
               A = new Vector4D(x1p,      yp,     z1p    );
               B = new Vector4D(x2p,      yp,     z2p    );
               C = new Vector4D(x1pnext,  ypnext, z1pnext);
               S.add(new Triangle(A, B, C, c));

               // gornja hemisfera
               A = new Vector4D(x2p,      yp,     z2p    );
               B = new Vector4D(x2pnext,  ypnext, z2pnext);
               C = new Vector4D(x1pnext,  ypnext, z1pnext);
               S.add(new Triangle(A, B, C, c));

               // donja hemisfera
               A = new Vector4D(x1p,     -yp,     z1p    );
               B = new Vector4D(x1pnext, -ypnext, z1pnext);
               C = new Vector4D(x2p,     -yp,     z2p    );
               S.add(new Triangle(A, B, C, c));

               // donja hemisfera
               A = new Vector4D(x1pnext, -ypnext, z1pnext);
               B = new Vector4D(x2pnext, -ypnext, z2pnext);
               C = new Vector4D(x2p,     -yp,     z2p    );
               S.add(new Triangle(A, B, C, c));
            }
         }


         return S;
      }
      public static Mesh uvsphere (double r, Color c, int level = 0)   //a sphere that resembles a party globe (made up from square-ish segments)
      {
         if( r <= 0 )
            throw new ArgumentException("Radius of sphere must be greater than zero");
         if( level < 0 )
            throw new ArgumentException("Level of refinement must be non-negative");

         level += 3;   //magical constant



         Mesh S = new Mesh();
         Vector4D A, B, C, D;
         double pi = Cmath.PI;


         //applies to all levels
         int    stacks = level - 1;             //number of stacks (like a paralel on the globe) that approximate the sphere
         int    slices = level;                 //number of slices to a stack (like orange slices)
         double theta  =   Cmath.PI / stacks;   //stack meridian arc height
         double alpha  = 2*Cmath.PI / slices;   //slice arc length

         //applies to a single level
         Vector4D O_curr;   //center of curr stack
         Vector4D O_next;   //center of next stack



         for( int i = 0; i < stacks; i++ )
         {
            O_curr = Matrix4D.rotateZ(-pi/2 + theta* i   )  *  (r * Vector4D.i);
            O_next = Matrix4D.rotateZ(-pi/2 + theta*(i+1))  *  (r * Vector4D.i);


            for( int k = 0; k < slices; k++ )
            {
               A = Matrix4D.rotateY( alpha* k    ) * O_curr;
               B = Matrix4D.rotateY( alpha*(k+1) ) * O_curr;
               C = Matrix4D.rotateY( alpha*(k+1) ) * O_next;
               D = Matrix4D.rotateY( alpha* k    ) * O_next;

               if( i != 0 )
                  S.add( new Triangle(A, B, C, c) );

               if( i != stacks-1 )
                  S.add( new Triangle(A, C, D, c) );
            }
         }


         return S;
      }
      public static Mesh icosphere(double r, Color c, int level = 0)   //base shape is an icosahedron, that progressively gets its sides split in four equi-triangles
      {
         if( r <= 0 )
            throw new ArgumentException("Radius of sphere must be greater than zero");
         if( level < 0 )
            throw new ArgumentException("Level of refinement must be non-negative");

         level += 1;   //magical constant



         List<Triangle> triangles = new List<Triangle>();

         //creates 12 vertices of a icosahedron
         double p = (1 + Math.Sqrt(5))/2;   //golden ratio phi

         //XZ-plane, counter-clockwise
         Vector4D A = new Vector4D(-1,  0,  p);
         Vector4D B = new Vector4D( 1,  0,  p);
         Vector4D C = new Vector4D( 1,  0, -p);
         Vector4D D = new Vector4D(-1,  0, -p);

         //XY-plane, counter-clockwise
         Vector4D E = new Vector4D(-p, -1,  0);
         Vector4D F = new Vector4D( p, -1,  0);
         Vector4D G = new Vector4D( p,  1,  0);
         Vector4D H = new Vector4D(-p,  1,  0);

         //YZ-plane, counter-clockwise
         Vector4D I = new Vector4D( 0, -p,  1);
         Vector4D J = new Vector4D( 0, -p, -1);
         Vector4D K = new Vector4D( 0,  p, -1);
         Vector4D L = new Vector4D( 0,  p,  1);



         //'normalizes' the icosahedron vectors to lie on a sphere of length r

         //XZ-plane, counter-clockwise
         A = A * r/A.len();
         B = B * r/B.len();
         C = C * r/C.len();
         D = D * r/D.len();

         //XY-plane, counter-clockwise
         E = E * r/E.len();
         F = F * r/F.len();
         G = G * r/G.len();
         H = H * r/H.len();

         //YZ-plane, counter-clockwise
         I = I * r/I.len();
         J = J * r/J.len();
         K = K * r/K.len();
         L = L * r/L.len();




         //create 20 triangles of the icosahedron (three vertices in a face are always! enumerated counter-clockwise)

         //5 faces around point A (left  layer), enumerated counter-clockwise (points -> B L H E I  +  A)
         triangles.Add(new Triangle(B, L, A, c));
         triangles.Add(new Triangle(L, H, A, c));
         triangles.Add(new Triangle(H, E, A, c));
         triangles.Add(new Triangle(E, I, A, c));
         triangles.Add(new Triangle(I, B, A, c));


         //5 faces around point C (right layer), enumerated counter-clockwise (points -> G F J D K  +  C)
         triangles.Add(new Triangle(G, F, C, c));
         triangles.Add(new Triangle(F, J, C, c));
         triangles.Add(new Triangle(J, D, C, c));
         triangles.Add(new Triangle(D, K, C, c));
         triangles.Add(new Triangle(K, G, C, c));


         //5 faces of the middle layer, touching the left  layer, enumerated clockwise (to match the left  layer)
         triangles.Add(new Triangle(B, I, F, c));
         triangles.Add(new Triangle(L, B, G, c));
         triangles.Add(new Triangle(H, L, K, c));
         triangles.Add(new Triangle(E, H, D, c));
         triangles.Add(new Triangle(I, E, J, c));


         //5 faces of the middle layer, touching the right layer, enumerated clockwise (to match the right layer)
         triangles.Add(new Triangle(F, G, B, c));
         triangles.Add(new Triangle(J, F, I, c));
         triangles.Add(new Triangle(D, J, E, c));
         triangles.Add(new Triangle(K, D, H, c));
         triangles.Add(new Triangle(G, K, L, c));




         Triangle T;
         Vector4D P, Q, R, P1, Q1, R1;   //A1, B1, C1 are middle points of edges BC, CA and AB, respectively
         int triangle_cnt;

         for( int i = 0; i < (level-1); i++ )
         {
            triangle_cnt = triangles.Count;
            for( int k = 0; k < triangle_cnt*4; )
            {
               T = triangles[k];
               
               P = T.getv(0);
               Q = T.getv(1);
               R = T.getv(2);

               //middle-point vectors; they don't lie on a sphere yet
               P1 = (Q + R)/2;
               Q1 = (R + P)/2;
               R1 = (P + Q)/2;


               //here is where the middle-point vectors are 'normalised' in length, in order to lie on a sphere
               P1 = P1 * r/P1.len();
               Q1 = Q1 * r/Q1.len();
               R1 = R1 * r/R1.len();


               //inserts the four newly-created triangles, and removes the base triangle
               triangles.Insert(k+1, new Triangle( P, R1, Q1, c));
               triangles.Insert(k+2, new Triangle(R1,  Q, P1, c));
               triangles.Insert(k+3, new Triangle(Q1, P1,  R, c));
               triangles.Insert(k+4, new Triangle(P1, Q1, R1, c));

               triangles.RemoveAt(k);


               k += 4;
            }
         }


         Mesh S = new Mesh(triangles);


         return S;
      }


      public static Mesh cyllinder(double r, double hei, Color c, int level = 0)
      {
         if( r <= 0 )
            throw new ArgumentException("Radius of circle must be greater than zero");
         if( level < 0 )
            throw new ArgumentException("Level of refinement must be non-negative");



         Mesh cyl_base = circle(r, c, level);

         Mesh bot = Matrix4D.transl(0, -hei/2, 0) * cyl_base;   //bottom base of cyllinder (creates a copy of circle that is hei/2 below the original)
         Mesh top = Matrix4D.transl(0,  hei/2, 0) * cyl_base;   //top    base of cyllinder (creates a copy of circle that is hei/2 above the original)


         Mesh S = new Mesh();
         S.add(top);




         Vector4D A, B, C, D;

         List<Triangle> bot_triangles = bot.get_triangles();
         List<Triangle> top_triangles = top.get_triangles();
         for( int i = 0; i < bot.triangle_cnt(); i++ )
         {
            A = bot_triangles[i].getv(0);
            B = bot_triangles[i].getv(1);
            C = top_triangles[i].getv(1);
            D = top_triangles[i].getv(0);

            S.add(new Triangle(A, B, C, c));
            S.add(new Triangle(A, C, D, c));
         }
         bot.clear();
         bot = (Matrix4D.transl(0, -hei/2, 0) * Matrix4D.rotateX(Cmath.PI)) * cyl_base;


         S.add(bot);
         return S;
      }
      public static Mesh cone     (double r, double hei, Color c, int level = 0)
      {
         if( r <= 0 )
            throw new ArgumentException("Radius of circle must be greater than zero");
         if( level < 0 )
            throw new ArgumentException("Level of refinement must be non-negative");



         Mesh cone_base = circle(r, c, level);
         cone_base.transf(Matrix4D.transl(0, -hei/2, 0) * Matrix4D.rotateX(Math.PI));


         Mesh S = new Mesh();
         S.add(cone_base);


         Vector4D A, B, C;
         C = new Vector4D(0, hei/2, 0);

         List<Triangle> triangles = cone_base.get_triangles();
         for( int i = 0; i < cone_base.triangle_cnt(); i++ )
         {
            A = triangles[i].getv(1);
            B = triangles[i].getv(0);

            S.add(new Triangle(A, B, C, c));
         }


         return S;
      }
      #endregion


      #region Getters
      public List<Triangle> get_triangles() => triangles.ConvertAll( Triangle => new Triangle(Triangle) );
      #endregion


      #region Mesh operators
      public static Mesh operator *(Matrix4D M, Mesh S)
         => new Mesh( S.get_triangles().ConvertAll(Triangle => M * Triangle) );

      public Mesh transf(Matrix4D _transf)
      {
         for( int i = 0; i < triangles.Count; i++ )
            triangles[i] = _transf * triangles[i];

         return this;
      }

      public void add(Triangle T)
      {
         if( T != null )
            triangles.Add(T);
      }
      public void add(List<Triangle> L)
      { triangles.AddRange(L); }
      public void add(Mesh S)
      { triangles.AddRange(S.get_triangles()); }

      public bool remove    (Triangle item)             => triangles.Remove(item);
      public int  remove_all(Predicate<Triangle> match) => triangles.RemoveAll(match);
      public void clear()
      { triangles.Clear(); }
      #endregion


      #region Mesh properties
      public Vector3D center()
      {
         Vector3D v = Vector3D.zero;

         for( int i = 0; i < triangles.Count; i++ )
            v += triangles[i].center();

         return v / triangles.Count;
      }
      public int triangle_cnt() => triangles.Count;
      #endregion


      #region Output
      public override string ToString()  => String.Join("", triangles);
      public          string write_all() => String.Join("", triangles.ConvertAll( Triangle => Triangle.write_all() ));
      #endregion


      #region Testing
      public static void test1()
      {
         Console.WriteLine("----------------- <<<<<<<< Mesh test 1");


         Vector4D v11 = new Vector4D(20, 50, 0);
         Vector4D v12 = new Vector4D(0, 50, 0);
         Vector4D v13 = new Vector4D(20, 50, 70);
         Color c1 = Color.Red;
         Triangle T1 = new Triangle(v11, v12, v13, c1);

         Vector4D v21 = new Vector4D(50, 10, 20);
         Vector4D v22 = new Vector4D(0, 50, 30);
         Vector4D v23 = new Vector4D(40, 50, 70);
         Color c2 = Color.Orange;
         Triangle T2 = new Triangle(v21, v22, v23, c2);

         Vector4D v31 = new Vector4D(20, 50, 0);
         Vector4D v32 = new Vector4D(0, 50, 0);
         Vector4D v33 = new Vector4D(20, 50, 70);
         Color c3 = Color.Violet;
         Triangle T3 = new Triangle(v31, v32, v33, c3);

         Mesh S = new Mesh();
         S.add(T1);
         S.add(T2);
         S.add(T3);



         Console.WriteLine("Mesh S        = {0}", S            );
         Console.WriteLine();
         Console.WriteLine("S.write_all() = {0}", S.write_all());

         Console.WriteLine("----------------");

         Matrix4D M = Matrix4D.scale(2, 3, 4);
         Console.WriteLine("Mesh S = {0}", S);
         Console.WriteLine("M = Matrix4D.scale(2, 3, 4) = {0}", M);

         Console.WriteLine("M*S = {0}", M * S);


      }
      #endregion


   }
}
