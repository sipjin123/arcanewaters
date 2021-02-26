using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShipBars : MonoBehaviour {
   #region Public Variables

   // The amount of health represented by one health unit
   public static int HP_PER_UNIT = 100;

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

   // The prefab we use for creating health units
   public ShipHealthUnit shipHealthUnitPrefab;

   // The container of health units
   public GameObject healthUnitContainer;

   #endregion

   void Awake () {
      // Look up components
      _canvasGroup = GetComponent<CanvasGroup>();
      _entity = GetComponentInParent<SeaEntity>();

      // Start out hidden
      barsContainer.SetActive(false);
   }

   protected virtual void Start () {
      if (_entity == null) {
         return;
      }
   }

   protected virtual void Update () {
      if (_entity == null) {
         return;
      }

      // Set our health bar based on our current health
      handleHealthBar();

      // Update our reload bar when we recently fired
      reloadBarImage.fillAmount = (float)(NetworkTime.time - _entity.getLastAttackTime()) / _entity.reloadDelay;

      // Hide our bars if we haven't had a combat action and if the player is not targetting this ship
      barsContainer.SetActive(_entity.hasAnyCombat() || _entity.isAttackCursorOver());

      // Hide and show our status icons accordingly
      handleStatusIcons();

      // Show a different background for ships owned by other players
      if (Global.player != null && _entity != Global.player) {
         barBackgroundImage.sprite = barsBackgroundAlt;
      }
      
      // Hide the canvas if the ship is dead
      _canvasGroup.alpha = _entity.isDead() ? 0 : 1;

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

   protected void handleHealthBar () {
      if (_entity.currentHealth == _lastHealth) {
         return;
      }

      int hpStep = 0;
      for (int i = 0; i < _healthUnits.Count; i++) {
         if ((hpStep + (HP_PER_UNIT/ 2)) < _entity.currentHealth) {
            _healthUnits[i].setStatus(ShipHealthUnit.Status.Healthy);
         } else if (hpStep < _entity.currentHealth) {
            _healthUnits[i].setStatus(ShipHealthUnit.Status.Damaged);
         } else {
            _healthUnits[i].setStatus(ShipHealthUnit.Status.Hidden);
         }
         hpStep += HP_PER_UNIT;
      }

      _lastHealth = _entity.currentHealth;
   }

   protected void initializeHealthBar () {
      healthUnitContainer.DestroyChildren();
      for (int i = 0; i < Mathf.Ceil((float) _entity.maxHealth / HP_PER_UNIT); i++) {
         ShipHealthUnit unit = Instantiate(shipHealthUnitPrefab, healthUnitContainer.transform, false);
         unit.setStatus(ShipHealthUnit.Status.Healthy);
         _healthUnits.Add(unit);
      }
   }

   #region Private Variables

   // Our associated Sea Entity
   protected SeaEntity _entity;

   // Our Canvas Group
   protected CanvasGroup _canvasGroup;

   // The list of all health unit objects
   private List<ShipHealthUnit> _healthUnits = new List<ShipHealthUnit>();

   // The last registered ship health value
   private float _lastHealth = 0;

   #endregion
}
