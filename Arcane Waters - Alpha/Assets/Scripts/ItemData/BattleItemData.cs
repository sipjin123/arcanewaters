using System.Xml.Serialization;
using UnityEngine;

[System.Serializable]
public class BattleItemData 
{
   #region Public Variables

   // Most basic data that each BattleItem holds
   public int itemID;

   // The item name
   public string itemName;

   // The description of the item
   public string itemDescription;

   // The icon path
   public string itemIconPath;

   // The level required for this item
   public int levelRequirement;

   // Effect that will be executed when the ability hits the target, it can be a buff/debuff too
   public string[] hitSpritesPath;

   // Main combat data that this item holds
   public Element elementType;

   //public AudioClip hitAudioClip;
   public string hitAudioClipPath;

   // Player class required to be able to use this item
   public Weapon.Class classRequirement;

   // Used mainly for the item builder, not in game
   public BattleItemType battleItemType;

   #endregion
   
   /// <summary>
   /// Created a new instance with all the basic values required for a new BattleItem
   /// </summary>
   /// <returns> Newly created battle item data, not to be used in game
   /// this data needs to be used to create an ability or a weapon. </returns>
   public static BattleItemData CreateInstance (int itemID, string name, string desc, Element elemType,
       string hitClip, string[] hitSprites, BattleItemType battleItemType, Weapon.Class classRequirement, string itemIcon, int levelRequirement) {
      BattleItemData data = new BattleItemData();

      data.itemName = name;
      data.itemDescription = desc;
      data.itemID = itemID;
      data.levelRequirement = levelRequirement;
      data.classRequirement = classRequirement;

      data.elementType = elemType;

      data.hitAudioClipPath = hitClip;
      data.hitSpritesPath = hitSprites;

      data.battleItemType = battleItemType;
      data.itemIconPath = itemIcon;

      return data;
   }

   /// <summary>
   /// Gets all base battle item data and sets it to this object.
   /// </summary>
   /// <param name="battleItemData"></param>
   protected void setBaseBattleItemData (BattleItemData battleItemData) {
      // Basic battle item data
      itemID = battleItemData.itemID;
      itemName = battleItemData.itemName;
      itemDescription = battleItemData.itemDescription;
      classRequirement = battleItemData.classRequirement;

      itemIconPath = battleItemData.itemIconPath;
      levelRequirement = battleItemData.levelRequirement;
      hitSpritesPath = battleItemData.hitSpritesPath;
      itemIconPath = battleItemData.itemIconPath;
      elementType = battleItemData.elementType;
      hitAudioClipPath = battleItemData.hitAudioClipPath;
      classRequirement = battleItemData.classRequirement;
      battleItemType = battleItemData.battleItemType;
   }
}

public enum BattleItemType
{
   UNDEFINED = 0,
   Ability = 1,
   Weapon = 2
}
