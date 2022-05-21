﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class NPCPanelQuestObjectiveCell : MonoBehaviour
{
   #region Public Variables

   // The icon representing the objective
   public Image icon;

   // The text displaying the progress of the quest
   public Text progressText;

   // The tooltip when hovering the icon
   public Tooltipped tooltip;

   // The color for completed objectives
   public Color completedObjectiveColor;

   // The color for incompleted objectives
   public Color incompletedObjectivesColor;

   // Icon that determines if this item is a blueprint
   public GameObject blueprintIcon;

   #endregion

   public void updateCellContent (Item item, int requirement, int current) {
      switch (item.category) {
         case Item.Category.Armor:
            ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataBySqlId(item.itemTypeId);
            if (armorData != null) {
               icon.sprite = ImageManager.getSprite(armorData.equipmentIconPath);
            }
            break;
         case Item.Category.Weapon:
            WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(item.itemTypeId);
            if (weaponData != null) {
               icon.sprite = ImageManager.getSprite(weaponData.equipmentIconPath);
            }
            break;
         case Item.Category.Hats:
            HatStatData hatData = EquipmentXMLManager.self.getHatData(item.itemTypeId);
            if (hatData != null) {
               icon.sprite = ImageManager.getSprite(hatData.equipmentIconPath);
            }
            break;
         case Item.Category.Quest_Item:
            QuestItem questItem = EquipmentXMLManager.self.getQuestItemById(item.itemTypeId);
            if (questItem != null) {
               icon.sprite = ImageManager.getSprite(questItem.iconPath);
            }
            break;
         case Item.Category.CraftingIngredients:
            CraftingIngredients.Type categoryType = (CraftingIngredients.Type) item.itemTypeId;
            Sprite newSprite = ImageManager.getSprite(CraftingIngredients.getIconPath(categoryType));
            icon.sprite = newSprite;
            break;
         case Item.Category.Blueprint:
            blueprintIcon.SetActive(true);
            CraftableItemRequirements craftingData = CraftingManager.self.getCraftableData(item.itemTypeId);
            if (craftingData == null) {
               D.debug("Failed to fetch Crafting Data: " + item.itemTypeId);
            } else {
               Sprite blueprintSprite = ImageManager.getSprite(EquipmentXMLManager.self.getItemIconPath(craftingData.resultItem));
               icon.sprite = blueprintSprite;
            }
            break;
         case Item.Category.Crop:
            if (CropsDataManager.self.tryGetCropData(item.itemTypeId, out CropsData cropData)) {
               icon.sprite = ImageManager.getSprite("Cargo/" + (Crop.Type)cropData.cropsType);
            }
            break;
         default:
            D.debug("Invalid Item Category: " + item.category);
            icon.sprite = null;
            break;
      }
      progressText.text = current + " / " + requirement;
      progressText.color = current >= requirement ? completedObjectiveColor : incompletedObjectivesColor;
   }
   
   #region Private Variables

   #endregion
}
