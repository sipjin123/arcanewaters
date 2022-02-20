using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Text;
using UnityEngine.Events;

public class CountdownScreen : MonoBehaviour
{
   #region Public Variables

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // The cancel button
   public Button cancelButton;

   // The action to perform when the countdown ends
   public UnityEvent onCountdownEndEvent;

   // The action to perform every time the countdown goes down
   public UnityEvent onCountdownStep;

   // The custom text displayed above the remaining seconds
   public Text customText;

   // The number of seconds remaining
   public Text secondsText;

   // The remaining seconds
   public float seconds = 0f;

   #endregion

   public void Update () {
      if (Global.player == null) {
         hide();
         return;
      }

      // Check if the player is moving or in combat
      if (_timePassed > DELAY_BEFORE_INPUT_INTERRUPTION &&
         (Global.player.isMoving() || (!Global.player.isDead() && Global.player.hasAnyCombat()))) {
         
         if (isShowing()) {
            cancelButton.onClick.Invoke();
         }

         // Stop the countdown
         hide();
      }

      // Decrease the remaining seconds
      _timePassed += Time.deltaTime;

      // Update the displayed seconds
      secondsText.text = Mathf.CeilToInt(seconds - _timePassed).ToString();

      if (_previousTime != _currentTime) {
         if (onCountdownStep != null) {
            onCountdownStep.Invoke();
         }
      }

      _previousTime = _currentTime;
      _currentTime = Mathf.CeilToInt(seconds - _timePassed);

      // Check if the end of the countdown has been reached
      if (_timePassed >= seconds) {
         onCountdownEndEvent.Invoke();
         hide();
      }
   }

   public void show () {
      this.gameObject.SetActive(true);
      this.canvasGroup.alpha = 1f;
      this.canvasGroup.blocksRaycasts = true;
      this.canvasGroup.interactable = true;
      _timePassed = 0f;
      _currentTime = 0;
      _previousTime = 0;
   }

   public void hide () {
      StopAllCoroutines();
      this.canvasGroup.alpha = 0f;
      this.canvasGroup.blocksRaycasts = false;
      this.canvasGroup.interactable = false;
      this.gameObject.SetActive(false);

      // Reset the Cancel Button
      toggleCancelButton(true);

      onCountdownEndEvent.RemoveAllListeners();
   }

   public bool isShowing () {
      return gameObject.activeSelf;
   }

   public void toggleCancelButton (bool show) {
      if (cancelButton == null) {
         return;
      }

      cancelButton.gameObject.SetActive(show);
   }

   #region Private Variables

   // The number of seconds the panel will show, even if the user is pressing keys
   private static float DELAY_BEFORE_INPUT_INTERRUPTION = 2f;

   // The time since the countdown started
   private float _timePassed = 0f;

   // The current time
   private float _currentTime = 0;

   // The previous time
   private float _previousTime = 0;

   #endregion
}