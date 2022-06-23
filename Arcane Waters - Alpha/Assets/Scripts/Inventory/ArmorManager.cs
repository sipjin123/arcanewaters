﻿using UnityEngine;
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

      if (_body != null && armorLayer != null) {
         if (_body.spriteOverrideId.Length > 0 && _body.spriteOverrideType > 0) {
            armorLayer.gameObject.SetActive(false);
         }
      }
   }

   [TargetRpc]
   public void Target_ReceiveEquipArmor (NetworkConnection connection, int newArmorId, int newArmorSqlId, int newArmorType, string rawArmorData, string palettes, bool equipOnStart) {
      ArmorStatData armorData = Util.xmlLoad<ArmorStatData>(rawArmorData);
      cachedArmorData = armorData;

      // Update the sprites for the new armor type
      updateSprites(newArmorType, palettes);

      // Play a sound
      if (!equipOnStart) {
         SoundEffectManager.self.playEquipSfx();
      }

      D.adminLog("Equipped armor" + " SQL: {" + armorData.sqlId +
         "} Name: {" + armorData.equipmentName +
         "} Class: {" + armorData.armorType + "}", D.ADMIN_LOG_TYPE.Equipment);

      Global.getUserObjects().armor = new Armor {
         id = newArmorId,
         category = Item.Category.Armor,
         itemTypeId = newArmorSqlId
      };
   }

   [ClientRpc]
   public void Rpc_EquipArmor (int newArmorId, int newArmorSqlId, int newArmorType, string rawArmorData, string palettes) {
      ArmorStatData armorData = Util.xmlLoad<ArmorStatData>(rawArmorData);
      cachedArmorData = armorData;

      // Update the sprites for the new armor type
      updateSprites(newArmorType, palettes);
   }

   public void updateDurability (int newDurability) {
      D.adminLog("Armor durability modified from [" + armorDurability + "] to [" + newDurability + "]", D.ADMIN_LOG_TYPE.Refine);
      armorDurability = newDurability;
   }

   [Server]
   public void updateArmorSyncVars (int armorTypeId, int armorId, string palettes, int durability, bool equipOnStart = false) {
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
         if (armorTypeId != 0) {
            D.debug("Armor data is null for {" + armorTypeId + "}");
         }
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

      if (!tryGetConnectionToClient(out NetworkConnection connection)) {
         D.debug("Connection to client was null!");
         return;
      }

      Target_ReceiveEquipArmor(connection, armorId, armorData.sqlId, armorData.armorType, ArmorStatData.serializeArmorStatData(armorData), palettes, equipOnStart);
      Rpc_EquipArmor(armorId, armorData.sqlId, armorData.armorType, ArmorStatData.serializeArmorStatData(armorData), palettes);
   }

   #region Private Variables

   #endregion
}
