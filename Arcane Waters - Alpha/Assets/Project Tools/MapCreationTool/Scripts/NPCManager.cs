﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MapCreationTool
{
   public class NPCManager : MonoBehaviour
   {
      public static event System.Action OnLoaded;
      public static NPCManager instance { get; private set; }

      private NPCData[] npcs = new NPCData[0];
      public Dictionary<int, NPCData> idToNpc { get; private set; }
      public bool loaded { get; private set; }

      private void Awake () {
         instance = this;
      }

      private IEnumerator Start () {
         yield return new WaitUntil(() => ImageManager.self != null);

         loadAllNpcs();
      }

      public Texture2D getTexture (int npcId) {
         if (!idToNpc.ContainsKey(npcId)) {
            Debug.LogWarning($"Unrecognized npc ID {npcId}.");
            return null;
         }

         NPCData data = idToNpc[npcId];
         return ImageManager.getTexture(data.spritePath);
      }

      public Texture2D getFirstNpcTexture () {
         return npcCount == 0 ? null : ImageManager.getTexture(npcs[0].spritePath);
      }

      public string[] formSelectionOptions () {
         return npcs.Select(n => n.npcId + ": " + n.name).ToArray();
      }

      public int npcCount
      {
         get { return npcs.Length; }
      }

      private void loadAllNpcs () {
         idToNpc = new Dictionary<int, NPCData>();

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<string> rawXMLData = DB_Main.getNPCXML();

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               foreach (string rawText in rawXMLData) {
                  TextAsset newTextAsset = new TextAsset(rawText);
                  NPCData npcData = Util.xmlLoad<NPCData>(newTextAsset);

                  // Save the NPC data in the memory cache
                  if (!idToNpc.ContainsKey(npcData.npcId)) {
                     idToNpc.Add(npcData.npcId, npcData);
                  }
               }

               instance.finishLoadingNpcs();
            });
         });
      }

      private void finishLoadingNpcs () {
         npcs = idToNpc.OrderBy(n => n.Key).Select(n => n.Value).ToArray();
         loaded = true;

         OnLoaded?.Invoke();
      }
   }
}
