using System;
using UnityEngine;

[Serializable]
public class CompositeAnimationFrame
{
   #region Public Variables

   // The index of the sprite used for the frame
   public int index;

   // The type of the frame
   public FrameTypes type;

   // The duration of the frame (seconds)
   public float duration = 0.25f;

   // The offset to apply the animated sprite
   public Vector3 offset = Vector3.zero;

   // The frame types
   public enum FrameTypes
   {
      // None
      None = 0,

      // Index
      Index = 1,

      // Flip
      Flip = 2,

      // Offset
      Offset = 3
   }

   #endregion
}
