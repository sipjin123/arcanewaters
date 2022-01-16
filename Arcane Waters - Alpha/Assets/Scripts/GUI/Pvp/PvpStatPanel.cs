using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using UnityEngine.EventSystems;

public class PvpStatPanel : Panel {
   #region Public Variables

   // The object holding all pvp stat rows
   public Transform pvpStatRowHolder;

   // Prefab of the pvp stat row
   public PvpStatRow pvpStatRowPrefab;

   // A reference to the prefab used to create character portraits
   public GameObject characterPortraitPrefab;

   // Self
   public static PvpStatPanel self;

   // Icon of the silver currency awarded for kills
   public Sprite silverIcon;

   // Icon of the silver currency awarded for assists
   public Sprite assistSilverIcon;

   // A reference to the title text
   public TextMeshProUGUI title;

   // A list of gameobjects that are only enabled on game end
   public List<GameObject> gameEndObjects;

   // A list of gameobjects that display the team scores for the current game, only used in some game modes, indexed by team type
   public List<GameObject> teamScoreContainers;

   // References to the banners that display victory or defeat
   public GameObject victoryBanner, defeatBanner;

   // References to the icons that indicate what quality reward a team was awarded
   public List<Image> teamRewardIcons;

   // References to the images that show gem icons
   public List<GameObject> teamGemIconContainers;

   // References to the text that indicates what quantity reward a team was awarded
   public List<TextMeshProUGUI> teamGoldRewardTexts;

   // References to the text that indicates how many gems a team was awarded
   public List<TextMeshProUGUI> teamGemRewardTexts;

   // References to the text that indicates what faction a team represents
   public List<TextMeshProUGUI> teamNames;

   // The labels for each pvp team's score, indexed by PvpTeamType
   public List<TextMeshProUGUI> teamScoreLabels;

   // A reference to the text component displaying the timer for the game
   public TextMeshProUGUI timerText;

   // References to the layout groups containing the portraits for each team
   public GridLayoutGroup teamAPortraits, teamBPortraits;

   // The prefab used for visually separating teams
   public GameObject separatorPrefab;

   // The nameplate color of each team in the pvp score panel
   public Color aTeamColor, bTeamColor, cTeamColor, dTeamColor;

   // Whether the current pvp game has ended
   public bool isGameEnded = false;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;

