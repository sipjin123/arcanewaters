using UnityEngine;
using System.Collections.Generic;
using System.Xml.Serialization;
using System;

[Serializable]
public class NPCGiftData
{
   #region Public Variables

   // The category of the gift item
   [XmlIgnore]
   public Item.Category itemCategory;

   [XmlElement("itemCategory")]
   public int ItemCategoryInt
   {
      get { return (int) itemCategory; }
      set { itemCategory = (Item.Category) value; }
   }

   // The type id of the gift item
   public int itemTypeId;

   // The primary recolor id
   [XmlIgnore]
   public string palette1 = "";

   [XmlElement("color1")]
   public string Color1Int
   {
      get { return palette1; }
      set { palette1 = value; }
   }

   // The secondary recolor id
   [XmlIgnore]
   public string palette2 = "";

   [XmlElement("color2")]
   public string Color2Int
   {
      get { return palette2; }
      set { palette2 = value; }
   }

   // The amount of friendship that offering this gift rewards
   public int rewardedFriendship;

   #endregion

   public NPCGiftData () {

   }

   public NPCGiftData (Item.Category itemCategory, int itemTypeId, string palette1, string palette2, int rewardedFriendship) {
      this.itemCategory = itemCategory;
      this.itemTypeId = itemTypeId;
      this.palette1 = palette1;
      this.palette2 = palette2;
      this.rewardedFriendship = rewardedFriendship;
   }

   #region Private Variables

   #endregion
}