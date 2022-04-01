using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Create Composite Animation")]
public class CompositeAnimation : ScriptableObject
{
   #region Public Variables

   // Should the animation loop?
   public bool isLooping;

   // Should the animation maintain the last frame on end?
   public bool keepLastFrame;

   // The set of frames
   public CompositeAnimationFrame[] frames;

   #endregion
}
