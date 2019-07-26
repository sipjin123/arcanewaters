using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MySql.Data.MySqlClient;
using System;

public class OreInfo {
   #region Public Variables

   // Database ID
   public int oreID;

   // Display Name
   public string oreName;
    
   // Type of ore, Iron, Gold, etc
   public OreType oreType;

   // Location where the ore is spawned
   public Area.Type areaType;

   // Coordinates where the ore is spawned
   public Vector2 position;
  
   // If ore should be revealed
   public bool isEnabled;

   // Index id of the ore
   public int oreIndex;

   #endregion

   public OreInfo () { }

#if IS_SERVER_BUILD

   public OreInfo (MySqlDataReader dataReader) {
      Debug.LogError("Received sql reader");

      Debug.LogError("THe x float positon is : " + DataUtil.getFloat(dataReader, "ore_pos_x"));
      Debug.LogError("THe y float positon is : " + DataUtil.getFloat(dataReader, "ore_pos_y"));

      this.oreID = DataUtil.getInt(dataReader, "ore_id");
      this.oreName = DataUtil.getString(dataReader, "ore_name");
      this.oreType = (OreType) Enum.Parse(typeof(OreType), DataUtil.getString(dataReader, "ore_type"), true);
      this.areaType = (Area.Type) Enum.Parse(typeof(Area.Type), DataUtil.getString(dataReader, "map_type"), true);
      this.position = new Vector2(DataUtil.getFloat(dataReader, "ore_pos_x"), DataUtil.getFloat(dataReader, "ore_pos_y"));
      this.isEnabled = DataUtil.getInt(dataReader, "ore_enabled") == 0 ? false : true;
      this.oreIndex = DataUtil.getInt(dataReader, "ore_index");
   }

#endif

   public OreInfo (int oreID, string oreName, string oreType, string areaType, float xpos, float ypos, bool isEnabled, int oreIndex) {
      this.oreID = oreID;
      this.oreName = oreName;
      this.oreType = (OreType) Enum.Parse(typeof(OreType), oreType, true);
      this.areaType = (Area.Type) Enum.Parse(typeof(Area.Type), areaType, true);
      this.position = new Vector2(xpos, ypos);
      this.isEnabled = isEnabled;
      this.oreIndex = oreIndex;
   }
}

public enum OreType
{
   None = 0,
   Iron = 1,
   Silver = 2,
   Gold = 3
}