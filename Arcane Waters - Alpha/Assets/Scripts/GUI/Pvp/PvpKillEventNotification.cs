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
      _creationTime = Time.realtimeSinceStartup;
      StartCoroutine(nameof(CO_Update));
   }

   private IEnumerator CO_Update () {
      while (true) {
         if (Time.realtimeSinceStartup > _creationTime + lifetimeSeconds && !isPermanent) {
            StopCoroutine(nameof(CO_Update));
            this.transform.SetParent(null);
            Destroy(this.gameObject);
         }
         yield return new WaitForSeconds(1);
      }
   }

   #region Private Variables

   // The time of creation of the indicator in seconds since the start of the game
   private float _creationTime;

   #endregion
}
