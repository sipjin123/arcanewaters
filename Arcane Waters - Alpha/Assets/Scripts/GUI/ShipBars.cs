using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShipBars : MonoBehaviour {
   #region Public Variables

   // Our health bar image
   public Image healthBarImage;

   // Our predicted damage bar image - the amount of damage the player shot will inflict
   public Image predictedDamageBarImage;

   // Our reload bar image
   public Image reloadBarImage;

   // The Faction icon
   public Image factionIcon;

   // The container for our bars
   public GameObject barsContainer;

   // The Image icon that shows when we're being targeted
   public Image targetedIcon;

   // The Image icon that shows when we're considered an enemy
   public Image enemyIcon;

   // The Image that holds the background for our bars
   public Image barBackgroundImage;

   // An alternate sprite we use for the health bar background on non-player ships
   public Sprite barsBackgroundAlt;

   #endregion

   void Awake () {
      // Look up components
      _canvasGroup = GetComponent<CanvasGroup>();
      _entity = GetComponentInParent<SeaEntity>();

      // Start out hidden
      barsContainer.SetActive(false);
   }

   private void Start () {
      if (_entity == null) {
         return;
      }

      if (factionIcon != null) {
         factionIcon.sprite = Faction.getShipIcon(_entity.faction);
      }
   }

   void Update () {
      if (_entity == null) {
         return;
      }

      // Set our health and predicted damage bar size based on our current health
      healthBarImage.fillAmount = (float) _entity.currentHealth / _entity.maxHealth;
      predictedDamageBarImage.fillAmount = healthBarImage.fillAmount;

      // Calculate the predicted damage from the aimed player shot
      float predictedDamage = (float) AttackManager.self.getPredictedDamage(_entity);
      predictedDamage = Mathf.Clamp(predictedDamage / _entity.maxHealth, 0, healthBarImage.fillAmount);

      // Subtract the predicted damage from the health bar - this will reveal the predictive bar below
      healthBarImage.fillAmount -= predictedDamage;

      // Update our reload bar when we recently fired
      reloadBarImage.fillAmount = (Time.time - _entity.getLastAttackTime()) / _entity.reloadDelay;

      // Only show the faction icon if we're in combat
      factionIcon.enabled = _entity.hasAnyCombat();

      // Hide our bars if we haven't had a combat action and if the player is not targetting this ship
      barsContainer.SetActive(_entity.hasAnyCombat() || AttackManager.self.isHoveringOver(_entity));

      // Hide and show our status icons accordingly
      handleStatusIcons();

      // Show a different background for ships owned by other players
      if (Global.player != null && _entity != Global.player) {
         barBackgroundImage.sprite = barsBackgroundAlt;
      }

      /*if (_entity.hasRecentCombat()) {
         _canvasGroup.alpha = 1f;
      } else {
         _canvasGroup.alpha -= 1f * Time.smoothDeltaTime;
      }*/
   }

   protected void handleStatusIcons () {
      // Default to hidden
      targetedIcon.gameObject.SetActive(false);
      enemyIcon.gameObject.SetActive(false);

      // Don't show if our bars aren't showing
      if (!barsContainer.activeSelf) {
         return;
      }

      // Show the red skull icon if we're an enemy of the player
      if (_entity.isEnemyOf(Global.player)) {
         enemyIcon.gameObject.SetActive(true);
      } else if (_entity.hasAttackers()) {
         // Show the target icon if we're being attacked
         targetedIcon.gameObject.SetActive(true);
      }
   }

   #region Private Variables

   // Our associated Sea Entity
   protected SeaEntity _entity;

   // Our Canvas Group
   protected CanvasGroup _canvasGroup;

   #endregion
}
