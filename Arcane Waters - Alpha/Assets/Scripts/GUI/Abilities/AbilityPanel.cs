﻿using UnityEngine;
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

   // Data Display
   public Text panelLevel, panelAP;

   // Cached info of the abilities
   public List<BasicAbilityData> cachedAbilityList;

   // Canvas reference
   public Canvas canvas;

   // The zone where grabbed abilities can be dropped
   public ItemDropZone inventoryDropZone;
   public List<ItemDropZone> equipmentDropZone;

   // Holds the canvas blocker 
   public GameObject canvasBlocker;

   // The scroller holding the abilities
   public ScrollRect scroller;

   #endregion

   public override void Awake () {
      canvas = transform.parent.GetComponent<Canvas>();
      base.Awake();
      self = this;
   }

   public void receiveDataFromServer (AbilitySQLData[] abilityList) {
      canvasBlocker.SetActive(false);

      // Clear all the rows and slots
      abilityRowsContainer.DestroyChildren();
      abilitySlotsContainer.DestroyChildren();
      _equippedAbilitySlots.Clear();
      cachedAbilityList = new List<BasicAbilityData>();
      equipmentDropZone = new List<ItemDropZone>();

      // Create empty ability slots
      for (int i = 0; i < AbilityManager.MAX_EQUIPPED_ABILITIES; i++) {
         AbilitySlot abilitySlot = Instantiate(abilitySlotPrefab, abilitySlotsContainer.transform, false);
         abilitySlot.abilitySlotId = i;

         // Add drop zone for ability equip
         ItemDropZone dropZone = abilitySlot.gameObject.AddComponent<ItemDropZone>();
         dropZone.rectTransform = abilitySlot.GetComponent<RectTransform>();
         equipmentDropZone.Add(dropZone);
         _equippedAbilitySlots.Add(abilitySlot);
      }

      // Create the ability rows
      foreach (AbilitySQLData ability in abilityList) {
         // Get the base data for the ability
         BasicAbilityData basicAbilityData = AbilityManager.getAbility(ability.abilityID, ability.abilityType);

         // Builds the ability description
         StringBuilder builder = new StringBuilder();
         builder.Append(basicAbilityData.itemDescription);
         string description = builder.ToString();

         // Determine if the ability is equipped
         if (ability.equipSlotIndex >= 0 && ability.equipSlotIndex < AbilityManager.MAX_EQUIPPED_ABILITIES) {
            // Initialize the equipped ability slot
            _equippedAbilitySlots[ability.equipSlotIndex].setSlotForAbilityData(ability.abilityID, basicAbilityData, description);
         } else {
            // Instantiate an ability row
            AbilityRow abilityRow = Instantiate(abilityRowPrefab, abilityRowsContainer.transform, false);
            abilityRow.setRowForAbilityData(basicAbilityData, description);
         }
         cachedAbilityList.Add(basicAbilityData);
      }
   }

   public void displayDescription(Sprite iconSprite, string name, string description) {
      descriptionIcon.sprite = iconSprite;
      descriptionName.text = name;
      descriptionText.text = description;
   }

   #region Equip Feature

   public void tryEquipAbility(int abilityId, int slotID) {
      if (_equippedAbilitySlots[slotID].isFree()) {
         Global.player.rpc.Cmd_UpdateAbility(abilityId, _equippedAbilitySlots[slotID].abilitySlotId);
      }
   }

   public void unequipAbility (int abilityId) {
      canvasBlocker.SetActive(true);
      Global.player.rpc.Cmd_UpdateAbility(abilityId, -1);
   }

   public void tryGrabAbility (AbilityRow abilityCell) {
      BasicAbilityData castedAbility = AbilityManager.getAbility(abilityCell.abilityName);
      _sourceAbilityCell = abilityCell;
      _cachedAbility = castedAbility;

      // Hide the cell being grabbed
      _sourceAbilityCell.hide();

      // Initialize the common grabbed object
      _draggedAbilityCell.setRowForAbilityData(castedAbility, castedAbility.itemDescription);
      _draggableAbility.activate();

      scroller.enabled = false;
   }

   public void tryGrabEquippedAbility (AbilitySlot abilitySlot) {
      BasicAbilityData castedAbility = AbilityManager.getAbility(abilitySlot.abilityName.text);
      _sourceAbilitySlot = abilitySlot;
      _cachedAbility = castedAbility;

      // Hide the cell being grabbed
      _sourceAbilitySlot.hide();

      // Initialize the common grabbed object
      _draggedAbilityCell.setRowForAbilityData(castedAbility, castedAbility.itemDescription);
      _draggableAbility.activate();
   }

   public void stopGrabbingAbility () {
      if (_sourceAbilityCell != null && _draggableAbility != null) {
         // Restore the grabbed cell
         _sourceAbilityCell.show();
         _draggedAbilityCell.hide();

         // Deactivate the grabbed ability
         _draggableAbility.deactivate();

         scroller.enabled = true;
      }

      if (_sourceAbilitySlot != null && _draggableAbility != null) {
         // Restore the grabbed cell
         _sourceAbilitySlot.show();
         _draggedAbilityCell.hide();

         // Deactivate the grabbed ability
         _draggableAbility.deactivate();

         scroller.enabled = true;
      }
   }

   public void tryDropGrabbedAbility (Vector2 screenPosition) {
      if (_sourceAbilityCell != null) {
         // Determine which action was performed
         bool droppedInInventory = inventoryDropZone.isInZone(screenPosition);
         bool droppedInEquipmentSlots = false;
         AbilitySlot droppedSlot = null;

         foreach (ItemDropZone zone in equipmentDropZone) {
            if (zone.isInZone(screenPosition)) {
               droppedInEquipmentSlots = true;
               droppedSlot = zone.GetComponent<AbilitySlot>();
               break;
            }
         }

         // Ability can be dragged from the equipment slots to the inventory or vice-versa
         if (droppedInEquipmentSlots && !droppedInInventory && droppedSlot.isFree()) {
            canvasBlocker.SetActive(true);

            int abilityIDCache = _cachedAbility.itemID;
            tryEquipAbility(abilityIDCache, droppedSlot.abilitySlotId);
            droppedSlot?.setSlotForAbilityData(_cachedAbility.itemID, _cachedAbility, _cachedAbility.itemDescription);
            _draggableAbility.deactivate();
         } else {
            // Otherwise, simply stop grabbing
            stopGrabbingAbility();
         }
      }
      
      if (_sourceAbilitySlot != null) {
         // Determine which action was performed
         bool droppedInInventory = inventoryDropZone.isInZone(screenPosition);
         bool droppedInEquipmentSlots = false;
         AbilitySlot droppedSlot = null;

         foreach (ItemDropZone zone in equipmentDropZone) {
            if (zone.isInZone(screenPosition)) {
               droppedInEquipmentSlots = true;
               droppedSlot = zone.GetComponent<AbilitySlot>();
               break;
            }
         }

         if (droppedInInventory) {
            canvasBlocker.SetActive(true);

            int abilityIDCache = _cachedAbility.itemID;
            unequipAbility(abilityIDCache);

            _draggableAbility.deactivate();
         } else if (droppedInEquipmentSlots && droppedSlot != _sourceAbilitySlot) {
            canvasBlocker.SetActive(true);

            int abilityIDCache = _cachedAbility.itemID;
            tryEquipAbility(abilityIDCache, droppedSlot.abilitySlotId);
            droppedSlot?.setSlotForAbilityData(_cachedAbility.itemID, _cachedAbility, _cachedAbility.itemDescription);
            _draggableAbility.deactivate();
         } else {
            // Otherwise, simply stop grabbing
            stopGrabbingAbility();
         }
      }
   }

   #endregion

   #region Private Variables

#pragma warning disable 0649
   // The equipped ability slots
   private List<AbilitySlot> _equippedAbilitySlots = new List<AbilitySlot>();

   // The data of the ability being dragged
   private BasicAbilityData _cachedAbility;

   // The cell from which an ability was grabbed
   [SerializeField]
   private AbilityRow _sourceAbilityCell, _draggedAbilityCell;

   // The current slot being updated
   private AbilitySlot _sourceAbilitySlot;

   // The grabbed ability UI that can be dragged
   [SerializeField]
   private GrabbedAbility _draggableAbility;
#pragma warning restore 0649 

   #endregion
}
