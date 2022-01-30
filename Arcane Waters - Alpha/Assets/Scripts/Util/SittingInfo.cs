using UnityEngine;

public struct SittingInfo
{
   #region Public Variables

   // Is the player sitting down?
   public bool isSitting;

   // The direction the player is facing while sitting down
   public Direction sittingDirection;

   // The position where the player is sitting
   public Vector3 sittingPosition;

   // Stores the position where the player stood before sitting down
   public Vector3 positionBeforeSitting;

   // The position of the chair the player is sitting on
   public Vector3 chairPosition;

   // The type of the chair
   public ChairClickable.ChairType chairType;

   #endregion
}
