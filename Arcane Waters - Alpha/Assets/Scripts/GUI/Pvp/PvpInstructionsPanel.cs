using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using UnityEngine.EventSystems;

public class PvpInstructionsPanel : MonoBehaviour, IPointerClickHandler {
   #region Public Variables

   // Singleton instance
   public static PvpInstructionsPanel self;

   // References to the text objects that display each team's name, indexed by team type
   public List<TextMeshProUGUI> teamNames;

   // References to the game objects that contain the instructions for each game mode
   public List<GameObject> instructionsByGameMode;

   // A reference to the image that shows the current map layout
   public Image minimapImage;

   // References to the objects that hold the names for each team's members
   public List<GameObject> teamContainers;

   // A reference to the prefab used to spawn in a team member cell
   public PvpStatRow teamMemberCellPrefab;

   // A reference to the text that displays the current game status
   public TextMeshProUGUI gameStatusText;

   // A reference to the text that displays the current game mode
   public TextMeshProUGUI gameModeText;

   // A reference to the text that displays the current map name
   public TextMeshProUGUI mapNameText;

   // A reference to the canvas group for this panel
   public CanvasGroup panelCanvasGroup;

   // Whether this panel is currently showing
   public static bool isShowing = false;

   #endregion

   private void Awake () {
      self = this;
   }

   private void Start () {
      loadSprites();
   }

   public void init (List<Faction.Type> teamFactions, int instanceId) {
      string areaKey = Global.player.areaKey;
      PvpGameMode gameMode = AreaManager.self.getAreaPvpGameMode(areaKey);
      setInstructions(gameMode);
      gameModeText.text = PvpManager.getGameModeDisplayName(gameMode);
      mapNameText.text = Area.getName(areaKey);
      Area area = AreaManager.self.getArea(areaKey);
      setTeamNames(teamFactions);
      _localPlayerInstanceId = instanceId;
      minimapImage.sprite = ImageManager.getSprite(MINIMAPS_PATH + areaKey);
      show();
   }

   public void show () {
      panelCanvasGroup.alpha = 1.0f;
      panelCanvasGroup.interactable = true;
      panelCanvasGroup.blocksRaycasts = true;
      isShowing = true;
   }

   public void hide () {
      panelCanvasGroup.alpha = 0.0f;
      panelCanvasGroup.interactable = false;
      panelCanvasGroup.blocksRaycasts = false;
      isShowing = false;
   }

   private void loadSprites () {
      // Load the cell background sprites
      Sprite[] cellBackgrounds = ImageManager.getSprites(TEAM_CELL_BACKGROUND_SPRITE_PATH);
      _teamCellBackgrounds.Add(ImageManager.self.blankSprite);
      foreach (Sprite cellBackground in cellBackgrounds) {
         _teamCellBackgrounds.Add(cellBackground);
      }
   }

   private void setInstructions (PvpGameMode gameMode) {
      // Activate the instructions for this gamemode, disable the others
      for (int i = 0; i < instructionsByGameMode.Count; i++) {
         GameObject instructions = instructionsByGameMode[i];
         if (instructions != null) {
            PvpGameMode instructionsGameMode = (PvpGameMode) i;
            instructions.SetActive(instructionsGameMode == gameMode);
         }
      }
   }

   private void setTeamNames (List<Faction.Type> teamFactions) {
      for (int i = 1; i < teamNames.Count; i++) {
         TextMeshProUGUI teamNameText = teamNames[i];
         if (teamNameText != null) {
            teamNameText.text = teamFactions[i - 1].ToString();
         }
      }
   }

   public void updatePlayers (List<int> playerUserIds) {
      if (_updatePlayersCoroutine != null) {
         StopCoroutine(_updatePlayersCoroutine);
      }

      _updatePlayersCoroutine = StartCoroutine(CO_UpdatePlayers(playerUserIds));
   }

   private IEnumerator CO_UpdatePlayers (List<int> playerUserIds) {
      foreach (GameObject teamContainer in teamContainers) {
         if (teamContainer != null) {
            teamContainer.DestroyChildren();
         }
      }

      List<int> playersStillJoining = new List<int>();

      foreach (int userId in playerUserIds) {
         NetEntity playerEntity = EntityManager.self.getEntity(userId);

         // If the player's entity can't be found yet, add it to the list of players still joining
         if (!playerEntity || playerEntity.pvpTeam == PvpTeamType.None) {
            playersStillJoining.Add(userId);
            continue;
         }

         addPlayerCell(playerEntity, playerEntity.pvpTeam);
      }

      // After adding all connected players, wait for players still joining, and add them once they are connected
      foreach (int userId in playersStillJoining) {
         NetEntity playerEntity = EntityManager.self.getEntity(userId);

         while (!playerEntity || playerEntity.pvpTeam == PvpTeamType.None) {
            yield return null;
            playerEntity = EntityManager.self.getEntity(userId);
         }

         addPlayerCell(playerEntity, playerEntity.pvpTeam);
      }
   }

   private void addPlayerCell (NetEntity playerEntity, PvpTeamType teamType) {
      if (teamType == PvpTeamType.None) {
         return;
      }

      PvpStatRow statRow = Instantiate(teamMemberCellPrefab, teamContainers[(int) teamType].transform);
      statRow.portrait.updateLayers(playerEntity);
      statRow.userName.text = playerEntity.entityName;

      Sprite cellBackgroundSprite = _teamCellBackgrounds[(int) teamType];
      statRow.setCellBackgroundSprites(cellBackgroundSprite);
   }

   public void updateGameStatusMessage (string newMessage) {
      gameStatusText.text = "Game Status: " + newMessage;
   }

   public void OnPointerClick (PointerEventData eventData) {
      if (eventData.rawPointerPress == this.gameObject) {
         // If the mouse is over the input field zone, select it through the panel black background
         Panel.tryFocusChatInputField();
      }
   }

   #region Private Variables

   // The filepath where all the minimap images are stored
   private const string MINIMAPS_PATH = "Sprites/GUI/Pvp Arena/";

   // The path to the sprites used for the cell backgrounds for each team
   private const string TEAM_CELL_BACKGROUND_SPRITE_PATH = "Sprites/GUI/Scoreboard/pvp_scoreboard_cell";

   // Cached sprites for cell backgrounds for each team
   private List<Sprite> _teamCellBackgrounds = new List<Sprite>();

   // The instance id of the game the local player is in
   private int _localPlayerInstanceId;

   // A reference to the currently running coroutine that updates the list of players for this panel
   private Coroutine _updatePlayersCoroutine = null;

   #endregion
}
