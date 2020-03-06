using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class GenericItemRow : MonoBehaviour {
   #region Public Variables

   // Holds the basic info of the item
   public Text itemCategory;
   public Text itemTypeId;
   public Text itemCategoryName;
   public Text itemTypeName;

   // The data of the item
   public string itemData;

   // Holds the icon of the item
   public Image itemIcon;

   // Button for changing selection data after clicking category
   public Button updateCategoryButton;

   // Button for changing selection data after clicking type
   public Button updateTypeButton;

   // The button for deleting data
   public Button deleteButton;

   #endregion

   protected void modifyContent (Item.Category category, int itemTypeID, string data) {
      if (category == Item.Category.None || itemTypeID == 0) {
         itemCategory.text = "(Select)";
         itemTypeId.text = "(Select)";
      } else {
         itemCategory.text = ((int) category).ToString();
         itemTypeId.text = itemTypeID.ToString();
      }

      if (category == Item.Category.Blueprint) {
         itemCategoryName.text = category.ToString();
         int modifiedID = itemTypeID;

         if (data != "") {
            if (data.StartsWith(Blueprint.WEAPON_DATA_PREFIX)) {
               if (itemTypeID.ToString().StartsWith(Blueprint.WEAPON_ID_PREFIX)) {
                  modifiedID = int.Parse(itemTypeID.ToString().Replace(Blueprint.WEAPON_ID_PREFIX, ""));
               }
               WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(modifiedID);

               itemTypeName.text = weaponData.equipmentName;
               string spritePath = weaponData.equipmentIconPath;
               itemIcon.sprite = ImageManager.getSprite(spritePath);
            } else if (data.StartsWith(Blueprint.ARMOR_DATA_PREFIX)) {
               if (itemTypeID.ToString().StartsWith(Blueprint.ARMOR_ID_PREFIX)) {
                  modifiedID = int.Parse(itemTypeID.ToString().Replace(Blueprint.ARMOR_ID_PREFIX, ""));
               }
               ArmorStatData armorData = EquipmentXMLManager.self.getArmorData(modifiedID);

               itemTypeName.text = armorData.equipmentName;
               string spritePath = armorData.equipmentIconPath;
               itemIcon.sprite = ImageManager.getSprite(spritePath);
            }
         }
      } else if (category == Item.Category.Helm) {
         itemCategoryName.text = category.ToString();
         itemTypeName.text = Util.getItemName(category, itemTypeID);

         string spritePath = EquipmentXMLManager.self.getHelmData(itemTypeID).equipmentIconPath;
         itemIcon.sprite = ImageManager.getSprite(spritePath);
      } else if (category == Item.Category.Armor) {
         itemCategoryName.text = category.ToString();

         ArmorStatData armorData = EquipmentXMLManager.self.getArmorData(itemTypeID);
         if (armorData != null) {
            itemTypeName.text = armorData.equipmentName;
            string spritePath = armorData.equipmentIconPath;
            itemIcon.sprite = ImageManager.getSprite(spritePath);
         } else {
            itemTypeName.text = "Error";
         }
      } else if (category == Item.Category.Weapon) {
         itemCategoryName.text = category.ToString();

         WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(itemTypeID);
         if (weaponData != null) {
            itemTypeName.text = weaponData.equipmentName;
            string spritePath = weaponData.equipmentIconPath;
            itemIcon.sprite = ImageManager.getSprite(spritePath);
         } else {
            itemTypeName.text = "Error";
         }
      } else {
         itemCategoryName.text = category.ToString();
         itemTypeName.text = Util.getItemName(category, itemTypeID);
         itemIcon.sprite = Util.getRawSpriteIcon(category, itemTypeID);
      }
   }

   public bool isValidItem () {
      if (itemCategory.text.Length < 1)
         return false;
      if (itemTypeId.text.Length < 1)
         return false;

      try {
         Item newItem = new Item {
            category = (Item.Category) int.Parse(itemCategory.text),
            itemTypeId = int.Parse(itemTypeId.text)
         };
      } catch {
         return false;
      }

      return true;
   }

   public Item getItem () {
      return new Item {
         category = (Item.Category) int.Parse(itemCategory.text),
         itemTypeId = int.Parse(itemTypeId.text)
      };
   }

   public void destroyRow () {
      Destroy(gameObject, .25f);
   }

   #region Private Variables

   #endregion
}
