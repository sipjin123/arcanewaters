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

   #endregion

   protected override void Start () {
      base.Start();

      if (_isActivated) {
         onActivated();
      }
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
         return;
      }
      
      base.Update();

      if (isDead()) {
         double timeSinceDeath = NetworkTime.time - _deathTime;
         if (timeSinceDeath >= DYING_TIME_BEFORE_DISABLE) {
            gameObject.SetActive(false);
         }
      }
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
   }

   public void setIsActivated (bool value) {
      bool oldValue = _isActivated;
      _isActivated = value;

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

   #region Private Variables

   // The timestamp for when this structure died
   private double _deathTime = 0.0f;

   // How long this structure will wait after dying, before being disabled
   private const double DYING_TIME_BEFORE_DISABLE = 2.0;

   // Whether this sea structure is active, and should perform its behaviour
   protected bool _isActivated = false;

   #endregion
}
