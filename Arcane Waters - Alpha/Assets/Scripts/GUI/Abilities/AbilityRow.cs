using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using System.Text;

public class AbilityRow : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler
{
   #region Public Variables

   // The icon of the ability
   public Image icon;

   // The name of the ability
   public string abilityName;

   // Holder of the contents of the template
   public GameObject contentHolder;

   #endregion

   public void setRowForAbilityData (BasicAbilityData basicAbilityData, string description) {
      _basicAbilityData = basicAbilityData;
      _description = description;
      icon.sprite = ImageManager.getSprite(basicAbilityData.itemIconPath);
      abilityName = basicAbilityData.itemName;
      show();
   }

   public void hide () {
      gameObject.SetActive(false);
      contentHolder.SetActive(false);
   }

   public void show () {
      gameObject.SetActive(true);
      contentHolder.SetActive(true);
   }

   public void OnPointerEnter (PointerEventData eventData) {
      // Display the ability description in the ability panel
      AbilityPanel.self.displayDescription(icon.sprite, abilityName, _description, _basicAbilityData.abilityLevel, _basicAbilityData.abilityCost);
   }

   public void OnPointerDown (PointerEventData eventData) {
      AbilityPanel.self.tryGrabAbility(this);
   }

   #region Private Variables

   // The base data of the displayed ability
   private BasicAbilityData _basicAbilityData;

   // The description of this ability
   private string _description;

   #endregion
}
