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
   public enum Type
   {
      None = 0,
      Healing = 1,
      Gun = 2,
      Sword = 3,
   }

   // The type of this perk
   public int perkTypeId;

   // The points a player has for this perk
   public int points;

   public Perk () { }

   public Perk (Perk.Type type, int points) {
      this.perkTypeId = getId(type);
      this.points = points;
   }

   public static Perk.Type getType (int perkId) {
      return (Perk.Type) perkId;
   }

   public static int getId (Perk.Type perkType) {
      return (int) perkType;
   }

#if IS_SERVER_BUILD
   public Perk (MySqlDataReader dataReader) {
      this.perkTypeId = dataReader.GetInt32("perkTypeId");
      this.points = dataReader.GetInt32("perkPoints");
   }
#endif

}