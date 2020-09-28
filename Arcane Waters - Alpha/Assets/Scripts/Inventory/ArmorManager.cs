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

   // Armor colors
   [SyncVar]
   public string palettes;

   // The current armor data
   public ArmorStatData cachedArmorData;

   #endregion

   public void Update () {
      // If we don't have anything equipped, turn off the animated sprite
      if (armorLayer != null) {
         Util.setAlpha(armorLayer.getRenderer().material, (hasArmor() ? bodySprite.color.a : 0f));
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

      // Set the correct sheet for our gender and armor type
      armorLayer.setType(gender, armorType);

      // Update our Material
      armorLayer.recolor(palettes);

      // Sync up all our animations
      if (_body != null) {
         _body.restartAnimations();
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
   }

   [Server]
   public void updateArmorSyncVars (int armorTypeId, int armorId, string palettes) {
      ArmorStatData armorData = EquipmentXMLManager.self.getArmorData(armorTypeId);

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

      // Send the Info to all clients
      Rpc_EquipArmor(ArmorStatData.serializeArmorStatData(armorData), palettes);
   }

   #region Private Variables

   #endregion
}
