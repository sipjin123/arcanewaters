using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class NPCManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static NPCManager self;

   #endregion

   public void Awake () {
      self = this;
   }

   public void storeNPC (NPC npc) {
      _npcs[npc.npcId] = npc;
   }

   public NPC getNPC (int npcId) {
      return _npcs[npcId];
   }

   #region Private Variables

   // Keeps track of the NPCs, based on their id
   protected Dictionary<int, NPC> _npcs = new Dictionary<int, NPC>();
      
   #endregion
}
