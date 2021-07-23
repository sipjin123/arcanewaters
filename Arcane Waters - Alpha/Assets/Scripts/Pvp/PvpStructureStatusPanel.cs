using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;
using System.Linq;

public class PvpStructureStatusPanel : MonoBehaviour {
   #region Public Variables

   // Singleton instance
   public static PvpStructureStatusPanel self;

   // References to the images showing the structures, indexed by lane type
   public List<Image> structureImages;

   // References to the health bars showing the health of the structures, indexed by lane type
   public List<Image> healthBars;

   // References to the images showing what lane a structure is in
   public List<Image> laneImages;

   // References to the icons for each structure, for each team, indexed by SeaStructure.Type
   public List<Sprite> structureIconsA, structureIconsB;

   // References to the icon assets showing what lane a structure is in
   public List<Sprite> laneIcons;

   // A gradient showing how the health bar color should change with value
   public Gradient healthBarColor;

   // A reference to the canvas group for this panel
   public CanvasGroup panelCanvasGroup;

   #endregion

   private void Awake () {
      self = this;

      // Populate list, so we can access by index
      for (int i = 0; i < 4; i++) {
         _trackedStructures.Add(null);
      }
   }

   private void Update () {
      if (!isShowing()) {
         return;
      }

      updateDisplayedStructures();
      updateHealthValues();
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

      _playerTeam = Global.player.pvpTeam;

      // Wait a small delay, to ensure all structures are created on the client
      yield return new WaitForSeconds(3.0f);

      detectStructures();
      show();
   }

   public void onPlayerLeftPvpGame () {
      StopAllCoroutines();
      hide();
      _hasSetup = false;
   }

   private void detectStructures () {
      clearStructureLists();

      // Count how many towers we have first, to assist in registering towers
      List<SeaStructure> allStructures = FindObjectsOfType<SeaStructure>().ToList();
      List<SeaStructure> instanceStructures = new List<SeaStructure>();
      foreach(SeaStructure structure in allStructures) {
         if (structure.instanceId == Global.player.instanceId) {
            instanceStructures.Add(structure);
         }
      }

      int numTowers = 0;
      foreach (SeaStructure structure in instanceStructures) {
         if (structure is PvpTower) {
            numTowers++;
         }
      }

      // We are currently assuming we'll have X towers per lane, plus one base tower each. So our total is 6 * X + 2.
      // Check if we have an appropriate number of towers
      if ((numTowers - 2) % 6 == 0) {
         _towersPerLane = (numTowers - 2) / 6;

         // Fill the lists with null for now, so we can access elements directly
         int structuresPerLane = 3 + _towersPerLane;
         for (int i = 0; i < structuresPerLane; i++) {
            _topStructuresA.Add(null);
            _midStructuresA.Add(null);
            _botStructuresA.Add(null);
            _topStructuresB.Add(null);
            _midStructuresB.Add(null);
            _botStructuresB.Add(null);
         }
      } else {
         D.error("This map doesn't have the correct number of towers. It has " + numTowers + ". Aborting structure detection.");
         return;
      }

      // Register the sea structures in their appropriate places
      foreach (SeaStructure structure in instanceStructures) {
         if (structure is PvpTower) {
            registerTower((PvpTower) structure);
         } else if (structure is PvpShipyard) {
            registerShipyard((PvpShipyard) structure);
         } else if (structure is PvpBase) {
            registerBase((PvpBase) structure);
         }

         _allStructures.Add(structure);
      }
   }

