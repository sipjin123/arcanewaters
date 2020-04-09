using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using System;

public class Discovery : NetworkBehaviour
{
   #region Public Variables

   // The chances of spawning this discovery
   public float spawnChance;

   // The discovery data
   [SyncVar]
   public DiscoveryData data;

   // A unique ID for this discovery in the game
   public int id;

   // The instance ID of the area this discovery belongs to
   public int instanceId;

   #endregion

   private void Awake () {
      _spriteAnimation = GetComponent<SpriteAnimation>();
   }

   private void Start () {
      initializeDiscovery();
   }

   private void initializeDiscovery () {
      _spriteAnimation.sprites = ImageManager.getSprites(data.spriteUrl);

      GetComponent<SpriteRenderer>().enabled = true;
   }

   [Server]
   private void fetchDiscovery (int id) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         data = DB_Main.getDiscoveryById(id);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            initializeDiscovery();
         });
      });
   }

   [Server]
   public void assignDiscoveryId (int id) {
      fetchDiscovery(id);
   }

   #region Private Variables

   // The sprite animation
   private SpriteAnimation _spriteAnimation;

   #endregion
}
