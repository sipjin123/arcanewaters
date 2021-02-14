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
/// </summary>

[RequireComponent(typeof(EventTrigger))]
public class ToolTipComponent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
   #region Public Variables

   // Possible positions for tooltip relative to UI element and opened panel
   public enum TooltipPlacement
   {
      AutoPlacement = 0,
      AboveUIElement = 1,
      LeftSideOfPanel = 2,
      RightSideOfPanel = 3
   }

   // Stores the desired tooltip placement
   public TooltipPlacement tooltipPlacement;

   // The content of the tooltip
   [HideInInspector]
   public string message;

   // Maxium Width of the tooltip.  A value of zero allows the tooltip to be sized automatically or to be set in the tooltip web tool.
   public float maxWidth;

   // Set to true if an offscreen tooltip should be shifted back onto screen instead of automatically being placed above the object.
   public bool forceFitOnScreen = false;

   #endregion

   public void OnPointerExit (PointerEventData pointerEventData) {
      TooltipHandler.self.cancelToolTip();
   }

   public void OnPointerEnter (PointerEventData pointerEventData) {
      // Variable to store the panel gameObject
      _panelRoot = this.gameObject;
      _tooltipOwner = this.gameObject;

      // Traverse up the parents looking for sprite with panel image
      Transform currentParent = transform.parent;
      while (currentParent != null) {
         if (currentParent.gameObject.GetComponent<Image>() != null) {
            if ((currentParent.gameObject.GetComponent<Image>().sprite.name == "panel_base_2x_nails") || (currentParent.gameObject.GetComponent<Image>().sprite.name == "panel_background") || (currentParent.gameObject.GetComponent<Image>().sprite.name == "panel_base_2x")) {
               _panelRoot = currentParent.gameObject;
            }
         }
         currentParent = currentParent.parent;
      }

      // Retrieve tooltip message from dictionary
      string dictKeySuffix = null;

      // Check if the tooltip text is being created dynamically at runtime (if so, there will be no entry in the xml document)
      if (this.GetComponent<ItemCellInventory>() != null){
         maxWidth = 250;
         TooltipHandler.self.callToolTip(_tooltipOwner, message, tooltipPlacement, this.transform.position, _panelRoot, maxWidth);
         return;
      }

      if (this.GetComponent<ItemCellIngredient>() != null){
         maxWidth = 250;
         TooltipHandler.self.callToolTip(_tooltipOwner, message, tooltipPlacement, this.transform.position, _panelRoot, maxWidth);
         return;
      }

      if (this.GetComponent<PerkElementTemplate>() != null){
         maxWidth = 220;
         TooltipHandler.self.callToolTip(_tooltipOwner, message, tooltipPlacement, this.transform.position, _panelRoot, maxWidth);
         return;
      }

      if (this.GetComponent<CreationPerkIcon>() != null) {
         maxWidth = 185;
         TooltipHandler.self.callToolTip(_tooltipOwner, message, tooltipPlacement, this.transform.position, _panelRoot, maxWidth);
         return;
      }

      if (this.GetComponent<PingPanel>() != null) {
         maxWidth = 100;
         TooltipHandler.self.callToolTip(_tooltipOwner, message, tooltipPlacement, this.transform.position, _panelRoot, maxWidth);
         return;
      }

      // Create the key needed to find the tooltip in the dictionary
      if ((this.GetComponent<Image>() != null) && (this.GetComponent<Image>().sprite != null)) {
         dictKeySuffix = this.GetComponent<Image>().sprite.name;
      }
      string dictKey = this.name + dictKeySuffix;

      // Check if the key is in the dictionary
      if (UIToolTipManager.self.toolTipDict.ContainsKey(dictKey)) {
         if (UIToolTipManager.self.toolTipDict[dictKey] != null) {
            // Retrieve the tootip text if the key is in the dictionary
            message = UIToolTipManager.self.toolTipDict[dictKey].value;
            tooltipPlacement = (TooltipPlacement) UIToolTipManager.self.toolTipDict[dictKey].displayLocation;

            // Send the message to the tooltip handler
            TooltipHandler.self.callToolTip(_tooltipOwner,message, tooltipPlacement, this.transform.position, _panelRoot, maxWidth);
         }
      }
   }

   #region Private Variables

   // The gameobject that holds the panel background image
   private GameObject _panelRoot;

   // The gameobject that the tooltip belongs to
   private GameObject _tooltipOwner;

   #endregion
}