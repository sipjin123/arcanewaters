using UnityEngine;

namespace MapCreationTool
{
   public interface IHighlightable
   {
      void setHighlight (bool hovered, bool selected, bool deleting);
   }
}
