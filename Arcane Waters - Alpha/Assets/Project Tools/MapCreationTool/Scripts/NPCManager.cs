using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MapCreationTool
{
   public class NPCManager : MonoBehaviour
   {
      public static NPCManager instance { get; private set; }

      private NPCData[] npcs;
      public Dictionary<int, NPCData> idToNpc { get; private set; }
      public Dictionary<string, Sprite> npcBodySprites { get; private set; }
      public bool loaded { get; private set; }


      private void Awake () {
         instance = this;
      }

      private IEnumerator Start () {
         yield return new WaitUntil(() => ImageManager.self != null);

         loadAllDataFiles();
         loadBodies();
      }

      public Texture2D getTexture(int npcId) {
         if (!idToNpc.ContainsKey(npcId)) {
            Debug.LogError("Unrecognized npc ID.");
            return null;
         }
            
         NPCData data = idToNpc[npcId];
         return ImageManager.getTexture(data.spritePath);
      }

      public string[] formSelectionOptions() {
         return npcs.Select(n => n.npcId + ": " + n.name).ToArray();
      }

      private void loadAllDataFiles () {
         // Build the path to the folder containing the NPC data XML files
         string directoryPath = Path.Combine(Application.dataPath, "Data", NPCToolManager.FOLDER_PATH);

         if (!Directory.Exists(directoryPath)) {
            DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
         }

         // Get the list of XML files in the folder
         string[] fileNames = ToolsUtil.getFileNamesInFolder(directoryPath, "*.xml");

         idToNpc = new Dictionary<int, NPCData>();
         // Iterate over the files
         for (int i = 0; i < fileNames.Length; i++) {
            // Build the path to a single file
            string filePath = Path.Combine(directoryPath, fileNames[i]);

            // Read and deserialize the file
            NPCData npcData = ToolsUtil.xmlLoad<NPCData>(filePath);

            // Save the NPC data in the memory cache
            if (!idToNpc.ContainsKey(npcData.npcId)) {
               idToNpc.Add(npcData.npcId, npcData);
            }
         }

         npcs = idToNpc.OrderBy(n => n.Key).Select(n => n.Value).ToArray();
         loaded = true;
      }

      private void loadBodies() {
         string spritePath = "Assets/Sprites/NPCs/Bodies/";
         npcBodySprites = new Dictionary<string, Sprite>();
         List<ImageManager.ImageData> spriteFiles = ImageManager.getSpritesInDirectory(spritePath);

         foreach (ImageManager.ImageData imgData in spriteFiles) {
            Sprite sourceSprite = imgData.sprite;
            npcBodySprites.Add(imgData.imagePath, sourceSprite);
         }
      }

   }

}
