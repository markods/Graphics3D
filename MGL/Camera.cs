using System;
using System.Collections.Generic;
using System.Drawing;

namespace MGL
{
   public class Camera
   {
      private int draw_wid, draw_hei;   //prostor za crtanje
      private double[][] zbuf;          //Z-buffer



      public double Zvp;    //Z koordinata VP  (      viewporta     ) u koordinatnom sistemu posmatraca
      public double Zncp;   //Z koordinata NCP (near  clipping plane)                -||-
      public double Zfcp;   //Z koordinata FCP (far       -||-      )                -||-
      
      public double Ytcp;   //Y koordinata TCP (top       -||-      )                -||-
      public double Ybcp;   //Y koordinata BCP (bottom    -||-      )                -||-
      
      public double Xlcp;   //X koordinata LCP (left      -||-      )                -||-
      public double Xrcp;   //X koordinata RCP (right     -||-      )                -||-
      


      private Matrix4D pp;   //perspective projection matrix
      private Matrix4D vt;   //viewport transformation matrix




      private List<Mesh> scene;   //skup svih mesh-ova na sceni
      

      public enum background : int { none, grid, lines, all };
      public enum surface    : int { none, back, front, both };
      public enum wireframe  : int { none, back, front, both };
      public enum visibility : int { unordered, back2front, zbuffer };
      public enum shading    : int { none, fix, flat };


      //gets the (distinct) values of enums as a list, then counts the number of elements in that list
      public readonly int background_mode_cnt = Enum.GetValues(typeof(background)).Length;
      public readonly int surface_mode_cnt    = Enum.GetValues(typeof(surface   )).Length;
      public readonly int wireframe_mode_cnt  = Enum.GetValues(typeof(wireframe )).Length;
      public readonly int visibility_mode_cnt = Enum.GetValues(typeof(visibility)).Length;
      public readonly int shading_mode_cnt    = Enum.GetValues(typeof(shading   )).Length;

      public int background_mode = (int) background.all;
      public int surface_mode    = (int) surface.both;
      public int wireframe_mode  = (int) wireframe.both;
      public int visibility_mode = (int) visibility.back2front;
      public int shading_mode    = (int) shading.fix;


      public bool clip_x = true;
      public bool clip_y = true;

      public bool draw_normals = false;


      
      #region Constructors
      public Camera(Graphics g)
      {
         
      }
      #endregion

   }
}
