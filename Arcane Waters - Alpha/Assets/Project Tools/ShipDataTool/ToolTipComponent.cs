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
public class ToolTipComponent : MonoBehaviour {
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

   #endregion

   private void Awake () {
      EventTrigger eventTrigger = GetComponent<EventTrigger>();
      EventTrigger.Entry eventEntry = new EventTrigger.Entry();
      eventEntry.eventID = EventTriggerType.PointerEnter;
      eventEntry.callback.AddListener((data) => { onHoverEnter((PointerEventData)data); });
      eventTrigger.triggers.Add(eventEntry);
      
      EventTrigger.Entry eventEntryExit = new EventTrigger.Entry();
      eventEntryExit.eventID = EventTriggerType.PointerExit;
      eventEntryExit.callback.AddListener((data) => { onHoverExit(); });
      eventTrigger.triggers.Add(eventEntryExit);
   }

   public void onHoverExit () {
      TooltipHandler.self.cancelToolTip();
   }

   public void onHoverEnter (PointerEventData eventData) {
      // Variable to store the panel gameObject
      _panelRoot = null;

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
      if ((this.GetComponent<ItemCellInventory>() != null) || (this.GetComponent<ItemCellIngredient>() != null) || (this.GetComponent<CreationPerkIcon>() != null)) {

         // Send the message to the tooltip handler
         TooltipHandler.self.callToolTip(message, tooltipPlacement, this.transform.position, _panelRoot);
      }
      // Create the key needed to find the tooltip in the dictionary
      else {
         if ((this.GetComponent<Image>() != null) && (this.GetComponent<Image>().sprite != null)) {
            dictKeySuffix = this.GetComponent<Image>().sprite.name;
         }
         string dictKey = this.name + dictKeySuffix;

         // Check if the key is in the dictionary
         if (UIToolTipManager.self.toolTipDict.ContainsKey(dictKey)) {
            if (UIToolTipManager.self.toolTipDict[dictKey] != null) {

               // Retrieve the tootip text if the key is in the dictionary
               message = UIToolTipManager.self.toolTipDict[dictKey];

               // Send the message to the tooltip handler
               TooltipHandler.self.callToolTip(message, tooltipPlacement, this.transform.position, _panelRoot);
            }
         } 
      }
   }

   #region Private Variables

   // The gameobject that holds the panel background image
   private GameObject _panelRoot;

   #endregion
}