using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using System;

public class SeaStructure : SeaEntity, IMapEditorDataReceiver {
   #region Public Variables

   // An action that can be subscribed to, to be notified of a structure's death
   public System.Action<SeaStructure> onDeathAction;

   // The sea structure that will be able to be damaged after this dies
   public SeaStructure unlockAfterDeath;

   // Which lane this structure is in
   [HideInInspector]
   public PvpLane laneType = PvpLane.None;

   // An identifier for multiple structures in the same lane
   public int indexInLane = 0;

   // A reference to the sprite renderer used to render the main part of this structure (the building itself)
   public SpriteRenderer mainRenderer;

   // A reference to the sprite renderer used to render the island for this structure
   public SpriteRenderer islandRenderer;

   // A reference to the sprites for this sea structure, indexed by PvpTeamType
   public List<Sprite> spritesByTeam, damagedSpritesByTeam, destroyedSpritesByTeam;

   // A list of gameobjects that this will disable when it dies
   public List<GameObject> disableOnDeath;

   #endregion

   protected override void Awake () {
      base.Awake();

      _sinkOnDeath = false;

      // Sea structures will be invulnerable by default, and we will disable this when needed
      if (isServer) {
         setIsInvulnerable(true);
      }
   }

   protected override void Start () {
      base.Start();

      if (_isActivated) {
         onActivated();
      }

      setupSprites();
   }

   public override bool isSeaStructure () {
      return true;
   }

   public override void onDeath () {
      base.onDeath();
      onDeathAction?.Invoke(this);
      _deathTime = NetworkTime.time;
      unlockAfterDeath?.setIsInvulnerable(false);
   }

   protected override void Update () {      
      if (!_isActivated) {
         if (isDead()) {
            checkIntegrity();
         }
         return;
      }
      
      base.Update();

      if (isDead()) {
         double timeSinceDeath = NetworkTime.time - _deathTime;
         if (timeSinceDeath >= DYING_TIME_BEFORE_DISABLE) {
            this.enabled = false;
            
            if (disableOnDeath != null) {
               foreach (GameObject obj in disableOnDeath) {
                  obj.SetActive(false);
               }
            }
         }
      }

      checkIntegrity();
   }

   public void receiveData (DataField[] fields) {
      foreach (DataField field in fields) {
         if (field.k.CompareTo(DataField.PVP_LANE) == 0) {
            try {
               PvpLane pvpLane = (PvpLane) Enum.Parse(typeof(PvpLane), field.v);
               this.laneType = pvpLane;
            } catch {
               this.laneType = PvpLane.None;
            }
         }
         if (field.k.CompareTo(DataField.PVP_LANE_NUMBER) == 0) {
            if (field.tryGetIntValue(out int pvpLaneNum)) {
               indexInLane = pvpLaneNum;
            }
         }
         if (field.k.CompareTo(DataField.PVP_TEAM_TYPE) == 0) {
            try {
               PvpTeamType pvpTeam = (PvpTeamType) Enum.Parse(typeof(PvpTeamType), field.v);
               this.pvpTeam = pvpTeam;
            } catch {
               this.pvpTeam = PvpTeamType.None;
            }
         }
      }

      setupSprites();
   }

   public void setIsActivated (bool value) {
      bool oldValue = _isActivated;

      if (isServer) {
         _isActivated = value;
      }

      if (value != oldValue) {
         if (value) {
            onActivated();
         } else {
            onDeactivated();
         }
      }      
   }

   protected virtual void onActivated () {}

   protected virtual void onDeactivated () {}

   [ClientRpc]
   public void Rpc_SetIsActivated(bool value) {
      setIsActivated(value);
   }

   protected virtual void setupSprites () {
      Sprite newSprite = getSprite();
      if (newSprite != null) {
         mainRenderer.sprite = newSprite;

         string paletteDef = PvpManager.getStructurePaletteForTeam(pvpTeam);
         mainRenderer.GetComponent<RecoloredSprite>().recolor(paletteDef);
      }
   }

   protected Sprite getSprite () {
      int teamIndex = (int) pvpTeam;
      List<Sprite> teamSprites;

      if (isDead()) {
         teamSprites = destroyedSpritesByTeam;
      } else if (currentHealth < (maxHealth / 2)) {
         teamSprites = damagedSpritesByTeam;
      } else {
         teamSprites = spritesByTeam;
      }

      if (teamSprites == null || teamIndex >= teamSprites.Count) {
         D.warning("Couldn't find a sprite for this " + this.GetType().ToString());
         return null;
      }

      return teamSprites[teamIndex];
   }

   private void checkIntegrity () {
      StructureIntegrity newIntegrity;

      if (isDead()) {
         newIntegrity = StructureIntegrity.Destroyed;
      } else if (currentHealth < maxHealth / 2) {
         newIntegrity = StructureIntegrity.Damaged;
      } else {
         newIntegrity = StructureIntegrity.Healthy;
      }

      if (_structureIntegrity != newIntegrity) {
         _structureIntegrity = newIntegrity;
         setupSprites();
      } else {
         _structureIntegrity = newIntegrity;
      }
   }

   #region Private Variables

   // The timestamp for when this structure died
   private double _deathTime = 0.0f;

   // How long this structure will wait after dying, before being disabled
   private const double DYING_TIME_BEFORE_DISABLE = 2.0;

   // Whether this sea structure is active, and should perform its behaviour
   [SyncVar]
   protected bool _isActivated = false;

   // An enum representing how damaged the sea structure is, and which sprite it should show as a result
   private StructureIntegrity _structureIntegrity = StructureIntegrity.Healthy;

   // An enum to represent how damaged a sea structure is
   private enum StructureIntegrity { Healthy = 0, Damaged = 1, Destroyed = 2 }

   #endregion
}

