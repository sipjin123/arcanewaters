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
   [HideInInspector][SyncVar]
   public PvpLane laneType = PvpLane.None;

   // An identifier for multiple structures in the same lane
   [SyncVar]
   public int indexInLane = 0;

   // A reference to the sprite renderer used to render the main part of this structure (the building itself)
   public SpriteRenderer mainRenderer;

   // A reference to the sprite renderer used to render the island for this structure
   public SpriteRenderer islandRenderer;

   // A list of gameobjects that this will disable when it dies
   public List<GameObject> disableOnDeath;

   // An enum to describe a type of sea structure
   public enum Type { None = 0, Tower = 1, Shipyard = 2, Base = 3 }

   #endregion

   protected override void Awake () {
      base.Awake();

      if (isServer) {
         sinkOnDeath = false;

         // Sea structures will be invulnerable by default, and we will disable this when needed
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

   [Server]
   protected override int getRewardedXP () {
      switch (getStructureType()) {
         case Type.Tower:
            return 50;
         case Type.Shipyard:
            return 70;
         case Type.Base:
            return 100;
         default:
            return 50;
      }
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

   protected virtual void onActivated () {
      StartCoroutine(CO_SetupFactionSprites());
   }

   protected virtual void onDeactivated () {}

   [ClientRpc]
   public void Rpc_SetIsActivated(bool value) {
      setIsActivated(value);
   }

   public virtual void setupSprites () {
      if (!mainRenderer || Util.isBatch()) {
         return;
      }

      Sprite newSprite = getSprite();
      if (newSprite != null) {
         mainRenderer.sprite = newSprite;

         string paletteDef = PvpManager.getStructurePaletteForTeam(pvpTeam);
         RecoloredSprite recoloredSprite = mainRenderer.GetComponent<RecoloredSprite>();
         if (recoloredSprite) {
            recoloredSprite.recolor(paletteDef);
         }
      }
   }

   private IEnumerator CO_SetupFactionSprites () {
      while (faction == Faction.Type.None) {
         yield return null;
      }

      setupSprites();
   }

   protected virtual Sprite getSprite () {
      return ImageManager.self.blankSprite;
   }

   protected virtual int getSpriteIndex () {
      int factionIndex = (int) faction;
      int integrityIndex = (int) _structureIntegrity;

      return (factionIndex * 3) + integrityIndex;
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

   public Type getStructureType () {
      if (this is PvpTower) {
         return Type.Tower;
      } else if (this is PvpShipyard) {
         return Type.Shipyard;
      } else if (this is PvpBase) {
         return Type.Base;
      } else {
         return Type.None;
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
   protected StructureIntegrity _structureIntegrity = StructureIntegrity.Healthy;

   // An enum to represent how damaged a sea structure is
   protected enum StructureIntegrity { Healthy = 0, Damaged = 1, Destroyed = 2 }

   #endregion
}

