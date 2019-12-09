using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using System.Text;

public class AbilityRow : MonoBehaviour, IPointerEnterHandler
{
   #region Public Variables

   // The icon of the ability
   public Image icon;

   // The name of the ability
   public Text abilityName;

   #endregion

   public void setRowForAbilityData (int abilityId, BasicAbilityData basicAbilityData, string description) {
      _basicAbilityData = basicAbilityData;
      _abilityId = abilityId;
      _description = description;
      icon.sprite = ImageManager.getSprite(basicAbilityData.itemIconPath);
      abilityName.text = basicAbilityData.itemName;
   }

   public void onRowButtonPress () {
      AbilityPanel.self.tryEquipAbility(_abilityId);
   }

   public void OnPointerEnter (PointerEventData eventData) {
      // Display the ability description in the ability panel
      AbilityPanel.self.displayDescription(icon.sprite, abilityName.text, _description);
   }

   #region Private Variables

   // The id of the ability
   private int _abilityId;

   // The base data of the displayed ability
   private BasicAbilityData _basicAbilityData;

   // The description of this ability
   private string _description;

   #endregion
}
