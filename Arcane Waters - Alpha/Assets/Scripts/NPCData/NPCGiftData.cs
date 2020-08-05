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

   // The recolor id
   [XmlIgnore]
   public string palettes = "";

   [XmlElement("colors")]
   public string ColorsInt
   {
      get { return palettes; }
      set { palettes = value; }
   }

   // The amount of friendship that offering this gift rewards
   public int rewardedFriendship;

   #endregion

   public NPCGiftData () {

   }

   public NPCGiftData (Item.Category itemCategory, int itemTypeId, string palettes, int rewardedFriendship) {
      this.itemCategory = itemCategory;
      this.itemTypeId = itemTypeId;
      this.palettes = palettes;
      this.rewardedFriendship = rewardedFriendship;
   }

   #region Private Variables

   #endregion
}