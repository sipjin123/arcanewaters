using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using System.Linq;

/// <summary>
/// This class will trigger the tooltip game object with the appropiate text.
/// The text is stored in XMLTootips.xml 
/// Two step process:
/// 1. Place this component on the game object needing a tooltip.
/// 2. Manual enter a key/value in the xml file.
/// Note: If the tooltip text is created dynamically at runtime, it will not be stored in the XML document.
/// Instead, the text will be set in this script.
/// </summary>


[RequireComponent(typeof(EventTrigger))]
public class ToolTipComponent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
   #region Public Variables

   // Possible positions for tooltip relative to UI element and opened panel
   public enum Type
   {
      DictionaryEntry = 0,
      ItemCellInventory = 1,
      ItemCellIngredient = 2,
      PerkElementTemplate = 3,
      CreationPerkIcon = 4,
      SellButton = 5,
      DynamicText = 6
   }
   
   public enum TooltipPlacement
   {
      AutoPlacement = 0,
      AboveUIElement = 1,
      LeftSideOfPanel = 2,
      RightSideOfPanel = 3,
      BelowUIElement = 4
   }
   
   public enum DisplayType {
      OnPointerEnter = 0,
      OnPointerDown = 1
   }
   
   // Stores the desired tooltip placement
   public TooltipPlacement tooltipPlacement;

   // Tooltip type
   public Type tooltipType;

   // The content of the tooltip
   [HideInInspector]
   public string message;

   // Maximum Width of the tooltip.  A value of zero allows the tooltip to be sized automatically or to be set in the tooltip web tool.
   public float maxWidth;

   // Set to true if an offscreen tooltip should be shifted back onto screen instead of automatically being placed above the object.
   public bool nudgeAwayFromBorder = false;

   // Identifies if the key is derived from a child object
   public bool isKeyDerivedFromChildObject = false;

   // Tooltip display type
   public DisplayType displayType; 

   #endregion

   public bool isActive () {
      return _isActive;
   }

   public void OnPointerExit (PointerEventData pointerEventData) {
      TooltipHandler.self.cancelToolTip();
      _isActive = false;
   }
   
   public void OnPointerEnter (PointerEventData eventData) {
      if (displayType == DisplayType.OnPointerEnter) {
         showTooltip();
      }
   }

   public void OnPointerDown (PointerEventData eventData) {
      if (displayType == DisplayType.OnPointerDown) {
         showTooltip();
      }
   }

   public void showTooltip () {
      // Variable to store the panel gameObject
      _panelRoot = this.gameObject;
      _tooltipOwner = this.gameObject;
      _isActive = true;

      // Traverse up the parents looking for sprite with panel image
      Transform currentParent = transform.parent;
      while (currentParent != null) {
         if (currentParent.gameObject.GetComponent<Image>() != null) {
            if ((currentParent.gameObject.GetComponent<Image>().sprite.name == "panel_base_2x_nails") || (currentParent.gameObject.GetComponent<Image>().sprite.name == "panel_background") || (currentParent.gameObject.GetComponent<Image>().sprite.name == "panel_base_2x")) {
               _panelRoot = currentParent.gameObject;
               break;
            }
         }
         currentParent = currentParent.parent;
      }

      // If this tooptip belongs to an inventory item, store it's rarity level and star level
      Rarity.Type itemRarityType = Rarity.Type.None;

      if (this.tooltipType == Type.ItemCellInventory) {
         // Get the rarity level of weapon shop items
         if (this.transform.parent.GetComponent<AdventureItemRow>() != null) {
            itemRarityType = this.transform.parent.GetComponent<AdventureItemRow>().item.getRarity();
         }

         // Get rarity level of items in the user's inventory
         if (this.GetComponent<ItemCellInventory>() != null) {
            itemRarityType = this.GetComponent<ItemCellInventory>().itemRarityType;
         }
         
         // Get rarity level of items in the pvp shop panel
         if (this.GetComponentInParent<PvpShopTemplate>() != null) {
            itemRarityType = this.GetComponentInParent<PvpShopTemplate>().rarityType;
         }

         // Get rarity level of hoverable icon
         if (GetComponent<HoverableItemIcon>() != null && GetComponent<HoverableItemIcon>().getItem() != null) {
            itemRarityType = GetComponent<HoverableItemIcon>().getItem().getRarity();
         }
      }

      // Check if the tooltip text is being created dynamically at runtime (if so, there will be no entry in the xml document)
      if (tooltipType == Type.ItemCellInventory) {
         maxWidth = 250;
         TooltipHandler.self.callToolTip(itemRarityType, _tooltipOwner, tooltipType, message, tooltipPlacement, this.transform.position, _panelRoot, maxWidth);
         return;
      }

      if (tooltipType == Type.ItemCellIngredient) {
         maxWidth = 250;
         TooltipHandler.self.callToolTip(itemRarityType, _tooltipOwner, tooltipType, message, tooltipPlacement, this.transform.position, _panelRoot, maxWidth);
         return;
      }

      if (tooltipType == Type.PerkElementTemplate) {
         maxWidth = 220;
         TooltipHandler.self.callToolTip(itemRarityType, _tooltipOwner, tooltipType, message, tooltipPlacement, this.transform.position, _panelRoot, maxWidth);
         return;
      }

      if (tooltipType == Type.CreationPerkIcon) {
         maxWidth = 185;
         TooltipHandler.self.callToolTip(itemRarityType, _tooltipOwner, tooltipType, message, tooltipPlacement, this.transform.position, _panelRoot, maxWidth);
         return;
      }

      if (tooltipType == Type.SellButton) {
         maxWidth = 200;
         TooltipHandler.self.callToolTip(itemRarityType, _tooltipOwner, tooltipType, message, tooltipPlacement, this.transform.position, _panelRoot, maxWidth);
         return;
      }

      if (tooltipType == Type.DynamicText) {
         // Do not modify maxWidth value here - it is being set directly by user
         TooltipHandler.self.callToolTip(itemRarityType, _tooltipOwner, tooltipType, message, tooltipPlacement, this.transform.position, _panelRoot, maxWidth);
         return;
      }

      // Variable to hold the sprite name of this item, which is then added to end of the dictionary key.
      string dictKeySuffix = null;
      string rootKey = null;

      // Create the key needed to find the tooltip in the dictionary
      // The key will usually be the name of the object that has the TooltipComponent attached, however,
      // sometimes the key may be on a child object (i.e. icon) of the current object (i.e. row of stats)
      // and we want the current object to trigger the tooltip.
      if (isKeyDerivedFromChildObject == true) {
         GameObject childKey = this.GetComponentInChildren<Image>().gameObject;
         if (childKey != null) {
            rootKey = childKey.name;
            if (childKey.GetComponent<Image>() != null) {
               if (childKey.GetComponent<Image>().sprite != null) {
                  dictKeySuffix = childKey.GetComponent<Image>().sprite.name;
               }
            }
         }
      } else {
         if ((this.GetComponent<Image>() != null) && (this.GetComponent<Image>().sprite != null)) {
            dictKeySuffix = this.GetComponent<Image>().sprite.name;
         }
         rootKey = this.name;
      }
      string dictKey = rootKey + dictKeySuffix;

      // Check if the key is in the dictionary
      if (UIToolTipManager.self.toolTipDict.ContainsKey(dictKey)) {
         if (UIToolTipManager.self.toolTipDict[dictKey] != null) {
            // Retrieve the tootip text if the key is in the dictionary
            message = UIToolTipManager.self.toolTipDict[dictKey].value;
            tooltipPlacement = (TooltipPlacement) UIToolTipManager.self.toolTipDict[dictKey].displayLocation;

            // Send the message to the tooltip handler
            TooltipHandler.self.callToolTip(itemRarityType, _tooltipOwner, tooltipType, message, tooltipPlacement, this.transform.position, _panelRoot, maxWidth);
         }
      }
   }

   #region Private Variables

   // The gameobject that holds the panel background image
   private GameObject _panelRoot;

   // The gameobject that the tooltip belongs to
   private GameObject _tooltipOwner;

   // Determine whether tooltip is being shown now
   private bool _isActive = false;

   #endregion
}