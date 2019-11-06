using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AnimUtil : MonoBehaviour {
   #region Public Variables

   // The string that identifies our Idle animations
   public static string IDLE = "Idle";

   // The string that identifies our Run animations
   public static string RUN = "Run";

   // The string that identifies our Attack animations
   public static string ATTACK = "Attack";

   // The string that identifies our Hurt animations
   public static string HURT = "Hurt";

   // The string that identifies our Death animations
   public static string DEATH = "Death";

   // The string that identifies our Dead animations
   public static string DEAD = "Dead";

   // The default FPS scale for animations (2.7 FPS)
   public const float DEFAULT_ANIM_FPS_SCALE = .27f;

   // The FPS scale at which we play enemy animations
   public const float ENEMY_FPS_SCALE = .40f;

   // The FPS scale at which we play ship animations
   // We want 2.5 frames a second to sync up with the 200ms frame lengths of the Tiled animations
   public const float SHIP_ANIM_FPS_SCALE = .25f;

   // The FPS scale at which we play sea monster animations
   public const float SEA_MONSTER_ANIM_FPS_SCALE = .45f;

   // The FPS scale at which we play character animations
   public const float BODY_ANIM_FPS_SCALE = .54f;

   // The FPS scale at which we play run animations
   public const float RUN_ANIM_FPS_SCALE = .72f;

   // Battle animation codes
   public static string BATTLE_EAST = "Battle_East";
   public static string BATTLE_SOUTH = "Battle_South";
   public static string RUN_EAST = "Run_East";
   public static string DEAD_EAST = "Dead_East";
   public static string DEAD_IDLE = "Dead_Idle";
   public static string JUMP_EAST = "Jump_East";
   public static string ATTACK_EAST = "Attack_East";
   public static string HURT_EAST = "Hurt_East";
   public static string BLOCK_EAST = "Block_East";

   #endregion

   public static AnimInfo getInfo (Anim.Group animGroup, Anim.Type animType) {
      switch (animGroup) {
         case Anim.Group.Player:
            switch (animType) {
               case Anim.Type.Idle_East:
                  return new AnimInfo(animType, 0, 3);
               case Anim.Type.Idle_North:
                  return new AnimInfo(animType, 4, 7);
               case Anim.Type.Idle_South:
                  return new AnimInfo(animType, 8, 11);

               case Anim.Type.Run_East:
                  return new AnimInfo(animType, 12, 17);
               case Anim.Type.Run_North:
                  return new AnimInfo(animType, 18, 23);
               case Anim.Type.Run_South:
                  return new AnimInfo(animType, 24, 29);

               case Anim.Type.Hurt_East:
                  return new AnimInfo(animType, 30, 30);

               case Anim.Type.Death_East:
                  return new AnimInfo(animType, 31, 31);

               case Anim.Type.Attack_East:
                  return new AnimInfo(animType, 32, 34);

               case Anim.Type.Battle_East:
                  return new AnimInfo(animType, 35, 38);
               case Anim.Type.Battle_North:
                  return new AnimInfo(animType, 39, 42);
               case Anim.Type.Battle_South:
                  return new AnimInfo(animType, 43, 46);

               case Anim.Type.Block_East:
                  return new AnimInfo(animType, 47, 47);

               case Anim.Type.Jump_East:
                  return new AnimInfo(animType, 14, 14);
            }
            break;

         case Anim.Group.PlayerShip:
            switch (animType) {
               case Anim.Type.Attack_South:
                  return new AnimInfo(animType, 0, 1);
               case Anim.Type.Attack_East:
                  return new AnimInfo(animType, 4, 5);
               case Anim.Type.Attack_North:
                  return new AnimInfo(animType, 8, 9);

               case Anim.Type.Idle_South:
                  return new AnimInfo(animType, 0, 1);
               case Anim.Type.Idle_East:
                  return new AnimInfo(animType, 4, 5);
               case Anim.Type.Idle_North:
                  return new AnimInfo(animType, 8, 9);

               case Anim.Type.Battle_South:
                  return new AnimInfo(animType, 0, 1);
               case Anim.Type.Battle_East:
                  return new AnimInfo(animType, 4, 5);
               case Anim.Type.Battle_North:
                  return new AnimInfo(animType, 8, 9);

               case Anim.Type.Run_South:
                  return new AnimInfo(animType, 0, 1);
               case Anim.Type.Run_East:
                  return new AnimInfo(animType, 4, 5);
               case Anim.Type.Run_North:
                  return new AnimInfo(animType, 8, 9);
            }
            break;

         case Anim.Group.Lizard:
            switch (animType) {
               case Anim.Type.Idle_East:
                  return new AnimInfo(animType, 0, 3);
               case Anim.Type.Idle_North:
                  return new AnimInfo(animType, 4, 7);
               case Anim.Type.Idle_South:
                  return new AnimInfo(animType, 8, 11);

               case Anim.Type.Run_East:
                  return new AnimInfo(animType, 12, 15);
               case Anim.Type.Run_North:
                  return new AnimInfo(animType, 16, 19);
               case Anim.Type.Run_South:
                  return new AnimInfo(animType, 20, 23);

               case Anim.Type.Attack_East:
                  return new AnimInfo(animType, 24, 31);

               case Anim.Type.Hurt_East:
                  return new AnimInfo(animType, 32, 32);

               case Anim.Type.Death_East:
                  return new AnimInfo(animType, 33, 38);

               case Anim.Type.Battle_East:
                  return new AnimInfo(animType, 0, 3);
               case Anim.Type.Battle_North:
                  return new AnimInfo(animType, 4, 7);
               case Anim.Type.Battle_South:
                  return new AnimInfo(animType, 8, 11);

               case Anim.Type.Block_East:
                  return new AnimInfo(animType, 0, 0);

               case Anim.Type.Jump_East:
                  return new AnimInfo(animType, 15, 15);
            }
            break;

            case Anim.Group.Golem:
               switch (animType) {
                  case Anim.Type.Idle_East:
                     return new AnimInfo(animType, 0, 2);
                  case Anim.Type.Idle_North:
                     return new AnimInfo(animType, 3, 5);
                  case Anim.Type.Idle_South:
                     return new AnimInfo(animType, 6, 8);

                  case Anim.Type.Run_East:
                     return new AnimInfo(animType, 9, 14);
                  case Anim.Type.Run_North:
                     return new AnimInfo(animType, 15, 20);
                  case Anim.Type.Run_South:
                     return new AnimInfo(animType, 21, 26);

                  case Anim.Type.Attack_East:
                     return new AnimInfo(animType, 27, 34);

                  case Anim.Type.Hurt_East:
                     return new AnimInfo(animType, 35, 35);

                  case Anim.Type.Death_East:
                     return new AnimInfo(animType, 36, 39);

                  case Anim.Type.Battle_East:
                     return new AnimInfo(animType, 0, 2);
                  case Anim.Type.Battle_North:
                     return new AnimInfo(animType, 3, 5);
                  case Anim.Type.Battle_South:
                     return new AnimInfo(animType, 6, 8);

                  case Anim.Type.Block_East:
                     return new AnimInfo(animType, 0, 0);

                  case Anim.Type.Jump_East:
                     return new AnimInfo(animType, 11, 11);
               }
            break;

         case Anim.Group.SeaMonster:
            switch (animType) {
               case Anim.Type.Idle_East:
                  return new AnimInfo(animType, 0, 3);
               case Anim.Type.Idle_North:
                  return new AnimInfo(animType, 4, 7);
               case Anim.Type.Idle_South:
                  return new AnimInfo(animType, 8, 11);

               case Anim.Type.Run_East:
                  return new AnimInfo(animType, 12, 15);
               case Anim.Type.Run_North:
                  return new AnimInfo(animType, 16, 19);
               case Anim.Type.Run_South:
                  return new AnimInfo(animType, 20, 23);

               case Anim.Type.Attack_East:
                  return new AnimInfo(animType, 24, 27);
               case Anim.Type.Attack_North:
                  return new AnimInfo(animType, 28, 31);
               case Anim.Type.Attack_South:
                  return new AnimInfo(animType, 32, 35);

               case Anim.Type.Death_East:
                  return new AnimInfo(animType, 39, 42);
            }
            break;
            
         case Anim.Group.ReefGiant:
            switch (animType) {
               case Anim.Type.Idle_East:
                  return new AnimInfo(animType, 0, 3);
               case Anim.Type.Idle_North:
                  return new AnimInfo(animType, 4, 7);
               case Anim.Type.Idle_South:
                  return new AnimInfo(animType, 8, 11);

               case Anim.Type.Run_East:
                  return new AnimInfo(animType, 12, 17);
               case Anim.Type.Run_North:
                  return new AnimInfo(animType, 18, 23);
               case Anim.Type.Run_South:
                  return new AnimInfo(animType, 24, 29);

               case Anim.Type.Attack_East:
                  return new AnimInfo(animType, 30, 35);
               case Anim.Type.Attack_North:
                  return new AnimInfo(animType, 36, 41);
               case Anim.Type.Attack_South:
                  return new AnimInfo(animType, 42, 47);

               case Anim.Type.Death_East:
                  return new AnimInfo(animType, 51, 59);
            }
            break;

         case Anim.Group.Tentacle:
            switch (animType) {
               case Anim.Type.Idle_East:
                  return new AnimInfo(animType, 0, 3);
               case Anim.Type.Idle_North:
                  return new AnimInfo(animType, 0, 3);
               case Anim.Type.Idle_South:
                  return new AnimInfo(animType, 0, 3);

               case Anim.Type.Run_East:
                  return new AnimInfo(animType, 4, 7);
               case Anim.Type.Run_North:
                  return new AnimInfo(animType, 4, 7);
               case Anim.Type.Run_South:
                  return new AnimInfo(animType, 4, 7);

               case Anim.Type.Attack_East:
                  return new AnimInfo(animType, 8, 15);
               case Anim.Type.Attack_North:
                  return new AnimInfo(animType, 8, 15);
               case Anim.Type.Attack_South:
                  return new AnimInfo(animType, 8, 15);

               case Anim.Type.Death_East:
                  return new AnimInfo(animType, 16, 22);
            }
            break;

         case Anim.Group.Horror:
            switch (animType) {
               case Anim.Type.Death_East:
                  return new AnimInfo(animType, 4, 13);
               default:
                  return new AnimInfo(animType, 0, 3);
            }
      }

      D.warning("Couldn't find animation info for group: " + animGroup + " and type: " + animType);
      return new AnimInfo(animType, 0, 0);
   }

   #region Private Variables

   #endregion
}