   private void updateDisplayedStructures () {
      SeaStructure topStructure = _trackedStructures[(int) PvpLane.Top];
      SeaStructure midStructure = _trackedStructures[(int) PvpLane.Mid];
      SeaStructure botStructure = _trackedStructures[(int) PvpLane.Bot];

      List<Sprite> structureIcons = (_playerTeam == PvpTeamType.A) ? structureIconsA : structureIconsB;

      // Check if we need to change the displayed structure
      if (topStructure == null || topStructure.isDead()) {
         SeaStructure newStructure = getStructureToDisplay(_playerTeam, PvpLane.Top);
         if (newStructure != null) {
            _trackedStructures[(int) PvpLane.Top] = newStructure;
            SeaStructure.Type structureType = _trackedStructures[(int) PvpLane.Top].getStructureType();
            structureImages[(int) PvpLane.Top].sprite = structureIcons[(int) structureType];
            laneImages[(int) PvpLane.Top].sprite = laneIcons[(int) newStructure.laneType];
            
            // Recolor the image
            string paletteDef = PvpManager.getStructurePaletteForTeam(_playerTeam);
            structureImages[(int) PvpLane.Top].GetComponent<RecoloredSprite>().recolor(paletteDef);
         }
      }

      // Check if we need to change the displayed structure
      if (midStructure == null || midStructure.isDead()) {
         SeaStructure newStructure = getStructureToDisplay(_playerTeam, PvpLane.Mid);
         if (newStructure != null) {
            _trackedStructures[(int) PvpLane.Mid] = newStructure;
            SeaStructure.Type structureType = _trackedStructures[(int) PvpLane.Mid].getStructureType();
            structureImages[(int) PvpLane.Mid].sprite = structureIcons[(int) structureType];
            laneImages[(int) PvpLane.Mid].sprite = laneIcons[(int) newStructure.laneType];

            // Recolor the image
            string paletteDef = PvpManager.getStructurePaletteForTeam(_playerTeam);
            structureImages[(int) PvpLane.Mid].GetComponent<RecoloredSprite>().recolor(paletteDef);
         }
      }

      // Check if we need to change the displayed structure
      if (botStructure == null || botStructure.isDead()) {
         SeaStructure newStructure = getStructureToDisplay(_playerTeam, PvpLane.Bot);
         if (newStructure != null) {
            _trackedStructures[(int) PvpLane.Bot] = newStructure;
            SeaStructure.Type structureType = _trackedStructures[(int) PvpLane.Bot].getStructureType();
            structureImages[(int) PvpLane.Bot].sprite = structureIcons[(int) structureType];
            laneImages[(int) PvpLane.Bot].sprite = laneIcons[(int) newStructure.laneType];

            // Recolor the image
            string paletteDef = PvpManager.getStructurePaletteForTeam(_playerTeam);
            structureImages[(int) PvpLane.Bot].GetComponent<RecoloredSprite>().recolor(paletteDef);
         }
      }
   }

   private void updateHealthValues () {
      SeaStructure topStructure = _trackedStructures[(int) PvpLane.Top];
      SeaStructure midStructure = _trackedStructures[(int) PvpLane.Mid];
      SeaStructure botStructure = _trackedStructures[(int) PvpLane.Bot];
      
      if (topStructure) {
         updateStructureHealthValues(topStructure, PvpLane.Top);
      }

      if (midStructure) {
         updateStructureHealthValues(midStructure, PvpLane.Mid);
      }

      if (botStructure) {
         updateStructureHealthValues(botStructure, PvpLane.Bot);
      }
   }

   private void updateStructureHealthValues (SeaStructure structure, PvpLane laneType) {
      float fillAmount = Mathf.Clamp01((float)structure.currentHealth / structure.maxHealth);
      int laneIndex = (int) laneType;
      healthBars[laneIndex].fillAmount = fillAmount;
      healthBars[laneIndex].color = healthBarColor.Evaluate(fillAmount);
   }

   public void setNewStructure (int newStructureNetId) {
      NetEntity entity = EntityManager.self.getEntity(newStructureNetId);
      if (entity && entity is SeaStructure) {
         setNewStructure(entity as SeaStructure);
      }
   }

   public void setNewStructure (SeaStructure structure) {
      int laneIndex = (int) structure.laneType;
      _trackedStructures[laneIndex] = structure;

      Sprite newStructureSprite = getSpriteForStructure(structure);
      if (newStructureSprite) {
         structureImages[laneIndex].sprite = newStructureSprite;
      }

      updateHealthValues();
   }

   private Sprite getSpriteForStructure (SeaStructure structure) {
      int iconIndex = (int) structure.getStructureType();

      if (structure.pvpTeam == PvpTeamType.A) {
         return structureIconsA[iconIndex];
      } else if (structure.pvpTeam == PvpTeamType.B) {
         return structureIconsB[iconIndex];
      }

      return null;
   }

   public void show () {
      if (isShowing()) {
         return;
      }
      DOTween.Rewind(this);
      panelCanvasGroup.DOFade(1.0f, FADE_DURATION);
      _isShowing = true;
   }

