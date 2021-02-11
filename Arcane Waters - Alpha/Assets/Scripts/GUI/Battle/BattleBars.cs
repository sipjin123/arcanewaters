using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class BattleBars : MonoBehaviour {
   #region Public Variables

   // Our components
   public TextMeshProUGUI nameText;
   public Image healthBar;
   public Image timerBar;
   public GameObject timerContainer;

   // Determines if this was initialized
   public bool isInitialized;

   // Disables the script update
   public bool isDisabled;

   #endregion

   private void Start () {
      // Look up components
      _battler = GetComponentInParent<Battler>();
      _canvasGroup = GetComponent<CanvasGroup>();

      // Only show the timer for our own player
      timerContainer.SetActive(_battler.player == Global.player);

      // Note our starting position
      _startPos = _battler.transform.position;

      // If we are not a monster battler we enable the stance graphics
      if (_battler.battlerType == BattlerType.AIEnemyControlled) {
         nameText.enabled = false;
      }
      isInitialized = true;
   }

   // ZERONEV-Comment: setting all these values in update are really inneficient
   // and can cause a lot of trouble later on, cause these values that are set are basically permanent.
   // For example, I think it is important to only change the health bar whenever we have a health change.
   // Having callbacks for whenever we have a health change for a battler
   private void Update () {
      if (!isInitialized || isDisabled) {
         return;
      }

      // Can't do anything until we have our battler
      if (_battler == null || _battler.player == null) {
         _canvasGroup.alpha = 0f;
         return;
      }

      // Set our text values
      if (_battler.battlerType == BattlerType.AIEnemyControlled) {
         nameText.text = _battler.getBattlerData().enemyName;
      } else {
         nameText.text = _battler.player.entityName + "";
      }

      // Figure out how full our bars should be
      timerBar.fillAmount = _battler.getActionTimerPercent();
      healthBar.fillAmount = ((float) _battler.displayedHealth / _battler.getStartingHealth());

      // Hide our bars while we're doing an attack
      _canvasGroup.alpha += _battler.isJumping ? -5f * Time.deltaTime : 5f * Time.deltaTime;
      _canvasGroup.alpha = Mathf.Clamp(_canvasGroup.alpha, 0f, 1f);
   }

   #region Private Variables

   // Our associated Battler
   protected Battler _battler;

   // Our Canvas Group
   protected CanvasGroup _canvasGroup;

   // The position we started at
   protected Vector2 _startPos;
      
   #endregion
}
