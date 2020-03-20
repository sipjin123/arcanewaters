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

   // The id of the companion
   public int companionId;

   // The Id of the companion type
   public int companionTypeId;

   // Contents that should be disabled if the template is empty
   public GameObject[] disabledContents;

   #endregion

   public void OnPointerDown (PointerEventData eventData) {
      CompanionPanel.self.tryGrabTemplate(this);
   }

   public void setRawData (CompanionInfo info) {
      this.companionType.text = ((Enemy.Type)info.companionType).ToString();
      this.companionLevel.text = info.companionLevel.ToString();
      this.companionName.text = info.companionName;
      this.companionTypeId = info.companionType;
      Sprite iconSprite = ImageManager.getSprite(info.iconPath);
      if (iconSprite) {
         this.companionIcon.sprite = iconSprite;
      }
      this.iconPath = info.iconPath;
      this.companionId = info.companionId;

      foreach (GameObject obj in disabledContents) {
         obj.SetActive(true);
      }
   }

   public void setData (CompanionTemplate copiedTemplate) {
      if (copiedTemplate == null) {
         isOccupied = false;
         this.companionType.text = string.Empty;
         this.companionLevel.text = string.Empty;
         this.companionName.text = string.Empty;
         this.companionIcon.sprite = ImageManager.self.blankSprite;
         this.iconPath = string.Empty;
         this.companionId = -1;
         this.companionTypeId = 0;

         foreach (GameObject obj in disabledContents) {
            obj.SetActive(false);
         }
      } else {
         isOccupied = true;
         this.companionType.text = copiedTemplate.companionType.text;
         this.companionLevel.text = copiedTemplate.companionLevel.text;
         this.companionName.text = copiedTemplate.companionName.text;
         this.companionIcon.sprite = copiedTemplate.companionIcon.sprite;
         this.iconPath = copiedTemplate.iconPath;
         this.companionId = copiedTemplate.companionId;
         this.companionTypeId = copiedTemplate.companionTypeId;

         foreach (GameObject obj in disabledContents) {
            obj.SetActive(true);
         }
      }
   }

   public CompanionInfo getInfo () {
      return new CompanionInfo {
         companionLevel = int.Parse(companionLevel.text),
         companionName = companionName.text,
         companionType = companionTypeId,
         equippedSlot = equipmentSlot,
         iconPath = iconPath,
         companionId = companionId,
      };
   }

   #region Private Variables

   #endregion
}
