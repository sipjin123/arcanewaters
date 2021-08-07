using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using DG.Tweening;

public class PvpScorePanel : MonoBehaviour {
   #region Public Variables

   // Singleton instance
   public static PvpScorePanel self;

   // The labels for each pvp team's name, indexed by PvpTeamType
   public List<TextMeshProUGUI> teamNameLabels;

   // The labels for each pvp team's score, indexed by PvpTeamType
   public List<TextMeshProUGUI> teamScoreLabels;

   #endregion

   private void Awake () {
      _panelCanvasGroup = GetComponent<CanvasGroup>();

      self = this;
   }

   public void show () {
      if (isShowing()) {
         return;
      }

      DOTween.Rewind(this);
      _panelCanvasGroup.DOFade(1.0f, FADE_DURATION);
      _isShowing = true;
   }

   public void hide () {
      if (!isShowing()) {
         return;
      }

      DOTween.Rewind(this);
      _panelCanvasGroup.DOFade(0.0f, FADE_DURATION);
      _isShowing = false;
   }

   public bool isShowing () {
      return _isShowing;
   }

   public void onPlayerJoinedPvpGame () {
      if (_hasSetup) {
         return;
      }

      _hasSetup = true;
      StartCoroutine(CO_OnPlayerJoinedPvpGame());
   }

   private IEnumerator CO_OnPlayerJoinedPvpGame () {
      // Wait for the player to be assigned a team
      while (Global.player.pvpTeam == PvpTeamType.None) {
         yield return null;
      }

      // TODO: Find a better way to detect if the game is CTF or not, could maybe make a new map special type? Or add a new variable 'game type' to pvp maps
      bool isCaptureTheFlag = false;
      foreach (PvpCaptureTargetHolder targetHolder in FindObjectsOfType<PvpCaptureTargetHolder>()) {
         if (targetHolder.instanceId == Global.player.instanceId) {
            isCaptureTheFlag = true;
            break;
         }
      }

      // We currently only show the score panel for capture the flag games
      if (isCaptureTheFlag) {
         show();
      }
   }

   public void onPlayerLeftPvpGame () {
      StopAllCoroutines();
      hide();
      _hasSetup = false;
   }

   public void updateScoreForTeam (int newScoreValue, PvpTeamType teamType) {
      teamScoreLabels[(int) teamType].text = newScoreValue.ToString();
   }

   #region Private Variables

   // Whether this panel is currently showing
   private bool _isShowing = false;

   // How long this panel takes to fade in or out
   private const float FADE_DURATION = 0.5f;

   // The canvas group for this panel
   private CanvasGroup _panelCanvasGroup;

   // Whether this panel has been setup
   private bool _hasSetup = false;

   #endregion
}
