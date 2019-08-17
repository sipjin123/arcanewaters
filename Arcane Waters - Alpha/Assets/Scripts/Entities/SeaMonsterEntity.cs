using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SeaMonsterEntity : SeaEntity
{
   #region Public Variables

   // The total tentacles left before this unit dies
   [SyncVar]
   public int tentaclesLeft;

   // Animator
   public Animator animator;

   // Tentacle Animation
   public enum TentacleAnimType
   {
      Idle,
      Attack,
      Die,
   }


   #endregion

   #region Private Variables

   #endregion
}
