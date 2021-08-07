using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;

public class PvpGameEndPanel : MonoBehaviour {
   #region Public Variables

   // Singleton instance
   public static PvpGameEndPanel self;

   // A reference to the canvas group containing the victory/defeat label
   public CanvasGroup victoryDefeatCanvasGroup;

   // References to the GameObjects that contain the 'Victory' and 'Defeat' labels
   public GameObject winText, loseText;

   // A reference to canvas groups for this panel
   public CanvasGroup canvasGroup, goHomeButtonCanvasGroup;

   #endregion

   private void Awake () {
      self = this;
   }

   public void show (bool playerWon) {
      if (isShowing()) {
         return;
      }

      canvasGroup.gameObject.SetActive(true);

      if (playerWon) {
         winText.SetActive(true);
         loseText.SetActive(false);
      } else {
         winText.SetActive(false);
         loseText.SetActive(true);
      }

      DOTween.Rewind(this);
      canvasGroup.DOFade(1.0f, FADE_DURATION);
      goHomeButtonCanvasGroup.DOFade(1.0f, FADE_DURATION);
      _isShowing = true;
   }

   public void hide () {
      if (!isShowing()) {
         return;
      }

      DOTween.Rewind(this);
      goHomeButtonCanvasGroup.DOFade(0.0f, FADE_DURATION);
      canvasGroup.DOFade(0.0f, FADE_DURATION).OnComplete(() => canvasGroup.gameObject.SetActive(false));
      _isShowing = false;
   }

   public void onPlayerLeftPvpGame () {
      BottomBar.self.disablePvpStatPanel();
      hide();
   }

   public bool isShowing() {
      return _isShowing;
   }

   public void onGoHomePressed () {
      if (Global.player) {
         PlayerShipEntity playerShip = Global.player.getPlayerShipEntity();
         if (playerShip) {
            RespawnScreen.self.respawnPlayerShipInTown(playerShip);
         }
      }
   }

   #region Private Variables

   // Whether or not this panel is currently showing
   private bool _isShowing = false;

   // How long this panel takes to fade in or out
   private const float FADE_DURATION = 0.5f;

   #endregion
}
