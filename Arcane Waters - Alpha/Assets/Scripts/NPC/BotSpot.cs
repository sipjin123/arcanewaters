using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class BotSpot : MonoBehaviour {
   #region Public Variables

   // The prefab we want to spawn
   public BotShipEntity prefab;

   // The NPC Type for this spot
   public NPC.Type npcType;

   // The route that this bot should follow
   public Route route;

   // The Nation associated with this bot
   public Nation.Type nationType;

   // A custom max force that we can optionally specify
   public float maxForceOverride = 0f;

   #endregion

   #region Private Variables

   #endregion
}
