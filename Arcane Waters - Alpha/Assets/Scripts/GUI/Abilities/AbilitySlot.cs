using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class AbilitySlot : MonoBehaviour, IPointerEnterHandler
{
   #region Public Variables

   // The slot id of the ability
   public int abilitySlotId;

   // The icon of the ability
   public Image icon;

   // The name of the ability
   public Text abilityName;

   // The unequip button
   public Button unequipButton;

   #endregion

   public void Awake () {
      icon.gameObject.SetActive(false);
      abilityName.text = "";
      unequipButton.gameObject.SetActive(false);
      _isUsed = false;
   }

   public void setSlotForAbilityData (int abilityId, BasicAbilityData basicAbilityData, string description) {
      _basicAbilityData = basicAbilityData;
      _description = description;
      _abilityId = abilityId;
      icon.sprite = ImageManager.getSprite(basicAbilityData.itemIconPath);
      abilityName.text = basicAbilityData.itemName;
      icon.gameObject.SetActive(true);
      unequipButton.gameObject.SetActive(true);
      _isUsed = true;
   }

   public void onUnequipButtonPress () {
      if (_isUsed) {
         AbilityPanel.self.unequipAbility(_abilityId);
      }
   }

   public void OnPointerEnter (PointerEventData eventData) {
      if (_isUsed) {
         // Display the ability description in the ability panel
         AbilityPanel.self.displayDescription(icon.sprite, abilityName.text, _description);
      }
   }

   public bool isFree () {
      return !_isUsed;
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
