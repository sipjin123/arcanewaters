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

      // Populate the factions list with 'None'
      for (int i = 0; i < 3; i++) {
         _teamFactions.Add(Faction.Type.None);
      }
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
      // Wait for the player to be assigned a faction
      while (Global.player.faction == Faction.Type.None) {
         yield return null;
      }

      _currentGameMode = AreaManager.self.getAreaPvpGameMode(Global.player.areaKey);
      requestTeamFactions();
   }

   private void requestTeamFactions () {
      Global.player.rpc.Cmd_RequestPvpGameFactions(Global.player.instanceId);
   }

   public void onPlayerLeftPvpGame () {
      StopAllCoroutines();
      hide();
      _hasSetup = false;
   }

   public void updateScoreForTeam (int newScoreValue, PvpTeamType teamType) {
      teamScoreLabels[(int) teamType].text = newScoreValue.ToString();
   }

   public void assignFactionToTeam (PvpTeamType teamType, Faction.Type factionType) {
      _teamFactions[(int) teamType] = factionType;
      teamNameLabels[(int) teamType].text = factionType.ToString();

      if (isReadyToShow()) {
         setStatPanelFactions();

         // We currently only show the score panel for CTF games
         if (_currentGameMode == PvpGameMode.CaptureTheFlag) {
            show();
         }
      }  
   }

   private bool isReadyToShow () {
      return (_teamFactions[(int) PvpTeamType.A] != Faction.Type.None && _teamFactions[(int) PvpTeamType.B] != Faction.Type.None);
   }

   public Faction.Type getFactionForTeam (PvpTeamType teamType) {
      return _teamFactions[(int)teamType];
   }

   private void setStatPanelFactions () {
      PvpStatPanel.self.setTeamFactions(_teamFactions[(int) PvpTeamType.A], _teamFactions[(int) PvpTeamType.B]);
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

   // What faction each pvp team is associated with
   private List<Faction.Type> _teamFactions = new List<Faction.Type>();

   // What pvp gamemode the user is currently playing
   private PvpGameMode _currentGameMode = PvpGameMode.None;

   #endregion
}
