using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShipAbilityTooltip : MonoBehaviour {
   #region Public Variables

   // The ability tooltip UI showing the basic info of the ability
   public Text abilityDamageText, abilityProjectileMass, abilityStatusText, abilityNameText;
   public Image abilityIcon, statusIcon;
   public Transform abilityTooltipPivot;
   public GameObject abilityToolTipHolder;
   public Text abilityDescription;
   public GameObject statusTab;

   // The debuff sprite pair representing each status
   public List<StatusSpritePair> debuffSpritePair;

   #endregion

   public void triggerAbilityTooltip (Vector2 coordinates, ShipAbilityData abilityData) {
      ProjectileStatData projectileData = ProjectileStatManager.self.getProjectileData(abilityData.projectileId);
      abilityTooltipPivot.transform.position = coordinates;
      abilityNameText.text = abilityData.abilityName;
      abilityIcon.sprite = ImageManager.getSprite(abilityData.skillIconPath);
      abilityToolTipHolder.SetActive(true);

      if (projectileData != null) {
         abilityDamageText.text = projectileData.projectileDamage.ToString();
         abilityStatusText.text = ((Status.Type) abilityData.statusType).ToString();
         if (isProjectile(abilityData)) {
            abilityProjectileMass.text = projectileData.projectileMass.ToString();
         } else {
            abilityProjectileMass.text = "-";
         }
         abilityDescription.text = abilityData.abilityDescription;
      } else {
         abilityDamageText.text = "Missing Data";
         abilityProjectileMass.text = "Missing Data";
         abilityStatusText.text = "Missing Data";
      }

      if ((Status.Type) abilityData.statusType == Status.Type.None) {
         statusTab.gameObject.SetActive(false);
      } else {
         statusTab.gameObject.SetActive(true);
         StatusSpritePair spritePair = debuffSpritePair.Find(_ => (int) _.statusType == abilityData.statusType);
         statusIcon.sprite = spritePair == null ? ImageManager.self.blankSprite : spritePair.statusSprite;
      }
   }

   private bool isProjectile (ShipAbilityData abilityData) {
      switch (abilityData.selectedAttackType) {
         case Attack.Type.Heal:
         case Attack.Type.SpeedBoost:
         case Attack.Type.DamageAmplify:
         case Attack.Type.ArmorBoost:
            return false;
      }
      return true;
   }

   #region Private Variables

   #endregion
}
