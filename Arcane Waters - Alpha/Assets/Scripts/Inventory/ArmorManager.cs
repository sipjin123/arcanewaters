using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Mirror;

public class ArmorManager : EquipmentManager {
   #region Public Variables

   // The Layers we're interested in
   public ArmorLayer armorLayer;

   [SyncVar]
   public int equippedArmorId;

   // The type of material the armor is using
   [SyncVar]
   public MaterialType materialType;

   // Armor Type
   [SyncVar]
   public int armorType = 0;

   // Armor colors
   [SyncVar]
   public ColorType color1;
   [SyncVar]
   public ColorType color2;

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
      if (_armor != null) {
         return _armor;
      }

      return new Armor(0, 0, ColorType.None, ColorType.None);
   }

   public void updateSprites () {
      this.updateSprites(this.armorType, this.color1, this.color2);
   }

   public void updateSprites (int armorType, ColorType color1, ColorType color2, MaterialType overrideMaterialType = MaterialType.None) {
      Gender.Type gender = getGender();

      // Set the correct sheet for our gender and armor type
      armorLayer.setType(gender, armorType);

      // Update our Material
      ColorKey colorKey = new ColorKey(gender, "armor_" + armorType.ToString());
      armorLayer.recolor(colorKey, color1, color2, overrideMaterialType != MaterialType.None ? overrideMaterialType : materialType);

      // Sync up all our animations
      if (_body != null) {
         _body.restartAnimations();
      }
   }

   [ClientRpc]
   public void Rpc_EquipArmor (string rawArmorData, ColorType color1, ColorType color2) {
      ArmorStatData armorData = Util.xmlLoad<ArmorStatData>(rawArmorData);
      Armor newArmor = ArmorStatData.translateDataToArmor(armorData);
      _armor = newArmor;

      // Update the sprites for the new armor type
      int newType = newArmor == null ? 0 : newArmor.itemTypeId;
      updateSprites(newType, color1, color2, newArmor.materialType);

      // Play a sound
      SoundManager.create3dSound("equip_", this.transform.position, 2);
      
      // Check if we completed a tutorial step
      TutorialData tutorialData = TutorialManager.self.currentTutorialData();
      if (_body != null && tutorialData != null) {
         if (_body.isLocalPlayer && tutorialData.actionType == ActionType.EquipArmor) {
            _body.Cmd_CompletedTutorialStep(TutorialManager.currentStep);
         }
      }
   }

   [Server]
   public void updateArmorSyncVars (Armor newArmor) {
      if (newArmor == null) {
         D.debug("Armor is null");
         return;
      }

      if (newArmor.itemTypeId == 0) {
         // No armor to equip
         return;
      }

      ArmorStatData armorData = EquipmentXMLManager.self.getArmorData(newArmor.itemTypeId);
      if (armorData != null) {
         armorData.itemSqlId = newArmor.id;
         newArmor.materialType = armorData.materialType;
         this.materialType = armorData.materialType;
      }

      _armor = newArmor;

      // Assign the armor ID
      this.equippedArmorId = (newArmor.itemTypeId == 0) ? 0 : newArmor.id;

      // Set the Sync Vars so they get sent to the clients
      this.armorType = armorData.armorType;
      this.color1 = newArmor.color1;
      this.color2 = newArmor.color2;

      // Send the Info to all clients
      Rpc_EquipArmor(ArmorStatData.serializeArmorStatData(armorData), newArmor.color1, newArmor.color2);
   }

   #region Private Variables

   // The equipped armor, if any
   protected Armor _armor = new Armor(0, 0);

   #endregion
}
