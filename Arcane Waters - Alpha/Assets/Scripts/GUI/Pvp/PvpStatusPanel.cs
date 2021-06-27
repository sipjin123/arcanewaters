using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class PvpStatusPanel : ClientMonoBehaviour
{
   #region Public Variables

   // Our Canvas Group
   public CanvasGroup canvasGroup;

   // The Silver Amount text
   public TextMeshProUGUI silverCountText;

   // The Silver Amount text (delta)
   public TextMeshProUGUI silverDeltaText;

   // The Amount of time the increase will remain on the screen
   public int silverDeltaTextDurationSeconds;

   // Self
   public static PvpStatusPanel self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
   }

   public void Start () {
      // Hide the panel by default
      hide();
      StartCoroutine(nameof(CO_Update));
   }

   private IEnumerator CO_Update () {
      while (true) {
         hide();
         if (Global.player != null) {
            // Get the current instance
            Instance instance = Global.player.getInstance();
            if (instance != null && instance.isPvP) {
               // Show the panel
               show();
               int delta = Global.lastUserSilver - _silverBeforeChange;
               silverCountText.text = _silverBeforeChange.ToString();
               if (_currentSilverDelta != delta){
                  _deltaToggleStartTime = Time.realtimeSinceStartup;
                  _currentSilverDelta = delta;
                  if (delta > 0) {
                     showDelta(delta);
                  }
               }
               // Check if the delta panel should be hidden
               if (isDeltaShowing()) {
                  if (Time.realtimeSinceStartup > _deltaToggleStartTime + silverDeltaTextDurationSeconds) {
                     hideDelta();
                     // Update the displayed amount of silver
                     silverCountText.text = Global.lastUserSilver.ToString();
                     _silverBeforeChange = Global.lastUserSilver;
                  }
               }
            }
         }
         yield return new WaitForSeconds(1);
      }
   }

   public bool isDeltaShowing () {
      return silverDeltaText.gameObject.activeSelf;
   }

   public void hideDelta () {
      silverDeltaText.gameObject.SetActive(false);
      silverDeltaText.text = string.Empty;
   }

   public void showDelta (int delta) {
      silverDeltaText.gameObject.SetActive(true);
      silverDeltaText.text = getDeltaDisplayString(delta);
   }

   private string getDeltaDisplayString (int delta) {
      if (delta > 0) {
         return "+" + delta.ToString();
      }
      return string.Empty;
   }

   public void show () {
      if (this.canvasGroup.alpha < 1f) {
         this.canvasGroup.alpha = 1f;
         this.canvasGroup.blocksRaycasts = true;
         this.canvasGroup.interactable = true;
      }
   }

   public void hide () {
      if (this.canvasGroup.alpha > 0f) {
         this.canvasGroup.alpha = 0f;
         this.canvasGroup.blocksRaycasts = false;
         this.canvasGroup.interactable = false;
      }
   }

   public bool isShowing () {
      return this.gameObject.activeSelf && canvasGroup.alpha > 0f;
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
