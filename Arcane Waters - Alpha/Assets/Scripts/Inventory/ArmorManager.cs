using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Mirror;

public class ArmorManager : EquipmentManager {
   #region Public Variables

   // The Layers we're interested in
   public ArmorLayer armorLayer;

   // The unique database inventory id of the armor
   [SyncVar]
   public int equippedArmorId;

   // The Sprite Id
   [SyncVar]
   public int armorType = 0;

   // Equipment sql Id
   [SyncVar]
   public int equipmentDataId = 0;

   // The durability of the armors
   [SyncVar]
   public int armorDurability = 0;

   // Armor colors
   [SyncVar]
   public string palettes;

   // The current armor data
   public ArmorStatData cachedArmorData;

   #endregion

   public void Update () {
      // If we don't have anything equipped, turn off the animated sprite
      if (armorLayer != null) {         
         armorLayer.getSpriteSwap().enabled = hasArmor();
      }
   }

   public bool hasArmor () {
      return (armorType != 0);
   }

   public Armor getArmor () {
      if (cachedArmorData != null) {
         return ArmorStatData.translateDataToArmor(cachedArmorData);
      }

      return new Armor(0, 0, "");
   }

   public void updateSprites () {
      this.updateSprites(this.armorType, this.palettes);
   }

   public void updateSprites (int armorType, string palettes) {
      Gender.Type gender = getGender();

      if (armorType == 0) {
         armorLayer.gameObject.SetActive(false);

         // Sync up all our animations
         if (_body != null) {
            _body.restartAnimations();
         }
      } else {
         // Set the correct sheet for our gender and armor type
         armorLayer.gameObject.SetActive(true);

         // Wait for the coroutine for texture swap to finish before triggering restart animation
         armorLayer.textureSwappedEvent.AddListener(() => {
            // Sync up all our animations
            if (_body != null) {
               _body.restartAnimations();
            }
            armorLayer.textureSwappedEvent.RemoveAllListeners();
         });

         armorLayer.setType(gender, armorType);

         // Update our Material
         armorLayer.recolor(palettes);
      }
   }

   [ClientRpc]
   public void Rpc_EquipArmor (string rawArmorData, string palettes) {
      ArmorStatData armorData = Util.xmlLoad<ArmorStatData>(rawArmorData);
      cachedArmorData = armorData;

      // Update the sprites for the new armor type
      int newType = armorData == null ? 0 : armorData.armorType;

      updateSprites(newType, palettes);

      // Play a sound
      SoundManager.create3dSound("equip_", this.transform.position, 2);

      Global.getUserObjects().armor = new Armor {
         id = equippedArmorId,
         category = Item.Category.Armor,
         itemTypeId = armorType
      };
   }

   public void updateDurability (int newDurability) {
      D.adminLog("Armor durability modified from [" + armorDurability + "] to [" + newDurability + "]", D.ADMIN_LOG_TYPE.Refine);
      armorDurability = newDurability;
   }

   [Server]
   public void updateArmorSyncVars (int armorTypeId, int armorId, string palettes, int durability) {
      ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataBySqlId(armorTypeId);

      // This ensures that there is a palette being assigned for the armor
      if (string.IsNullOrEmpty(palettes)) {
         PaletteToolData paletteData = PaletteSwapManager.self.getPaletteByName(PaletteSwapManager.DEFAULT_ARMOR_PALETTE_NAME);
         if (paletteData != null) {
            palettes = paletteData.paletteName;
         } else {
            D.debug("Non existing palette Name: " + PaletteSwapManager.DEFAULT_ARMOR_PALETTE_NAME);
         }
      }

      if (armorData == null) {
         armorData = ArmorStatData.getDefaultData();
      }
      cachedArmorData = armorData;

      // Assign the armor ID
      this.equippedArmorId = armorId;

      // Set the Sync Vars so they get sent to the clients
      this.equipmentDataId = armorData.sqlId;
      this.armorType = armorData.armorType;
      this.palettes = palettes;
      this.armorDurability = durability;

      // Send the Info to all clients
      Rpc_EquipArmor(ArmorStatData.serializeArmorStatData(armorData), palettes);
   }

   #region Private Variables

   #endregion
}
