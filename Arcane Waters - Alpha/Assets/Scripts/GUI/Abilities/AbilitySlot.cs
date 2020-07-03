using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class AbilitySlot : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler
{
   #region Public Variables

   // The slot id of the ability
   public int abilitySlotId;

   // The icon of the ability
   public Image icon;

   // The name of the ability
   public Text abilityName;

   // Transform where Tooltip should snap to
   public Transform displayPoint;

   // Holder of the contents of the template
   public GameObject contentHolder, blankContentHolder;

   // Reference to the selection button
   public Button buttonReference;

   // Disables the capability to grab this template
   public bool disableGrab;

   #endregion

   public void Awake () {
      icon.gameObject.SetActive(false);
      abilityName.text = "";
      _isUsed = false;
      buttonReference.interactable = false;
   }

   public void setSlotForAbilityData (int abilityId, BasicAbilityData basicAbilityData, string description) {
      _basicAbilityData = basicAbilityData;
      _description = description;
      _abilityId = abilityId;
      icon.sprite = ImageManager.getSprite(basicAbilityData.itemIconPath);
      abilityName.text = basicAbilityData.itemName;
      icon.gameObject.SetActive(true);
      _isUsed = true;
      buttonReference.interactable = true;

      show();
   }

   public void OnPointerEnter (PointerEventData eventData) {
      if (_isUsed) {
         // Display the ability description in the ability panel
         AbilityPanel.self.displayDescription(icon.sprite, abilityName.text, _description, _basicAbilityData.abilityLevel, _basicAbilityData.abilityCost);
      }
   }

   public bool isFree () {
      return !_isUsed;
   }

   public void OnPointerDown (PointerEventData eventData) {
      if (_isUsed && !disableGrab) {
         AbilityPanel.self.tryGrabEquippedAbility(this);
      }
   }

   public void hide () {
      contentHolder.SetActive(false);
      blankContentHolder.SetActive(true);
      buttonReference.interactable = false;
   }

   public void show () {
      buttonReference.interactable = true;
      contentHolder.SetActive(true);
      blankContentHolder.SetActive(false);
   }

   #region Private Variables

   // The id of the ability
   private int _abilityId;

   // The base data of the displayed ability
   private BasicAbilityData _basicAbilityData;

   // The description of this ability
   private string _description;

   // Gets set to true when the slot holds an ability
   private bool _isUsed = true;

   #endregion
}
