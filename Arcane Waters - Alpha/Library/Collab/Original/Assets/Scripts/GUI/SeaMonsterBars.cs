using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SeaMonsterBars : MonoBehaviour
{
   #region Public Variables

   // Our health bar image
   public Image healthBarImage;

   // Our reload bar image
   public Image reloadBarImage;
   
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

   // Determines if this unit should update health bar upon taking damage
   public bool isDamageable;

   #endregion

   void Awake () {
      // Look up components
      _canvasGroup = GetComponent<CanvasGroup>();
      _entity = GetComponentInParent<SeaEntity>();

      // Start out hidden
      barsContainer.SetActive(false);
   }

   void Update () {
      if (_entity == null) {
         return;
      }

      // Hide and show our status icons accordingly
      handleStatusIcons();

      // Hide our bars if we haven't had a combat action
      barsContainer.SetActive(_entity.hasAnyCombat());

      if (!isDamageable) {
         return;
      }

      // Set our health bar size based on our current health
      healthBarImage.fillAmount = (float) _entity.currentHealth / _entity.maxHealth;

      // Update our reload bar when we recently fired
      reloadBarImage.fillAmount = (Time.time - _entity.getLastAttackTime()) / _entity.reloadDelay;

      // Show a different background for ships owned by other players
      if (Global.player != null && _entity != Global.player) {
         barBackgroundImage.sprite = barsBackgroundAlt;
      }
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
         if (!isDamageable) {
            enemyIcon.gameObject.SetActive(true);
         }
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
