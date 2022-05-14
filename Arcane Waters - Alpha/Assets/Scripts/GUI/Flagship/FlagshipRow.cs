﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using MapCreationTool;
using System.Linq;

public class FlagshipRow : MonoBehaviour {
   #region Public Variables

   // The icon
   public Image iconImage;

   // The text
   public Text itemName;

   // The Ship associated with this row
   public ShipInfo shipInfo;

   // The button for assigning the flagship
   public Button flagshipButton;

   // The Image that shows up for the Flagship row
   public Image flagshipIcon; 

   // The stat texts
   public Text damageText;
   public Text healthText;
   public Text suppliesText;
   public Text cargoText;
   public Text speedText;
   public Text attackRangeText;
   public Text sailorsText;
   public Text levelRequirementText;

   // The ability row references
   public FlagShipAbilityRow[] abilityTemplates;

   // Indicator showing level requirement does not meet
   public GameObject levelRequirementWarning;

   #endregion

   public void setRowForItem (ShipInfo shipInfo, int level) {
      this.shipInfo = shipInfo;

      // Update the icon name and text
      Sprite[] sprites = ImageManager.getSprites(Ship.getSkinPath(shipInfo.shipType, shipInfo.skinType));
      if (sprites.Length >= 9) {
         iconImage.sprite = sprites[9];
      } else {
         iconImage.sprite = sprites[0];
      }
      itemName.text = "" + shipInfo.shipName;

      // Fill in the stats
      damageText.text = (shipInfo.damage * 100).ToString("f1") + "%";
      healthText.text = "" + shipInfo.maxHealth;
      suppliesText.text = "" + shipInfo.maxFood;
      cargoText.text = "" + shipInfo.cargoMax;
      speedText.text = "" + shipInfo.speed;
      attackRangeText.text = "" + shipInfo.attackRange;

      ShipData shipData = ShipDataManager.self.getShipData(shipInfo.shipXmlId);
      if (shipData != null) {
         levelRequirementWarning.SetActive(level < shipData.shipLevelRequirement);
         levelRequirementText.text = "" + shipData.shipLevelRequirement;
      } else {
         levelRequirementText.text = "";
         levelRequirementWarning.SetActive(false);
      }

      // Disable the flagship button at sea
      flagshipButton.interactable = (Global.player is BodyEntity);

      // Toggle the flagship icon appropriately
      bool isCurrentShip = shipInfo.shipId == FlagshipPanel.playerFlagshipId;
      flagshipIcon.gameObject.SetActive(isCurrentShip);
      flagshipButton.gameObject.SetActive(!isCurrentShip);

      setAbilities(shipInfo.shipAbilities);
   }

   private void setAbilities (ShipAbilityInfo info) {
      int counter = 0;
      List<int> shipAbilities = info.ShipAbilities.ToList();

      // Override abilities that does not have the necessary abilities
      if (info.ShipAbilities.Length < CannonPanel.MAX_ABILITY_COUNT) {
         shipAbilities.Clear();
         foreach (int ability in ShipAbilityInfo.STARTING_ABILITIES) {
            shipAbilities.Add(ability);
         }
      }

      for (int i = 0; i < abilityTemplates.Length; i++) {
         abilityTemplates[i].gameObject.SetActive(false);
      }

      foreach (int abilityId in shipAbilities) {
         ShipAbilityData abilityData = ShipAbilityManager.self.getAbility(abilityId);
         FlagShipAbilityRow flagShipRow = abilityTemplates[counter];
         flagShipRow.gameObject.SetActive(true);
         flagShipRow.iconImage.sprite = ImageManager.getSprite(abilityData.skillIconPath);
         flagShipRow.abilityName = abilityData.abilityName;
         flagShipRow.abilityNameText.text = abilityData.abilityName;
         flagShipRow.abilityInfo = abilityData.abilityDescription;
         flagShipRow.shipAbilityData = abilityData;

         // Process the tooltip hover for abilities
         flagShipRow.abilityNameHolder.GetComponent<ToolTipComponent>().message = abilityData.abilityName + "\n" + abilityData.abilityDescription;
         EventTrigger eventTrigger = flagShipRow.abilityNameHolder.GetComponent<EventTrigger>();
         Utilities.addPointerListener(eventTrigger, EventTriggerType.PointerEnter, (e) => flagShipRow.pointerEnter());
         Utilities.addPointerListener(eventTrigger, EventTriggerType.PointerExit, (e) => flagShipRow.pointerExit());
         counter++;
      }
   }

   public void chooseThisAsFlagship () {
      Global.player.rpc.Cmd_RequestNewFlagship(shipInfo.shipId);
   }

   #region Private Variables

   #endregion
}