      // Populate the factions list with 'None'
      for (int i = 0; i < 3; i++) {
         _teamFactions.Add(Faction.Type.None);
      }
   }

   public override void Start () {
      base.Start();

      loadSprites();
      gameObject.SetActive(false);
   }

   private void loadSprites () {
      // Load the cell background sprites
      Sprite[] cellBackgrounds = ImageManager.getSprites(TEAM_CELL_BACKGROUND_SPRITE_PATH);
      _teamCellBackgrounds.Add(ImageManager.self.blankSprite);
      foreach (Sprite cellBackground in cellBackgrounds) {
         _teamCellBackgrounds.Add(cellBackground);
      }

      // Load the reward icon sprites
      Sprite[] rewardIcons = ImageManager.getSprites(REWARD_ICON_SPRITE_PATH);
      foreach (Sprite rewardIcon in rewardIcons) {
         _rewardIcons.Add(rewardIcon);
      }
   }

   public void setTitle (string newTitle) {
      if (title != null) {
         title.text = newTitle;
      }
   }

   public void populatePvpPanelData (GameStatsData pvpStatData) {
      pvpStatRowHolder.gameObject.DestroyChildren();

      List<GameStats> teamAStats = new List<GameStats>();
      List<GameStats> teamBStats = new List<GameStats>();

      // Add team A stats first, and then team B stats, so scoreboard is separated by team
      foreach (GameStats playerStat in pvpStatData.stats) {
         if (playerStat.playerTeam == (int) PvpTeamType.A) {
            teamAStats.Add(playerStat);
         } else if (playerStat.playerTeam == (int) PvpTeamType.B) {
            teamBStats.Add(playerStat);
         }
      }

      foreach (GameStats playerStat in teamAStats) {
         addStatRow(playerStat);
      }
      Instantiate(separatorPrefab, pvpStatRowHolder);
      foreach (GameStats playerStat in teamBStats) {
         addStatRow(playerStat);
      }

      setupEndGamePortraits(pvpStatData);
   }

   private void addStatRow (GameStats rowStats) {
      PvpStatRow statRow = Instantiate(pvpStatRowPrefab, pvpStatRowHolder);
      statRow.kills.text = rowStats.PvpPlayerKills.ToString();
      statRow.assists.text = rowStats.playerAssists.ToString();
      statRow.deaths.text = rowStats.PvpPlayerDeaths.ToString();
      statRow.monsterKills.text = rowStats.playerMonsterKills.ToString();
      statRow.buildingsDestroyed.text = rowStats.playerStructuresDestroyed.ToString();
      statRow.shipKills.text = rowStats.playerShipKills.ToString();
      statRow.silver.text = rowStats.silver.ToString();
      statRow.userName.text = rowStats.playerName.ToString();
      statRow.flagCount.text = rowStats.flagCount.ToString();

      statRow.pvpTeamType = (PvpTeamType) rowStats.playerTeam;
      switch (statRow.pvpTeamType) {
         case PvpTeamType.A:
            foreach (Image colorBox in statRow.colorBoxes) {
               colorBox.color = aTeamColor;
            }
            break;
         case PvpTeamType.B:
            foreach (Image colorBox in statRow.colorBoxes) {
               colorBox.color = bTeamColor;
            }
            break;
         case PvpTeamType.C:
            foreach (Image colorBox in statRow.colorBoxes) {
               colorBox.color = cTeamColor;
            }
            break;
         case PvpTeamType.D:
            foreach (Image colorBox in statRow.colorBoxes) {
               colorBox.color = dTeamColor;
            }
            break;
      }

      NetEntity playerEntity = EntityManager.self.getEntity(rowStats.userId);
      if (playerEntity) {
         statRow.portrait.updateLayers(playerEntity);
      }

      Sprite cellBackgroundSprite = _teamCellBackgrounds[rowStats.playerTeam];
      statRow.setCellBackgroundSprites(cellBackgroundSprite);
   }

   public void updateDisplayMode (string areaKey, bool isGameEnded, bool isVictory, PvpTeamType winningTeam, int gemReward = 0) {
      PvpGameMode gameMode = AreaManager.self.getAreaPvpGameMode(areaKey);
      
      if (_requestTeamFactions != null) {
         PvpManager.self.StopCoroutine(_requestTeamFactions);
      }

      this.isGameEnded = isGameEnded;

      _requestTeamFactions = PvpManager.self.StartCoroutine(CO_RequestTeamFactions());

      // Only enable the score container for CTF games currently
      foreach (GameObject scoreContainer in teamScoreContainers) {
         if (scoreContainer != null) {
            scoreContainer.SetActive(gameMode == PvpGameMode.CaptureTheFlag);
         }
      }
      
      foreach (GameObject gameEndObject in gameEndObjects) {
         gameEndObject.SetActive(isGameEnded);
      }

      List<PvpTeamType> allTeams = new List<PvpTeamType>() { PvpTeamType.A, PvpTeamType.B };
      List<PvpTeamType> losingTeams = allTeams.Clone();
      losingTeams.Remove(winningTeam);
      List<PvpTeamType> winningTeams = new List<PvpTeamType>() { winningTeam };

      if (isGameEnded) {
         // Update team reward texts and icons
         foreach (PvpTeamType team in allTeams) {
            bool isWinningTeam = winningTeams.Contains(team);
            teamRewardIcons[(int) team].sprite = (isWinningTeam) ? _rewardIcons[0] : _rewardIcons[1];

            // If the game was valid for rewards, populate rewards
            if (gemReward > 0) {
               teamGoldRewardTexts[(int) team].text = (isWinningTeam) ? PvpGame.WINNER_GOLD.ToString() : PvpGame.LOSER_GOLD.ToString();

               if (isWinningTeam) {
                  teamGemRewardTexts[(int) team].text = gemReward.ToString();
               } else {
                  teamGemRewardTexts[(int) team].gameObject.SetActive(false);
               }

               teamGemIconContainers[(int) team].gameObject.SetActive(isWinningTeam);
            
            // If the game wasn't valid for rewards, don't display gems, and show 0 reward.
            } else {
               teamGoldRewardTexts[(int) team].text = "0";
               teamGemRewardTexts[(int) team].gameObject.SetActive(false);
               teamGemIconContainers[(int) team].gameObject.SetActive(false);
            }
         }

         // Enable the victory / defeat banner
         GameObject bannerToEnable = (isVictory) ? victoryBanner : defeatBanner;
         GameObject bannerToDisable = (isVictory) ? defeatBanner : victoryBanner;

         bannerToEnable.SetActive(true);
         bannerToDisable.SetActive(false);

         CancelInvoke(nameof(updateTimerText));
      }
   }

   public void setGameStartTime (float gameStartTime) {
      _gameStartTime = gameStartTime;
      CancelInvoke(nameof(updateTimerText));

      // Make the timer update on the second
      float timeSinceGameStart = (float) NetworkTime.time - gameStartTime;
      float remainder = timeSinceGameStart % 1.0f;
      InvokeRepeating(nameof(updateTimerText), remainder, 1.0f);
   }

   private IEnumerator CO_RequestTeamFactions () {
      while (Global.player.faction == Faction.Type.None) {
         yield return null;
      }

      Global.player.rpc.Cmd_RequestPvpGameFactions(Global.player.instanceId);
   }

   public void assignFactionToTeam (PvpTeamType teamType, Faction.Type factionType) {
      _teamFactions[(int) teamType] = factionType;
      teamNames[(int) teamType].text = factionType.ToString();
   }

   private void setupEndGamePortraits (GameStatsData data) {
      List<int> teamAUserIds= new List<int>();
      List<int> teamBUserIds = new List<int>();

      // Sort the user ids into teams
      foreach (GameStats playerStat in data.stats) {
         if (playerStat.playerTeam == (int) PvpTeamType.A) {
            teamAUserIds.Add(playerStat.userId);
         } else if (playerStat.playerTeam == (int) PvpTeamType.B) {
            teamBUserIds.Add(playerStat.userId);
         }
      }

      int teamAPlayerCount = teamAUserIds.Count;
      int teamBPlayerCount = teamBUserIds.Count;
      int localPlayerUserId = Global.player.userId;
      PvpTeamType localPlayerTeam = Global.player.pvpTeam;

      // Clear out any old portraits
      teamAPortraits.gameObject.DestroyChildren();
      teamBPortraits.gameObject.DestroyChildren();

      // Wait until all other portraits have been added, and then add the local player, to ensure they're on top
      if (teamAUserIds.Contains(localPlayerUserId)) {
         teamAUserIds.Remove(localPlayerUserId);
      } else if (teamBUserIds.Contains(localPlayerUserId)) {
         teamBUserIds.Remove(localPlayerUserId);
      }

      foreach (int userId in teamAUserIds) {
         addPortraitToRewards(userId, teamAPortraits);
      }

      foreach (int userId in teamBUserIds) {
         addPortraitToRewards(userId, teamBPortraits);
      }

      // Add local player portrait
      if (localPlayerTeam == PvpTeamType.A) {
         addPortraitToRewards(localPlayerUserId, teamAPortraits);
      } else if (localPlayerTeam == PvpTeamType.B) {
         addPortraitToRewards(localPlayerUserId, teamBPortraits);
      }

      // Update spacing values on grid layout groups, to fit the number of players
      Vector2 teamASpacing = teamAPortraits.spacing;
      teamASpacing.x = calculatePortraitSpacing(teamAPlayerCount);
      teamAPortraits.spacing = teamASpacing;

      Vector2 teamBSpacing = teamBPortraits.spacing;
      teamBSpacing.x = calculatePortraitSpacing(teamBPlayerCount);
      teamBPortraits.spacing = teamBSpacing;

   }

   private void addPortraitToRewards (int userId, GridLayoutGroup layoutGroup) {
      NetEntity playerEntity = EntityManager.self.getEntity(userId);
      if (playerEntity) {
         GameObject newPortraitObject = Instantiate(characterPortraitPrefab, layoutGroup.transform);
         CharacterPortrait newPortrait = newPortraitObject.GetComponentInChildren<CharacterPortrait>();
         newPortrait.updateLayers(playerEntity);
      }
   }

   private int calculatePortraitSpacing (int numPlayers) {
      // Calculate the best spacing to use in a grid layout group of portraits, for this number of players
      const int availableSpace = 148;
      const int spaceConstant = -32;

      int spacing;

      // Avoid dividing by 0
      if (numPlayers == 1) {
         spacing = -2;
      } else {
         spacing = spaceConstant + Mathf.FloorToInt(availableSpace / (numPlayers - 1));
      }

      spacing = Mathf.Clamp(spacing, -24, -2);
      return spacing;
   }

   public void onGoHomePressed () {
      if (Global.player) {
         PlayerShipEntity playerShip = Global.player.getPlayerShipEntity();
         if (playerShip) {
            RespawnScreen.self.respawnPlayerShipInTown(playerShip);
         }
      }

      CancelInvoke(nameof(updateTimerText));
   }

   public void onPlayerJoinedPvpGame () {
      updateDisplayMode(Global.player.areaKey, false, false, PvpTeamType.None);
   }

   public void onPlayerLeftPvpGame () {
      close();
      CancelInvoke(nameof(updateTimerText));
   }

   public void updateScoreForTeam (int newScoreValue, PvpTeamType teamType) {
      teamScoreLabels[(int) teamType].text = newScoreValue.ToString();
   }

   public void setTimerText (string newTimerText) {
      timerText.text = newTimerText;
   }

   private void updateTimerText () {
      float timeSinceGameStart = (float)NetworkTime.time - _gameStartTime;
      int secondsSinceGameStart = Mathf.RoundToInt(timeSinceGameStart);
      string minutes = Mathf.Floor(secondsSinceGameStart / 60).ToString("00");
      string seconds = Mathf.Floor(secondsSinceGameStart % 60).ToString("00");
      string newTimerText = minutes + ":" + seconds;

      timerText.text = newTimerText;
   }

   public override void OnPointerClick (PointerEventData eventData) {
      if (eventData.rawPointerPress == this.gameObject) {
         // If the mouse is over the input field zone, select it through the panel black background
         tryFocusChatInputField();
      }
   }

   #region Private Variables

   // Cached sprites for cell backgrounds for each team
   private List<Sprite> _teamCellBackgrounds = new List<Sprite>();

   // Cached sprites for the reward icons to be displayed for the winning and losing teams
   private List<Sprite> _rewardIcons = new List<Sprite>();

   // The stored faction types, indexed by pvp team type
   private List<Faction.Type> _teamFactions = new List<Faction.Type>();

   // A reference to the currently running coroutine for requesting team factions
   private Coroutine _requestTeamFactions = null;

   // The path to the sprites used for the cell backgrounds for each team
   private const string TEAM_CELL_BACKGROUND_SPRITE_PATH = "Sprites/GUI/Scoreboard/pvp_scoreboard_cell";

   // The path to the sprites used to show the quality of reward for each team
   private const string REWARD_ICON_SPRITE_PATH = "Sprites/GUI/Scoreboard/pvp_scoreboard_chest_icon";

   // The time at which the game the player is in started
   private float _gameStartTime;

   #endregion
}
