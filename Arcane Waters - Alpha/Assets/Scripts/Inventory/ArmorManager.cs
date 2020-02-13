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

   // Armor Type
   [SyncVar]
   public Armor.Type armorType = Armor.Type.None;

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
      return (armorType != Armor.Type.None);
   }

   public Armor getArmor () {
      if (_armor != null) {
         return _armor;
      }

      return new Armor(0, Armor.Type.None, ColorType.None, ColorType.None);
   }

   public void updateSprites () {
      this.updateSprites(this.armorType, this.color1, this.color2);
   }

   public void updateSprites (Armor.Type armorType, ColorType color1, ColorType color2) {
      Gender.Type gender = getGender();

      // Set the correct sheet for our gender and armor type
      armorLayer.setType(gender, armorType);

      // Update our Material
      ColorKey colorKey = new ColorKey(gender, armorType);
      armorLayer.recolor(colorKey, color1, color2);

      // Sync up all our animations
      if (_body != null) {
         _body.restartAnimations();
      }
   }

   [ClientRpc]
   public void Rpc_EquipArmor (Armor newArmor, ColorType color1, ColorType color2) {
      _armor = newArmor;

      // Update the sprites for the new armor type
      Armor.Type newType = newArmor == null ? Armor.Type.None : newArmor.type;
      updateSprites(newType, color1, color2);

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

      _armor = newArmor;

      // Assign the armor ID
      this.equippedArmorId = (newArmor.type == Armor.Type.None) ? 0 : newArmor.id;

      // Set the Sync Vars so they get sent to the clients
      this.armorType = newArmor.type;
      this.color1 = newArmor.color1;
      this.color2 = newArmor.color2;

      // Send the Info to all clients
      Rpc_EquipArmor(newArmor, newArmor.color1, newArmor.color2);
   }

   #region Private Variables

   // The equipped armor, if any
   protected Armor _armor = new Armor(0, Armor.Type.None);

   #endregion
}
