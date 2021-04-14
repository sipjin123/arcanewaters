using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using System.Linq;

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

   // Prefab reference of the debuff icon
   public BuffIcon debuffIconPrefab;

   // The parent of the debuff icons to be created
   public Transform debuffIconParent;

   // The current debuff icons generated for this battler
   public List<BuffIcon> currentDebuffIcons = new List<BuffIcon>();

   #endregion

   private void Awake () {
      // Look up components
      _battler = GetComponentInParent<Battler>();
      _canvasGroup = GetComponent<CanvasGroup>();
   }

   private void Start () {
      // Only show the timer for our own player
      timerContainer.SetActive(_battler.player == Global.player);

      // Note our starting position
      _startPos = _battler.transform.position;

      // If we are not a monster battler we enable the stance graphics
      if (_battler.battlerType == BattlerType.AIEnemyControlled) {
         nameText.enabled = false;
      }
      isInitialized = true;

      // Check debuff stats of the battler and update the visuals accordingly
      InvokeRepeating(nameof(checkForDebuffStats), 1, 1);
   }

   private void checkForDebuffStats () {
      if (_battler.selectedBattleBar != null) {
         List<Status.Type> statList = _battler.debuffList.Keys.ToList();
         _battler.selectedBattleBar.checkDebuffStats(statList);
      }
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
      if (_battler != BattleSelectionManager.self.selectedBattler) {
         _canvasGroup.alpha += _battler.isJumping ? -5f * Time.deltaTime : 5f * Time.deltaTime;
         _canvasGroup.alpha = Mathf.Clamp(_canvasGroup.alpha, 0f, 1f);
      } else {
         _canvasGroup.alpha = 1;
      }
   }

   private void checkDebuffStats (List<Status.Type> statList) {
      // Setup existing debuff stats here
      foreach (Status.Type stat in statList) {
         addDebuffStatus(stat);
      }

      if (currentDebuffIcons.Count > 0) {
         // Add expired stats in a list
         List<Status.Type> expiredStatList = new List<Status.Type>();
         foreach (BuffIcon stat in currentDebuffIcons) {
            if (!statList.Contains(stat.statusType) || statList.Count < 1) {
               expiredStatList.Add(stat.statusType);
            }
         }

         // Remove all expired stats from the GUI Canvas
         for (int i = 0; i < expiredStatList.Count; i++) {
            removeDebuffStatus(expiredStatList[0]);
            expiredStatList.RemoveAt(0);
         }
      }
   }

   private void addDebuffStatus (Status.Type statType) {
      if (!currentDebuffIcons.Exists(_ => _.statusType == statType)) {
         BuffIcon currentIcon = Instantiate(debuffIconPrefab, debuffIconParent);
         currentIcon.statusType = statType;
         currentIcon.buffIcon.sprite = currentIcon.buffSpritePair.Find(_ => _.statusType == statType).statusSprite;
         currentDebuffIcons.Add(currentIcon);
      }
   }

   private void removeDebuffStatus (Status.Type statType) {
      if (currentDebuffIcons.Exists(_ => _.statusType == statType)) {
         BuffIcon currentIcon = currentDebuffIcons.Find(_ => _.statusType == statType);
         currentDebuffIcons.Remove(currentIcon);
         Destroy(currentIcon.gameObject);
      }
   }

   public void toggleDisplay (bool isShown) {
      if (_canvasGroup != null) {
         _canvasGroup.alpha = isShown ? 1 : 0;
      }
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
