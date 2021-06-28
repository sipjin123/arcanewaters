using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class PvpStatusPanel : ClientMonoBehaviour
{
   #region Public Variables

   // The Silver Amount text
   public TextMeshProUGUI silverCountText;

   // The Silver Amount text (delta)
   public TextMeshProUGUI silverDeltaText;

   // The Amount of time the increase will remain on the screen
   public int silverDeltaTextDurationSeconds;

   // The container for the kill event indicators
   public VerticalLayoutGroup killEventNotificationList;

   // Reference to the prefab for the Kill Event Notification
   public GameObject KillEventNotificationPrefab;

   // Reference to the number of kill events
   public int KillEventsCountMax;

   // Self
   public static PvpStatusPanel self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
      this.gameObject.SetActive(false);
   }

   public void Start () {
      InvokeRepeating(nameof(CO_Update), 0, 1);
   }

   private void CO_Update () {
      // Check if the delta panel should be hidden
      if (isDeltaShowing()) {
         if (Time.realtimeSinceStartup > _deltaToggleStartTime + silverDeltaTextDurationSeconds) {
            hideDelta();
            // Update the displayed amount of silver
            int silverAfterChange = _silverBeforeChange + _currentSilverDelta;
            silverCountText.text = silverAfterChange.ToString();
            _silverBeforeChange = silverAfterChange;
            _currentSilverDelta = 0;
         }
      }
   }

   public void addSilver (int gain) {
      if (Global.player == null) {
         return;
      }

      Instance instance = Global.player.getInstance();
      if (instance == null || !instance.isPvP) {
         return;
      }

      silverCountText.text = _silverBeforeChange.ToString();
      _currentSilverDelta += gain;
      _deltaToggleStartTime = Time.realtimeSinceStartup;
      showDelta();
   }

   public bool isDeltaShowing () {
      return silverDeltaText.gameObject.activeSelf;
   }

   public void hideDelta () {
      silverDeltaText.gameObject.SetActive(false);
      silverDeltaText.text = string.Empty;
   }

   public void showDelta () {
      silverDeltaText.gameObject.SetActive(true);
      silverDeltaText.text = getDisplayStringForDelta();
   }

   public void toggle (bool show) {
      this.gameObject.SetActive(show);
      if (!show) {
         CancelInvoke(nameof(CO_Update));
      }
   }

   public bool isShowing () {
      return this.gameObject.activeSelf;
   }

   public void addKillEvent (string attackerName, Color attackerColor, string targetName, Color targetColor) {

      int displayedNotificationCount = killEventNotificationList.transform.childCount;

      PvpKillEventNotification latestNotification = null;

      if (displayedNotificationCount < KillEventsCountMax) {
         GameObject newIndicator = Instantiate(KillEventNotificationPrefab);
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
      if (displayedNotificationCount > Mathf.Max(KillEventsCountMax, 0)) {
         while (killEventNotificationList.transform.childCount > KillEventsCountMax) {
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
      return string.Empty;
   }

   private void updateKillEventNotification (PvpKillEventNotification indicator, string attackerName, Color attackerColor, string targetName, Color targetColor) {
      indicator.txtAttacker.text = attackerName;
      indicator.txtAttacker.color = attackerColor;
      indicator.txtAttacked.text = targetName;
      indicator.txtAttacked.color = targetColor;
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
