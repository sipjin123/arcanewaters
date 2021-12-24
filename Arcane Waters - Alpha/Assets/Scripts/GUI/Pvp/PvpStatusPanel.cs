using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using UnityEngine.Events;

public class PvpStatusPanel : ClientMonoBehaviour
{
   #region Public Variables

   // The Reference to the CanvasGroup
   public CanvasGroup canvasGroup;

   // The Silver Amount text
   public TextMeshProUGUI silverCountText;

   // The Silver Amount text (delta)
   public TextMeshProUGUI silverDeltaText;

   // The Amount of time the increase will remain on the screen
   public int silverDeltaTextDurationSeconds;

   // The container for the kill event indicators
   public VerticalLayoutGroup killEventNotificationList;

   // Reference to the prefab for the Kill Event Notification
   public GameObject killEventNotificationPrefab;

   // Reference to the number of kill events
   public int killEventsCountMax;

   // The color of the delta indicator when the delta is positive
   public Color positiveDeltaColor;

   // The color of the delta indicator when the delta is negative
   public Color negativeDeltaColor;

   // Self
   public static PvpStatusPanel self;

   // Event that invokes when currency is increased
   public AddSilverEvent silverAddedEvent = new AddSilverEvent();

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
   }

   public void Start () {
      startVisibilityCheck();
   }

   private bool isInstanceValid (Instance instance) {
      if (instance == null) {
         return false;
      }

      return instance.isPvP || instance.isVoyage || instance.isLeague || VoyageManager.isTreasureSiteArea(instance.areaKey);
   }

   private void updateVisibilityCheck () {
      if (Global.player == null) {
         hide();
         return;
      }

      if (!isInstanceValid(Global.player.getInstance()) && !Global.player.areaKey.Contains(Area.TUTORIAL_AREA)) {
         hide();
         return;
      }

      show();

      if (isDeltaShowing()) {
         if (Time.realtimeSinceStartup > _deltaToggleStartTime + silverDeltaTextDurationSeconds) {
            hideDelta();

            // Update the displayed amount of silver
            int silverAfterChange = _silverBeforeChange + _currentSilverDelta;
            silverCountText.text = silverAfterChange.ToString();
            _silverBeforeChange = silverAfterChange;
            silverAddedEvent.Invoke(silverAfterChange);
            _currentSilverDelta = 0;
         }
      }
   }

   public void addSilver (int gain) {
      if (Global.player == null) {
         return;
      }

      if (!isInstanceValid(Global.player.getInstance()) && !Global.player.areaKey.Contains(Area.TUTORIAL_AREA)) {
         return;
      }

      silverCountText.text = _silverBeforeChange.ToString();

      _currentSilverDelta += gain;
      _deltaToggleStartTime = Time.realtimeSinceStartup;
      showDelta();
   }

   public void showDelta () {
      silverDeltaText.gameObject.SetActive(true);
      silverDeltaText.text = getDisplayStringForDelta();

      // Adjust the color the delta
      updateDeltaColor();
   }

   public void hideDelta () {
      silverDeltaText.gameObject.SetActive(false);
      silverDeltaText.text = string.Empty;
   }

   private void updateDeltaColor () {
      if (silverDeltaText == null) {
         return;
      }

      if (_currentSilverDelta >= 0) {
         silverDeltaText.color = positiveDeltaColor;
      } else {
         silverDeltaText.color = negativeDeltaColor;
      }
   }

   public bool isDeltaShowing () {
      return silverDeltaText.gameObject.activeSelf;
   }

   public void show () {
      canvasGroup.alpha = 1;
   }

   public void hide () {
      canvasGroup.alpha = 0;
      if (Global.player != null) {
         Global.player.rpc.Cmd_RequestResetPvpSilverPanel();
         Global.player.rpc.Cmd_RequestResetVoyageRatingPoints();
      }
   }

   public void reset (int currentSilverAmount) {
      _deltaToggleStartTime = 0;
      _silverBeforeChange = currentSilverAmount;
      _currentSilverDelta = 0;
      if (silverCountText != null) {
         silverCountText.text = _silverBeforeChange.ToString();
      }
      if (silverDeltaText != null) {
         silverDeltaText.text = string.Empty;
      }
   }

   public bool isShowing () {
      return this.gameObject.activeSelf && canvasGroup.alpha > 0;
   }

   private void startVisibilityCheck () {
      InvokeRepeating(nameof(updateVisibilityCheck), 0, 1);
   }

   public void addKillEvent (string attackerName, Color attackerColor, string targetName, Color targetColor) {

      int displayedNotificationCount = killEventNotificationList.transform.childCount;

      PvpKillEventNotification latestNotification = null;

      if (displayedNotificationCount < killEventsCountMax) {
         GameObject newIndicator = Instantiate(killEventNotificationPrefab);
         latestNotification = newIndicator.GetComponent<PvpKillEventNotification>();
         latestNotification.transform.SetParent(killEventNotificationList.transform);
      } else {
         if (displayedNotificationCount > 0) {
            Transform t = killEventNotificationList.transform.GetChild(displayedNotificationCount - 1);
            latestNotification = t.GetComponent<PvpKillEventNotification>();
         }
      }
      latestNotification.transform.SetAsFirstSibling();

      // Remove the indicators in excess
      displayedNotificationCount = killEventNotificationList.transform.childCount;
      if (displayedNotificationCount > Mathf.Max(killEventsCountMax, 0)) {
         while (killEventNotificationList.transform.childCount > killEventsCountMax) {
            var child = killEventNotificationList.transform.GetChild(killEventNotificationList.transform.childCount - 1);
            child.transform.SetParent(null);
            Destroy(child.gameObject);
         }
      }

      if (latestNotification != null) {
         updateKillEventNotification(latestNotification, attackerName, attackerColor, targetName, targetColor);
      }
   }

   private string getDisplayStringForDelta () {
      if (_currentSilverDelta > 0) {
         return "+" + _currentSilverDelta.ToString();
      }
      else if (_currentSilverDelta < 0) {
         return _currentSilverDelta.ToString();
      }

      return string.Empty;
   }

   private void updateKillEventNotification (PvpKillEventNotification indicator, string attackerName, Color attackerColor, string targetName, Color targetColor) {
      indicator.txtAttacker.text = attackerName;
      indicator.txtAttacker.color = attackerColor;
      indicator.txtAttacked.text = targetName;
      indicator.txtAttacked.color = targetColor;
   }

   public int getCurrentSilver () {
      return _silverBeforeChange;
   }

   #region Private Variables

   // The moment in time, where the delta panel was shown
   private float _deltaToggleStartTime;

   // The amount of silver displayed before the delta was updated
   private int _silverBeforeChange = 0;

   // The current silver delta
   private int _currentSilverDelta;

   #endregion
}

public class AddSilverEvent : UnityEvent<int> {
}