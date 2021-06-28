using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SilverManager : NetworkBehaviour
{
   #region Public Variables

   // In Pvp Battles this is the amount of silver that players have at the beginning of the match
   public static int SILVER_PLAYER_INITIAL_AMOUNT = 0;

   // Amount of silver earned after destroying a player's ship
   public static int SILVER_PLAYER_SHIP_KILL_REWARD = 100;

   // Amount of silver earned after killing a sea monster
   public static int SILVER_SEA_MONSTER_KILL_REWARD = 30;

   // Amount of silver earned after destroying a sea structure
   public static int SILVER_SEA_STRUCTURE_KILL_REWARD = 80;

   // Amount of silver earned after destroying an enemy bot ship
   public static int SILVER_BOT_SHIP_KILL_REWARD = 20;

   #endregion
}
