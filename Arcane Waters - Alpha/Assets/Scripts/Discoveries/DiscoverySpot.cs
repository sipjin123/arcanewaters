using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using System;

public class DiscoverySpot : NetworkBehaviour, IMapEditorDataReceiver
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
   }

   public void receiveData (DataField[] dataFields) {
      // Cache all the discoveries that could exist in this spot
      List<int> possibleDiscoveryIds = new List<int>();

      foreach (DataField field in dataFields) {
         if (field.k == DataField.DISCOVERY_SPAWN_CHANCE) {
            spawnChance = field.floatValue;
         } else if (field.k == DataField.POSSIBLE_DISCOVERY) {
            int id = field.intValue;

            // When ID is 0, it's because it wasn't assigned in the MapEditor
            if (id > 0) {
               possibleDiscoveryIds.Add(id);
            }
         }
      }

      // Rnadomly select one of the possible discoveries
      selectRandomDiscovery(possibleDiscoveryIds);
   }

   private void selectRandomDiscovery (List<int> discoveryIds) {
      if (discoveryIds.Count < 1) {
         Debug.LogWarning($"Discovery spot with ID {id} in instance {instanceId} contains no valid discoveries.");
         return;
      }

      int chosenId = UnityEngine.Random.Range(0, discoveryIds.Count);

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         data = DB_Main.getDiscoveryById(chosenId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            initializeDiscovery();
         });
      });
   }

   #region Private Variables

   // The sprite animation
   private SpriteAnimation _spriteAnimation;

   #endregion
}
