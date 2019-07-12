using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Anim : MonoBehaviour {
   #region Public Variables

   // The types of animations
   public enum Type {  None = 0,
      Idle_East = 1, Idle_North = 2, Idle_South = 3,
      Run_East = 4, Run_North = 5, Run_South = 6,
      Hurt_East = 7,
      Death_East = 8,
      Attack_East = 9,
      Battle_East = 10, Battle_North = 11, Battle_South = 12,
      Block_East = 13,
      Jump_East = 14,
   }

   // The different animation groups
   public enum Group {  None = 0,
      Player = 1,
      Lizard = 2,
      Golem = 3,
   }
      
   #endregion

   public static bool pausesAtEnd (Type animType) {
      switch (animType) {
         case Type.Death_East:
            return true;
         default:
            return false;
      }
   }

   #region Private Variables
      
   #endregion
}
