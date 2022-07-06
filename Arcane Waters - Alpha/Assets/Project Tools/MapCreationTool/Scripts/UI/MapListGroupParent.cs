using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using MapCreationTool.Serialization;
using System;
using System.Linq;

namespace MapCreationTool
{
   public class MapListGroupParent : MonoBehaviour
   {
      #region Public Variables

      // Symbols for expansion button
      public const string DOWN_TRIANGLE = "▼";
      public const string LEFT_TRIANGLE = "▶";

      // Entry of a single map
      public MapListEntry mapEntryPref;

      // Parent of the child maps
      public GameObject entryParent;

      // Button for expanding/collapsing the group
      public Button arrowButton;

      // The label of the name of the group
      public Text nameText;

      // Dropdown by which the biome is changed for the entire group
      public Dropdown biomeDropdown;

      // The button by which the group is duplicated
      public Button duplicateButton;

      // The button by which latest versions of the group are published
      public Button publishButton;

      // The button by which the group is deleted
      public Button deleteButton;

      #endregion

      private void Awake () {
         // Set options for biome dropdown
         biomeDropdown.options.Clear();
         biomeDropdown.options.Add(new Dropdown.OptionData { text = "Set Biome:" });
         foreach (Biome.Type type in Enum.GetValues(typeof(Biome.Type))) {
            if (type != Biome.Type.None) {
               biomeDropdown.options.Add(new Dropdown.OptionData { text = type.ToString() });
            }
         }
         biomeDropdown.value = 0;

         // Hook up events
         biomeDropdown.onValueChanged.AddListener((opt) => biomeDropdownChanged());
         duplicateButton.onClick.AddListener(duplicateGroup);
         publishButton.onClick.AddListener(publishLatestGroup);
         deleteButton.onClick.AddListener(deleteGroup);
      }

      public void set (KeyValuePair<Map, IEnumerable<Map>> group) {
         _map = group.Key;
         _children = new List<Map>();

         nameText.text = _map.name + " (group)";

         // Pick out a color for this group's markers
         _lastHue = (_lastHue + 0.15f) % 1f;
         Color markerColor = Color.HSVToRGB(_lastHue, 0.6f, 1f);

         arrowButton.GetComponentInChildren<Text>(true).color = markerColor;
         nameText.color = markerColor;

         MapListEntry pEntry = Instantiate(mapEntryPref, entryParent.transform);
         pEntry.set(_map, true);
         pEntry.setMarkerColor(markerColor);

         foreach (Map child in group.Value) {
            MapListEntry cEntry = Instantiate(mapEntryPref, entryParent.transform);
            cEntry.set(child, true);
            cEntry.setMarkerColor(markerColor);
            _children.Add(child);
         }
      }

      public void setExpanded (bool expanded) {
         entryParent.SetActive(expanded);
         arrowButton.GetComponentInChildren<Text>(true).text = expanded ? DOWN_TRIANGLE : LEFT_TRIANGLE;
      }

      public void toggleExpand () {
         MapListPanelState.toggleExpandedMapId(_map.id);
         setExpanded(MapListPanelState.self.expandedMapIds.Contains(_map.id));
      }

      public void biomeDropdownChanged () {
         // If the first title entry was selected do nothing
         if (biomeDropdown.value == 0) {
            return;
         }

         if (!MasterToolAccountManager.canAlterData()) {
            UI.messagePanel.displayUnauthorized("Your account type has no permissions to alter data");
            return;
         }

         if (MasterToolAccountManager.PERMISSION_LEVEL != PrivilegeType.Admin && _map.creatorID != MasterToolAccountManager.self.currentAccountID) {
            UI.messagePanel.displayUnauthorized("You are not the creator of this map");
            return;
         }

         // Get the biome
         Biome.Type biome = (Biome.Type) Enum.Parse(typeof(Biome.Type), biomeDropdown.options[biomeDropdown.value].text);

         UI.yesNoDialog.display(
            "Changing Biome of a Group",
            $"Are you sure you want to change the biome of all the maps in this group to { biome }?\nNew versions will be created for the change.\nCurrent changes will be lost.",
            () => changeBiomeConfirm(biome),
            null);
      }

      public void changeBiomeConfirm (Biome.Type biome) {
         // Get the target maps
         List<Map> targetMaps = _children.Append(_map).ToList();

         UI.loadingPanel.display($"Changing Biome of a Group:\nDownloading maps: 0/{ targetMaps.Count }");

         // Download the maps
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<MapVersion> targetVersions = new List<MapVersion>();

            foreach (Map map in targetMaps) {
               targetVersions.Add(DB_Main.getLatestMapVersionEditor(map, false));

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  UI.loadingPanel.display($"Changing Biome of a Group:\nDownloading maps: { targetVersions.Count }/{ targetMaps.Count }");
               });
            }

