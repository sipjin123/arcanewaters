using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PvpKillEventNotification : MonoBehaviour
{
   #region Public Variables

   // Reference to the containing panel
   public CanvasGroup canvasGroup;

   // Reference to the label that displays the name of the attacker
   public TMPro.TextMeshProUGUI txtAttacker;

   // Reference to the label that displays the name of the attacked entity
   public TMPro.TextMeshProUGUI txtAttacked;

   // The duration of this indicator
   public int lifetimeSeconds = 10;

   // Should the notification be permanent?
   public bool isPermanent;

   #endregion

   public void Start () {
      show();
   }

   private void startCountdown () {
      InvokeRepeating(nameof(updateNotification), 0, 1);
   }

   private void stopCountdown () {
      CancelInvoke(nameof(updateNotification));
   }

   private void updateNotification () {
      if (Time.realtimeSinceStartup > _creationTime + lifetimeSeconds && !isPermanent) {
         hide();
      }
   }

   private void show () {
      _creationTime = Time.realtimeSinceStartup;
      startCountdown();
   }

   private void hide () {
      stopCountdown();
      this.transform.SetParent(null);
      Destroy(this.gameObject);
   }

   #region Private Variables

   // The time of creation of the indicator in seconds since the start of the game
   private float _creationTime;

   #endregion
}
