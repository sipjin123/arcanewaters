using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

[System.Serializable]
public class PerkData 
{
   #region Public Variables

   // The unique ID for this perk
   public int perkId;

   // The type of the perk
   public int perkTypeId;

   // The name of the perk
   public string name;

   // The description of the perk
   public string description;

   // The path to the icon for this perk
   public string iconPath;

   // The boost this perk gives, normalized (1 = 100%)
   public float boostFactor;

   #endregion

   public PerkData () { }

#if IS_SERVER_BUILD

   public PerkData (MySqlDataReader dataReader) {
      PerkData xml = Util.xmlLoad<PerkData>(dataReader.GetString("xmlContent"));
      this.perkId = dataReader.GetInt32("xml_id");
      this.perkTypeId = xml.perkTypeId;
      this.name = xml.name;
      this.description = xml.description;
      this.iconPath = xml.iconPath;
      this.boostFactor = xml.boostFactor;
   }

#endif
      
   #region Private Variables

   #endregion
}
