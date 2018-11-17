using System;
using System.Drawing;


struct ColorARGB
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
    public void  GetColor(ref byte a, ref byte r, ref byte g, ref byte b)
    {
        a = A;  r = R;  g = G;  b = B;
    }
}


namespace MGL
{
   static class Ccolor
   {
      static Color Brighten(Color c, double factor)
      {
         if( factor < 0 )
            throw new ArgumentException("Cannot brighten negative times");
         
         int a = c.R;
         int r = (int) Cmath.interv_conf(c.R * factor, 0, 255);
         int g = (int) Cmath.interv_conf(c.G * factor, 0, 255);
         int b = (int) Cmath.interv_conf(c.B * factor, 0, 255);

         return Color.FromArgb( a, r, g, b );
      }


   }
}
