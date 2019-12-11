using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using System.Linq;
using System.Text;

public class AbilityPanel : Panel {
   #region Public Variables

   // The container for the ability rows
   public GameObject abilityRowsContainer;

   // The container for the ability slots
   public GameObject abilitySlotsContainer;

   // The prefab we use for creating ability rows
   public AbilityRow abilityRowPrefab;

   // The prefab we use for creating ability slots
   public AbilitySlot abilitySlotPrefab;

   // The icon of the currently hovered ability
   public Image descriptionIcon;

   // The name of the currently hovered ability
   public Text descriptionName;

   // The description of the currently hovered ability
   public Text descriptionText;

   // Self
   public static AbilityPanel self;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;
   }

   public void receiveDataFromServer (AbilitySQLData[] abilityList) {
      // Clear all the rows and slots
      abilityRowsContainer.DestroyChildren();
      abilitySlotsContainer.DestroyChildren();
      _equippedAbilitySlots.Clear();

      // Create empty ability slots
      for (int i = 0; i < AbilityManager.MAX_EQUIPPED_ABILITIES; i++) {
         AbilitySlot abilitySlot = Instantiate(abilitySlotPrefab, abilitySlotsContainer.transform, false);
         abilitySlot.abilitySlotId = i;
         _equippedAbilitySlots.Add(abilitySlot);
      }

      // Create the ability rows
      foreach (AbilitySQLData ability in abilityList) {
         // Get the base data for the ability
         BasicAbilityData basicAbilityData = AbilityManager.getAbility(ability.abilityID, ability.abilityType);

         // Builds the ability description
         StringBuilder builder = new StringBuilder();
         builder.Append("LVL: ");
         builder.Append(basicAbilityData.levelRequirement);
         builder.Append("\n");
         builder.Append("AP: ");
         builder.Append(basicAbilityData.apChange);
         builder.Append("\n");
         builder.Append(basicAbilityData.itemDescription);
         string description = builder.ToString();

         // Determine if the ability is equipped
         if (ability.equipSlotIndex >= 0) {
            // Initialize the equipped ability slot
            _equippedAbilitySlots[ability.equipSlotIndex].setSlotForAbilityData(ability.abilityID, basicAbilityData, description);
         } else {
            // Instantiate an ability row
            AbilityRow abilityRow = Instantiate(abilityRowPrefab, abilityRowsContainer.transform, false);
            abilityRow.setRowForAbilityData(ability.abilityID, basicAbilityData, description);
         }
      }
   }

   public void displayDescription(Sprite iconSprite, string name, string description) {
      descriptionIcon.sprite = iconSprite;
      descriptionName.text = name;
      descriptionText.text = description;
   }

   public void tryEquipAbility(int abilityId) {
      foreach (AbilitySlot slot in _equippedAbilitySlots) {
         if (slot.isFree()) {
            Global.player.rpc.Cmd_UpdateAbility(abilityId, slot.abilitySlotId);
            break;
         }
      }
   }

   public void unequipAbility(int abilityId) {
      Global.player.rpc.Cmd_UpdateAbility(abilityId, -1);
   }

   #region Private Variables

   // The equipped ability slots
   private List<AbilitySlot> _equippedAbilitySlots = new List<AbilitySlot>();

   #endregion
}
