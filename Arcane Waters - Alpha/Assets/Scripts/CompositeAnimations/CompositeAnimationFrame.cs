using System;

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

   // The frame types
   public enum FrameTypes
   {
      // None
      None = 0,

      // Index
      Index = 1,

      // Flip
      Flip = 2,
   }

   #endregion
}
