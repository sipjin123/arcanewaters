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
      Mining = 15,
      Attack_North = 16,
      Attack_South = 17,
      Ready_Attack = 18,
      Finish_Attack = 19,
      Interact_East = 20,
      Interact_North = 21,
      Interact_South = 22,
      Play_Once = 23,
      Punch = 24,
      NC_Jump_East = 25,
      NC_Jump_South = 26,
      NC_Jump_North = 27,
      Pet_East = 28,
      Pet_North = 29,
      Pet_South = 30,
      Throw_Projectile = 31,
      Toast = 32,
      SpecialAnimation = 33
   }

   // The different animation groups
   public enum Group {  None = 0,
      Player = 1,
      Lizard = 2,
      Golem = 3,
      SeaMonster = 4,
      ReefGiant = 5,
      Tentacle = 6,
      Horror = 7,
      PlayerShip = 8,
      Golem_Boss = 9,
      Lizard_Boss = 10,
      Pirate = 11,
      Wisp = 12,
      Snake = 13,
      Shroom = 14,
      Elemental = 15
   }
      
   #endregion

   public static bool pausesAtEnd (Type animType) {
      switch (animType) {
         case Type.Death_East:
         case Type.Attack_East:
         case Type.Attack_North:
         case Type.Attack_South:
         case Type.Ready_Attack:
         case Type.Finish_Attack:
         case Type.Play_Once:
         case Type.Punch:
         case Type.SpecialAnimation:
            return true;
         default:
            return false;
      }
   }

   #region Private Variables
      
   #endregion
}
