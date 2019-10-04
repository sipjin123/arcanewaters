using UnityEngine;
using System.Collections.Generic;
using System.Xml.Serialization;
using System;

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
   public ColorType color1 = ColorType.None;

   [XmlElement("color1")]
   public int Color1Int
   {
      get { return (int) color1; }
      set { color1 = (ColorType) value; }
   }

   // The secondary recolor id
   [XmlIgnore]
   public ColorType color2 = ColorType.None;

   [XmlElement("color2")]
   public int Color2Int
   {
      get { return (int) color2; }
      set { color2 = (ColorType) value; }
   }

   // The amount of friendship that offering this gift rewards
   public int rewardedFriendship;

   #endregion

   public NPCGiftData () {

   }

   public NPCGiftData (Item.Category itemCategory, int itemTypeId, ColorType color1, ColorType color2, int rewardedFriendship) {
      this.itemCategory = itemCategory;
      this.itemTypeId = itemTypeId;
      this.color1 = color1;
      this.color2 = color2;
      this.rewardedFriendship = rewardedFriendship;
   }

   #region Private Variables

   #endregion
}