            // Process all downloaded versions
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               StartCoroutine(changeBiomeRoutine(biome, targetVersions));
            });
         });
      }

      public IEnumerator changeBiomeRoutine (Biome.Type biome, List<MapVersion> maps) {
         List<MapVersion> results = new List<MapVersion>();

         foreach (MapVersion map in maps) {
            UI.loadingPanel.display($"Changing Biome of a Group:\nProcessing: { results.Count }/{ maps.Count }");

            Overlord.instance.applyData(map);
            yield return new WaitForEndOfFrame();

            // Make drawboard change the biome
            Tools.changeBiome(biome);
            yield return new WaitForEndOfFrame();

            // Gather the changes
            MapVersion newVersion = new MapVersion {
               mapId = DrawBoard.loadedVersion.map.id,
               version = -1,
               createdAt = DateTime.UtcNow,
               updatedAt = DateTime.UtcNow,
               editorData = DrawBoard.instance.formSerializedData(),
               gameData = DrawBoard.instance.formExportData(),
               map = new Map { id = DrawBoard.loadedVersion.mapId, biome = Tools.biome },
               spawns = DrawBoard.instance.formSpawnList(DrawBoard.loadedVersion.mapId, DrawBoard.loadedVersion.version)
            };

            results.Add(newVersion);
         }

         // Upload changes to database
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            for (int i = 0; i < results.Count; i++) {
               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  UI.loadingPanel.display($"Changing Biome of a Group:\nUploading: { i }/{ results.Count }");
               });
               DB_Main.createNewMapVersion(results[i], biome, Overlord.authUserId, "Changed Biome to " + biome.ToString());
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               Overlord.loadAllRemoteData();
               UI.mapList.open();
            });
         });
      }

      public void deleteGroup () {
         if (!MasterToolAccountManager.canAlterData()) {
            UI.messagePanel.displayUnauthorized("Your account type has no permissions to alter data");
            return;
         }

         if (MasterToolAccountManager.PERMISSION_LEVEL != PrivilegeType.Admin && _map.creatorID != MasterToolAccountManager.self.currentAccountID) {
            UI.messagePanel.displayUnauthorized("You are not the creator of this map");
            return;
         }

         UI.yesNoDialog.display(
            "Deleting a Group",
            $"Are you sure you want to delete all maps in this group?",
            () => deleteGroupConfirm(),
            null);
      }

      public void deleteGroupConfirm () {
         UnityThreading.Task task = UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            string dbError = null;
            try {
               DB_Main.deleteMapGroup(_map.id);
            } catch (Exception ex) {
               dbError = ex.Message;
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               if (dbError != null) {
                  UI.messagePanel.displayError("Error deleting the map group:\n" + dbError.ToString());
               } else {
                  Overlord.loadAllRemoteData();
                  UI.mapList.open();
               }
            });

         });

         UI.loadingPanel.display("Deleting Map Group", task);
      }

      public void publishLatestGroup () {
         if (!MasterToolAccountManager.canAlterData()) {
            UI.messagePanel.displayUnauthorized("Your account type has no permissions to alter data");
            return;
         }

         if (MasterToolAccountManager.PERMISSION_LEVEL != PrivilegeType.Admin && _map.creatorID != MasterToolAccountManager.self.currentAccountID) {
            UI.messagePanel.displayUnauthorized("You are not the creator of this map");
            return;
         }

         UI.yesNoDialog.display(
            "Changing Biome of a Group",
            $"Are you sure you want to publish the latest version of each map in this group?",
            () => publishLatestGroupConfirm(),
            null);
      }

      public void publishLatestGroupConfirm () {
         UnityThreading.Task task = UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            string dbError = null;
            try {
               DB_Main.publishLatestVersionForAllGroup(_map.id);
            } catch (Exception ex) {
               dbError = ex.Message;
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               if (dbError != null) {
                  UI.messagePanel.displayError("Error Publishing the map group:\n" + dbError.ToString());
               } else {
                  Overlord.loadAllRemoteData();
                  UI.mapList.open();
               }
            });
         });

         UI.loadingPanel.display("Publishing Map Group", task);
      }

      public void duplicateGroup () {
         if (!MasterToolAccountManager.canAlterData()) {
            UI.messagePanel.displayUnauthorized("Your account type has no permissions to alter data");
            return;
         }

         if (MasterToolAccountManager.PERMISSION_LEVEL != PrivilegeType.Admin && _map.creatorID != MasterToolAccountManager.self.currentAccountID) {
            UI.messagePanel.displayUnauthorized("You are not the creator of this map");
            return;
         }

         UI.yesNoDialog.display(
            "Duplicating Map Group",
            "Are you sure you want to duplicate this map group?\nNew maps will be created for the entire group. New map names will end with a random integer.",
            () => duplicateGroupConfirm(),
            null);
      }

      public void duplicateGroupConfirm () {
         UnityThreading.Task task = UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            string dbError = null;
            try {
               DB_Main.duplicateMapGroup(_map.id, MasterToolAccountManager.self.currentAccountID);
            } catch (Exception ex) {
               dbError = ex.Message;
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               if (dbError != null) {
                  UI.messagePanel.displayError("Error duplicating the map group:\n" + dbError.ToString());
               } else {
                  Overlord.loadAllRemoteData();
                  UI.mapList.open();
               }
            });

         });

         UI.loadingPanel.display("Duplicating Map Group", task);
      }

      #region Private Variables

      // Last marker hue value that was used
      private static float _lastHue = 0;

      // The parent map that this group is targeting
      private Map _map;

      // Children maps of the main map
      private List<Map> _children;

      #endregion
   }
}