   public void hide () {
      if (!isShowing()) {
         return;
      }

      DOTween.Rewind(this);
      panelCanvasGroup.DOFade(0.0f, FADE_DURATION);
      _isShowing = false;
   }

   public bool isShowing () {
      return _isShowing;
   }

   private SeaStructure getStructureToDisplay (PvpTeamType team, PvpLane lane) {
      // The sea structures are ordered from the front of the lane to the back, so we just need to return the first living structure we find
      foreach (SeaStructure structure in getStructures(team, lane)) {
         if (!structure.isDead()) {
            return structure;
         }
      }

      return null;
   }

   private List<SeaStructure> getStructures (PvpTeamType team, PvpLane lane) {
      if (team == PvpTeamType.A) {
         if (lane == PvpLane.Top) {
            return _topStructuresA;
         } else if (lane == PvpLane.Mid) {
            return _midStructuresA;
         } else if (lane == PvpLane.Bot) {
            return _botStructuresA;
         }
      } else if (team == PvpTeamType.B) {
         if (lane == PvpLane.Top) {
            return _topStructuresB;
         } else if (lane == PvpLane.Mid) {
            return _midStructuresB;
         } else if (lane == PvpLane.Bot) {
            return _botStructuresB;
         }
      }
      return null;
   }

   private void clearStructureLists () {
      _topStructuresA.Clear();
      _midStructuresA.Clear();
      _botStructuresA.Clear();
      _topStructuresB.Clear();
      _midStructuresB.Clear();
      _botStructuresB.Clear();
      _allStructures.Clear();
   }

   private void registerTower (PvpTower tower) {
      int indexInList = 0;

      if (tower.laneType == PvpLane.Base) {
         indexInList = _towersPerLane + 1;

         // Base towers are registered in all lanes, for bot ship targeting and structure invulnerability purposes
         getStructures(tower.pvpTeam, PvpLane.Top)[indexInList] = tower;
         getStructures(tower.pvpTeam, PvpLane.Mid)[indexInList] = tower;
         getStructures(tower.pvpTeam, PvpLane.Bot)[indexInList] = tower;
      } else {
         indexInList = tower.indexInLane;
         getStructures(tower.pvpTeam, tower.laneType)[indexInList] = tower;
      }
   }

   private void registerShipyard (PvpShipyard shipyard) {
      int indexInList = _towersPerLane;
      getStructures(shipyard.pvpTeam, shipyard.laneType)[indexInList] = shipyard;
   }

   private void registerBase (PvpBase pvpBase) {
      int indexInList = _towersPerLane + 2;

      // Bases are registered in all lanes, for bot ship targeting and structure invulnerability purposes
      getStructures(pvpBase.pvpTeam, PvpLane.Top)[indexInList] = pvpBase;
      getStructures(pvpBase.pvpTeam, PvpLane.Mid)[indexInList] = pvpBase;
      getStructures(pvpBase.pvpTeam, PvpLane.Bot)[indexInList] = pvpBase;
   }

   #region Private Variables

   // A list of references to the structures we are displaying the status of
   private List<SeaStructure> _trackedStructures = new List<SeaStructure>();

   // How long this panel takes to fade in or out
   private const float FADE_DURATION = 0.5f;

   // A list of references to the structures in each lane, for each team, for the current pvp game
   private List<SeaStructure> _topStructuresA = new List<SeaStructure>();
   private List<SeaStructure> _midStructuresA = new List<SeaStructure>();
   private List<SeaStructure> _botStructuresA = new List<SeaStructure>();
   private List<SeaStructure> _topStructuresB = new List<SeaStructure>();
   private List<SeaStructure> _midStructuresB = new List<SeaStructure>();
   private List<SeaStructure> _botStructuresB = new List<SeaStructure>();

   // A list of references to all the structures in the current pvp game
   private List<SeaStructure> _allStructures = new List<SeaStructure>();

   // How many towers are in each lane, for the current pvp game
   private int _towersPerLane = 0;

   // The pvp team that the player is currently on
   private PvpTeamType _playerTeam;

   // Whether this panel is currently showing
   private bool _isShowing = false;

   // Whether this panel has been setup
   private bool _hasSetup = false;

   #endregion
}
