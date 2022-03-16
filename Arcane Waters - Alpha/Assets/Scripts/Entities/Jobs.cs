using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Jobs {
   #region Public Variables

   // The Types of Jobs
   public enum Type {  None = 0, Farmer = 1, Sailor = 2, Explorer = 3, Trader = 4, Crafter = 5, Miner = 6, Badges = 7 }

   // The user ID for these jobs
   public int userId;

   // The XP amount for each job
   public int farmerXP;
   public int sailorXP;
   public int explorerXP;
   public int traderXP;
   public int crafterXP;
   public int minerXP;
   public int badgesXP;

   #endregion

   public Jobs () {

   }

   public Jobs (int userId) {
      this.userId = userId;
   }

   public int getXP (Type jobType) {
      switch (jobType) {
         case Type.Farmer:
            return farmerXP;
         case Type.Sailor:
            return sailorXP;
         case Type.Explorer:
            return explorerXP;
         case Type.Trader:
            return traderXP;
         case Type.Crafter:
            return crafterXP;
         case Type.Miner:
            return minerXP;
         case Type.Badges:
            return badgesXP;
         default:
            return 0;
      }
   }

   public static string getColumnName (Type jobType) {
      // Get the column name in the database
      switch (jobType) {
         case Type.Crafter:
            return "crafting";
         case Type.Explorer:
            return "exploring";
         case Type.Farmer:
            return "farming";
         case Type.Miner:
            return "mining";
         case Type.Sailor:
            return "sailing";
         case Type.Trader:
            return "trading";
         case Type.Badges:
            return "badges";
         default:
            return "none";
      }
   }

   #region Private Variables
      
   #endregion
}
