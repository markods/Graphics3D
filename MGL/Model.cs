using System;

namespace MGL
{
   public class MeshTree
   {
      public Mesh     node;     //mesh node
      public Matrix4D transf;   //transformation matrix

      public MeshTree parent;   //parent of mesh node


      #region Constructors
      public MeshTree()
      {
         node   = null;
         transf = null;

         parent = null;
      }

      public MeshTree(Mesh _node, Matrix4D _transf, MeshTree _parent)
      {
         node   = _node;
         transf = _transf;

         parent = _parent;
      }
      #endregion


      #region Getters
      private Matrix4D get_transf()
      {
         if( parent == null ) return transf;

         Matrix4D parent_transf = parent.get_transf();
         

         if( transf != null && parent_transf != null )
            return parent_transf * transf;
         else if( parent_transf != null )
            return parent_transf;
         else
            return transf;
      }
      public  Matrix4D get_parent_transf() => parent.get_transf();
      public  Matrix4D get_node_transf()   => transf;
      #endregion


      #region Model operators
      public static MeshTree operator *(Matrix4D M, MeshTree ML)
      {
         ML.transf = (ML.transf == null) ? M : M * ML.transf;
         return ML;
      }
      
      public void add_child(Mesh _node = null, Matrix4D _transf = null, MeshTree _parent = null)
      {
         MeshTree ML   = new MeshTree(_node, _transf, _parent);
         ML.parent = this;
      }
      #endregion


   }
}
