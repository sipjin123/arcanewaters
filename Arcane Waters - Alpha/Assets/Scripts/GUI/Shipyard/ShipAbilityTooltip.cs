using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShipAbilityTooltip : MonoBehaviour {
   #region Public Variables

   // The ability tooltip UI showing the basic info of the ability
   public Text abilityDamageText, abilityProjectileMass, abilityStatusText, abilityNameText;
   public Image abilityIcon;
   public Transform abilityTooltipPivot;
   public GameObject abilityToolTipHolder;
   public Text abilityDescription;

   #endregion

   public void triggerAbilityTooltip (Vector2 coordinates, ShipAbilityData abilityData) {
      ProjectileStatData projectileData = ProjectileStatManager.self.getProjectileData(abilityData.projectileId);
      abilityTooltipPivot.transform.position = coordinates;
      abilityNameText.text = abilityData.abilityName;
      abilityIcon.sprite = ImageManager.getSprite(abilityData.skillIconPath);
      abilityToolTipHolder.SetActive(true);

      if (projectileData != null) {
         abilityDamageText.text = projectileData.projectileDamage.ToString();
         abilityStatusText.text = abilityData.statusType.ToString();
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
