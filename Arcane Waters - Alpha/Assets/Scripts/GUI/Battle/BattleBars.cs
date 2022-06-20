using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using System.Linq;

public class BattleBars : MonoBehaviour {
   #region Public Variables

   // The text component displaying the battler's name
   public TextMeshProUGUI nameText;
   
   // The filled image used to display the health bar for local players, and non-player-controlled battlers
   public Image localHealthBar;

   // The filled image used to display the health bar for remote players
   public Image remoteHealthBar;

   // The filled image used to display the timer bar for local players
   public Image timerBar;

   // Determines if this was initialized
   public bool isInitialized;

   // Disables the script update
   public bool isDisabled;

   // Prefab reference of the buff icon
   public BuffIcon buffIconPrefab;

   // The parent of the buff icons to be created
   public Transform buffIconParent;

   // The current debuff icons generated for this battler
   public List<BuffIcon> currentDebuffIcons = new List<BuffIcon>();

   // The component that holds the outline/outsides of the name's UI
   public TextMeshProUGUI nameTextOutside;

   // The component that holds the insides of the name's UI
   public TextMeshProUGUI nameTextInside;

   // The width of the outline
   [Range(0.0f, 1.0f)]
   public float nameOutlineWidth;

   // The color the outline
   public Color32 nameOutlineColor;

   // The color of the label
   public Color32 nameColor;

   // The color the outline for the local player
   public Color32 nameOutlineColorLocalPlayer;

   // The color of the label for the local Player
   public Color32 nameColorLocalPlayer;

   // Holds the health bar, name, ap
   public GameObject infoHolder;

   #endregion

   private void Awake () {
      // Look up components
      _battler = GetComponentInParent<Battler>();
      _canvasGroup = GetComponent<CanvasGroup>();
   }

   private void Start () {
      // Note our starting position
      _startPos = _battler.transform.position;

      // If we are not a monster battler we enable the stance graphics
      if (_battler.battlerType == BattlerType.AIEnemyControlled) {
         nameText.enabled = false;
      }

      // Assign the health bar image to update
      if (_battler.battlerType == BattlerType.PlayerControlled) {
         _healthBar = (_battler.isLocalBattler()) ? localHealthBar : remoteHealthBar;

         // For the local player, show the smaller battlebars with the timer
         if (_battler.isLocalBattler()) {
            remoteHealthBar.transform.parent.gameObject.SetActive(false);
            localHealthBar.transform.parent.gameObject.SetActive(true);
         }
      } else {
         _healthBar = localHealthBar;
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
      if (_battler == null || _battler.player == null || _battler.battle == null) {
         _canvasGroup.alpha = 0f;
         return;
      }

      if (_battler.isDead()) {
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
      _healthBar.fillAmount = (((float) _battler.displayedHealth - _battler.damageTicks) / _battler.getStartingHealth());

      // Hide our bars while we're doing an attack
      if ((_battler != BattleSelectionManager.self.selectedBattler && !_battler.isLocalBattler()) || _battler.isAttacking) {                                              
         if (_battler.isJumping) {
            _canvasGroup.alpha -= 5f * Time.deltaTime;
            if (_canvasGroup.alpha < 0.1f) {
               _canvasGroup.alpha = 0;
            }
            _canvasGroup.alpha = Mathf.Clamp(_canvasGroup.alpha, 0f, 1f);
         }
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
            if (expiredStatList[0] == Status.Type.Stunned && !_battler.canCastAbility()) {
               _battler.setBattlerCanCastAbility(true);
            }
            removeDebuffStatus(expiredStatList[0]);
            expiredStatList.RemoveAt(0);
         }
      }
   }

   private void addDebuffStatus (Status.Type statType) {
      if (!currentDebuffIcons.Exists(_ => _.statusType == statType)) {
         BuffIcon currentIcon = Instantiate(buffIconPrefab, buffIconParent);
         currentIcon.statusType = statType;

         if (currentIcon.statusSpritePair.Find(_ => _.statusType == statType) != null) {
            currentIcon.buffIcon.sprite = currentIcon.statusSpritePair.Find(_ => _.statusType == statType).statusSprite;
            currentDebuffIcons.Add(currentIcon);
            currentIcon.simpleAnim.enabled = true;
         } else {
            D.debug("Cannot find buff type {" + statType + "}");
         }
      }
   }

   private void removeDebuffStatus (Status.Type statType) {
      if (currentDebuffIcons.Exists(_ => _.statusType == statType)) {
         BuffIcon currentIcon = currentDebuffIcons.Find(_ => _.statusType == statType);
         currentDebuffIcons.Remove(currentIcon);
         Destroy(currentIcon.gameObject);
      }
   }

   public void toggleDisplay (bool isShown, bool showName = true) {
      transform.gameObject.SetActive(isShown);
      infoHolder.SetActive(isShown);
      nameText.gameObject.SetActive(showName);
   }

   #region Private Variables

   // Our associated Battler
   protected Battler _battler;

   // Our Canvas Group
   protected CanvasGroup _canvasGroup;

   // The position we started at
   protected Vector2 _startPos;

   // The filled image we will be updating in real time to display health
   protected Image _healthBar;
      
   #endregion
}
