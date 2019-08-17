using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class HorrorEntity : SeaMonsterEntity
{
   #region Public Variables

   // The Type of NPC that is sailing this ship
   [SyncVar]
   public NPC.Type npcType;

   // The Name of the NPC that is sailing this ship
   [SyncVar]
   public string npcName;

   #endregion

   protected override void Start () {
      base.Start();

      // Note our spawn position
      _spawnPos = this.transform.position;

      // Set our name
      this.nameText.text = "[" + getNameForFaction() + "]";
      NPC.setNameColor(nameText, npcType);
   }

   protected override void Update () {
      base.Update();

      // If we're dead and have finished sinking, remove the ship
      if (isServer && isDead() && spritesContainer.transform.localPosition.y < -.25f) {
         InstanceManager.self.removeEntityFromInstance(this);

         // Destroy the object
         NetworkServer.Destroy(this.gameObject);
      }
   }

   protected override void FixedUpdate () {
      base.FixedUpdate();
   }

   protected string getNameForFaction () {
      switch (this.faction) {
         case Faction.Type.Pirates:
            return "Pirate";
         case Faction.Type.Privateers:
            return "Privateer";
         case Faction.Type.Merchants:
            return "Merchant";
         case Faction.Type.Cartographers:
         case Faction.Type.Naturalists:
            return "Explorer";
         default:
            return "Sailor";
      }
   }

   [ClientRpc]
   public void Rpc_CallAnimation (TentacleAnimType tentacleAnim) {
      switch(tentacleAnim) {
         case TentacleAnimType.Die:
            animator.Play("Die");
            break;
      }
   }

   #region Private Variables

   // The position we spawned at
   protected Vector2 _spawnPos;

   #endregion
}
