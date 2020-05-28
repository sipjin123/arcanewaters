using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

[System.Serializable]
public class Perk
{
   // The ID for unassigned perk points
   public const int UNASSIGNED_ID = 0;

   public enum Type
   {
      None = 0,
      Healing = 1,
      Gun = 2,
      Sword = 3,
      Movement = 4,
   }

   // The unique ID of the perk using the points
   public int perkId;

   // The points a player has for this perk
   public int points;

   public Perk () { }

   public Perk (int perkId, int points) {
      this.perkId = perkId;
      this.points = points;
   }

   public static Perk.Type getType (int perkTypeId) {
      return (Perk.Type) perkTypeId;
   }

   public static int getTypeId (Perk.Type perkType) {
      return (int) perkType;
   }

#if IS_SERVER_BUILD
   public Perk (MySqlDataReader dataReader) {
      this.perkId = dataReader.GetInt32("perkId");
      this.points = dataReader.GetInt32("perkPoints");
   }
#endif

}