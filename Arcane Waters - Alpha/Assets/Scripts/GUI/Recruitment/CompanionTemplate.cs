using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class CompanionTemplate : MonoBehaviour, IPointerDownHandler
{
   #region Public Variables

   // The basic info of the companion
   public Text companionType;
   public Text companionLevel;
   public Text companionName;
   public Image companionIcon;

   // Selects this template for dragging
   public Button selectButton;

   // Determines if this template is available
   public bool isOccupied;

   // Equipment slot index
   public int equipmentSlot;

   // The icon path of this template
   public string iconPath;

   #endregion

   public void OnPointerDown (PointerEventData eventData) {
      CompanionPanel.self.tryGrabTemplate(this);
   }

   public void setData (CompanionTemplate copiedTemplate) {
      if (copiedTemplate == null) {
         isOccupied = false;
         this.companionType.text = string.Empty;
         this.companionLevel.text = string.Empty;
         this.companionName.text = string.Empty;
         this.companionIcon.sprite = ImageManager.self.blankSprite;
         this.iconPath = string.Empty;
      } else {
         isOccupied = true;
         this.companionType.text = copiedTemplate.companionType.text;
         this.companionLevel.text = copiedTemplate.companionLevel.text;
         this.companionName.text = copiedTemplate.companionName.text;
         this.companionIcon.sprite = copiedTemplate.companionIcon.sprite;
         this.iconPath = copiedTemplate.iconPath;
      }
   }

   public CompanionInfo getInfo () {
      return new CompanionInfo {
         companionLevel = int.Parse(companionLevel.text),
         companionName = companionName.text,
         companionType = int.Parse(companionType.text),
         equippedSlot = equipmentSlot,
         iconPath = iconPath
      };
   }

   #region Private Variables

   #endregion
}
