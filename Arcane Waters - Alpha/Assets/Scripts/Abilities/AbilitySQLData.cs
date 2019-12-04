using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class AbilitySQLData
{
   // The name of the ability
   public string name;

   // The ability id which is used for referencing database and xml
   public int abilityID;

   // Description of the ability
   public string description;

   // The slot where the ability is equipped
   public int equipSlotIndex;

   // The level of the ability
   public int abilityLevel;

   public AbilitySQLData () {

   }

   public static AbilitySQLData TranslateBasicAbility (BasicAbilityData data) {
      AbilitySQLData newSQLData = new AbilitySQLData {
         abilityID = data.itemID,
         abilityLevel = 1,
         description = data.itemDescription,
         equipSlotIndex = -1,
         name = data.itemName
      };

      return newSQLData;
   }

   public static List<AbilitySQLData> TranslateBasicAbility (List<BasicAbilityData> dataList) {
      List<AbilitySQLData> newDataList = new List<AbilitySQLData>();
      foreach (BasicAbilityData data in dataList) {
         AbilitySQLData newSQLData = new AbilitySQLData {
            abilityID = data.itemID,
            abilityLevel = 1,
            description = data.itemDescription,
            equipSlotIndex = -1,
            name = data.itemName
         };
         newDataList.Add(newSQLData);
      }
      return newDataList;
   }
   
   #if IS_SERVER_BUILD

   public AbilitySQLData (MySql.Data.MySqlClient.MySqlDataReader dataReader) {
      this.name = DataUtil.getString(dataReader, "ability_name");
      this.abilityID = DataUtil.getInt(dataReader, "ability_id");
      this.description = DataUtil.getString(dataReader, "ability_description");
      this.equipSlotIndex = DataUtil.getInt(dataReader, "ability_equip_slot");
      this.abilityLevel = DataUtil.getInt(dataReader, "ability_level");
   }

   #endif
}
