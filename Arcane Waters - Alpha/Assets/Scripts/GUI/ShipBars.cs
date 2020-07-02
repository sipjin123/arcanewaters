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

   // The guild icon
   public GuildIcon guildIcon;

   // The container for our bars
   public GameObject barsContainer;

   // And empty column used to correctly align icons in certain cases
   public GameObject togglableSpacer;

   // The Image icon that shows when we're being targeted
   public Image targetedIcon;

   // The Image icon that shows when we're considered an enemy
   public Image enemyIcon;

   // The Image that holds the background for our bars
   public Image barBackgroundImage;

   // An alternate sprite we use for the health bar background on non-player ships
   public Sprite barsBackgroundAlt;

   // The character portrait
   public CharacterPortrait portrait;

   // The canvas group of the portrait
   public CanvasGroup portraitCanvasGroup;

   // The frame image of the character portrait
   public Image portraitFrameImage;

   // The frame used if the portrait is the local player's
   public Sprite localPlayerFrame;

   // The frame used if the portrait is not the local player's
   public Sprite nonLocalPlayerFrame;

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

      if (portrait != null) {
         StartCoroutine(CO_InitializeUserInfo());
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

      // Hide our bars if we haven't had a combat action and if the player is not targetting this ship
      barsContainer.SetActive(_entity.hasAnyCombat() || _entity.isAttackCursorOver());

      // Enable the empty column to correctly align icons when there is no guild icon
      togglableSpacer.SetActive(!guildIcon.gameObject.activeSelf && !barsContainer.activeSelf);

      // Hide and show our status icons accordingly
      handleStatusIcons();

      // Show a different background for ships owned by other players
      if (Global.player != null && _entity != Global.player) {
         barBackgroundImage.sprite = barsBackgroundAlt;
      }

      if (portrait == null) {
         return;
      }

      // Only show the user info when the mouse is over the entity or its member cell in the group panel
      if (!_entity.isDead() && (_entity.isMouseOver() || _entity.isAttackCursorOver() || VoyageGroupPanel.self.isMouseOverMemberCell(_entity.userId))) {
         showUserInfo();
      } else {
         hideUserInfo();
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

   private IEnumerator CO_InitializeUserInfo () {
      // Wait until the entity has been initialized
      while (Util.isEmpty(_entity.entityName)) {
         yield return null;
      }

      portrait.initialize(_entity);

      // Set the portrait frame for local or non local entities
      if (_entity.isLocalPlayer) {
         portraitFrameImage.sprite = localPlayerFrame;
      } else {
         portraitFrameImage.sprite = nonLocalPlayerFrame;
      }

      if (_entity.guildId > 0) {
         PlayerShipEntity playerShip = (PlayerShipEntity) _entity;
         guildIcon.setBorder(playerShip.guildIconBorder);
         guildIcon.setBackground(playerShip.guildIconBackground, playerShip.guildIconBackPalette1, playerShip.guildIconBackPalette2);
         guildIcon.setSigil(playerShip.guildIconSigil, playerShip.guildIconSigilPalette1, playerShip.guildIconSigilPalette2);
      } else {
         guildIcon.gameObject.SetActive(false);
      }
   }

   private void showUserInfo () {
      if (portraitCanvasGroup.alpha < 1) {
         portraitCanvasGroup.alpha = 1;
         guildIcon.show();
         portrait.updateBackground(_entity);
      }
   }

   private void hideUserInfo () {
      if (portraitCanvasGroup.alpha > 0) {
         portraitCanvasGroup.alpha = 0;
         guildIcon.hide();
      }
   }

   #region Private Variables

   // Our associated Sea Entity
   protected SeaEntity _entity;

   // Our Canvas Group
   protected CanvasGroup _canvasGroup;

   #endregion
}
