using System.Drawing;
using System.Runtime.CompilerServices;

namespace MGL
{
   public class Camera
   {
      private double frust_wid = 2;
      private double frust_hei = 2;
      private double frust_dep = 1;
      private double fov;


      public Camera()
      {
         fov = Cmath.PI/2;
      }



      //viewing frustrum culling matrix
      //
      //      X           Y              Z             W
      //[  2n/(r-l),      0,        (r+l)/(r-l),       0       ]
      //[     0,       2n/(t-b),    (t+b)/(t-b),       0       ]
      //[     0,          0,       -(f+n)/(f-n),   -2fn/(f-n)  ]
      //[     0,          0,            -1,            0       ]

      //===============================================







      private const int reg_n = 2 << 5;   //near   regional code
      private const int reg_f = 2 << 4;   //far        -||-
      private const int reg_t = 2 << 3;   //top        -||-
      private const int reg_b = 2 << 2;   //bottom     -||-
      private const int reg_l = 2 << 1;   //left       -||-
      private const int reg_r = 2 << 0;   //right      -||-

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      private void ClipLine(ref Vector3D v0, ref Vector3D v1)
      {



         Vector3D[] v = new Vector3D[2];
         int[] v_reg = new int[2];   //vectors' regional code

         v[0] = v0;
         v[1] = v1;

         
         for( int i = 0; i < 2; i++ )   //setting regional codes
         {
            v_reg[i] = 0;

            v_reg[i] &= (v[i].getz() > frust_dep  ) ? reg_n : (v[i].getz() < 0          ) ? reg_f : ~0;
            v_reg[i] &= (v[i].gety() > frust_hei/2) ? reg_t : (v[i].getx() < frust_hei/2) ? reg_b : ~0;
            v_reg[i] &= (v[i].getx() > frust_wid/2) ? reg_r : (v[i].getx() < frust_wid/2) ? reg_l : ~0;
         }

         
         if( (v_reg[0] & v_reg[1]) == 0 ) return;   //apparently & doesn't take prescedence over ==
         

         Vector3D[] normals = new Vector3D[6];
         normals[5] =  Vector3D.k;   //normal on near   qube face
         normals[4] =  Vector3D.k;   //normal on far      -||-
         normals[3] =  Vector3D.j;   //normal on top      -||-
         normals[2] =  Vector3D.j;   //normal on bottom   -||-
         normals[1] =  Vector3D.i;   //normal on left     -||-
         normals[0] =  Vector3D.i;   //normal on right    -||-

         /*
         for( int i = 0; i < 2; i++ )
         {
            for( int k = 5; k >= 0 || ; k-- )
            {
               if( (v_reg[i] & 2 << k) != 0 )
            }

            if     ( (v_reg[i] & reg_n) != 0 )
            {
               
            }
            else if( (v_reg[i] & reg_f) != 0 ) { }
            else if( (v_reg[i] & reg_l) != 0 ) { }
            else if( (v_reg[i] & reg_r) != 0 ) { }
            else if( (v_reg[i] & reg_t) != 0 ) { }
            else if( (v_reg[i] & reg_b) != 0 ) { }
         }
         */


      }



   }
}
