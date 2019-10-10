using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class NPCToolManager : MonoBehaviour {

   #region Public Variables

   // Self
   public static NPCToolManager self;

   // The NPC Selection Screen
   public NPCSelectionScreen npcSelectionScreen;

   #endregion

   public void Awake () {
      self = this;
      loadAllDataFiles();
   }

   public void Start () {
      npcSelectionScreen.updatePanelWithNPCs(_npcData);
      npcSelectionScreen.show();
   }

   public void loadAllDataFiles () {
      // Build the path to the folder containing the NPC data XML files
      string directoryPath = Path.Combine(Application.dataPath, "Data", "NPC");

      // Get the list of XML files in the folder
      string[] fileNames = ToolsUtil.getFileNamesInFolder(directoryPath, "*.xml");

      // Iterate over the files
      for (int i = 0; i < fileNames.Length; i++) {
         // Build the path to a single file
         string filePath = Path.Combine(directoryPath, fileNames[i]);

         // Read and deserialize the file
         NPCData npcData = ToolsUtil.xmlLoad<NPCData>(filePath);

         // Save the NPC data in the memory cache
         _npcData.Add(npcData.npcId, npcData);
      }
   }

   public void createNewNPC(int npcId) {
      // Verify that the NPC id is free
      if (!isNPCIdFree(npcId)) {
         Debug.LogError(string.Format("A NPC with the same ID {0} already exists.", npcId.ToString()));
         return;
      }

      // Create an empty npc
      NPCData npcData = new NPCData(npcId, "", "", "", "", "", "", "", "", "NPC", Faction.Type.Neutral,
         Specialty.Type.Adventurer, true, true, -1, new List<Quest>() { },
         new List<NPCGiftData>() { });

      // Add the data to the dictionary
      _npcData.Add(npcData.npcId, npcData);

      // Save the data to the file
      saveNPCDataToFile(npcData);

      // Update the list of NPCs
      npcSelectionScreen.updatePanelWithNPCs(_npcData);
   }

   public void updateNPCData (NPCData data) {
      // Verify that the npc exist
      if (!_npcData.ContainsKey(data.npcId)) {
         Debug.LogError(string.Format("The NPC {0} does not exist.", data.npcId));
         return;
      }

      // Delete the old file
      deleteNPCDataFile(_npcData[data.npcId]);

      // Update the data in the dictionary
      _npcData[data.npcId] = data;

      // Create a new file with the data
      saveNPCDataToFile(data);

      // Refresh the list of npcs
      npcSelectionScreen.updatePanelWithNPCs(_npcData);
   }

   public bool isNPCIdFree (int npcId) {
      bool free = true;
      foreach (NPCData npcData in _npcData.Values) {
         if (npcData.npcId == npcId) {
            free = false;
            break;
         }
      }
      return free;
   }

   public NPCData getNPCData(int npcId) {
      if (_npcData.ContainsKey(npcId)) {
         return _npcData[npcId];
      } else {
         Debug.LogError(string.Format("The NPC {0} does not exist.", npcId));
         return null;
      }
   }

   private void deleteNPCDataFile (NPCData data) {
      // Build the file name
      string fileName = data.npcId.ToString() + "_" + data.name;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", "NPC", fileName + ".xml");

      // Save the file
      ToolsUtil.deleteFile(path);
   }

   private void saveNPCDataToFile (NPCData data) {
      // Build the file name
      string fileName = data.npcId.ToString() + "_" + data.name;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", "NPC", fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   #region Private Variables

   // The cached NPC data
   private Dictionary<int, NPCData> _npcData = new Dictionary<int, NPCData>();

   #endregion
}
