using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class MapListPanel : UIPanel
   {
      [SerializeField]
      private MapListEntry entryPref = null;
      [SerializeField]
      private Transform entryParent = null;
      [SerializeField]
      private Text secondTitleText = null;
      [SerializeField]
      private Text loadingText = null;
      [SerializeField]
      private Text errorText = null;

      private List<MapListEntry> entries = new List<MapListEntry>();

      private void clearEverything () {
         foreach (MapListEntry entry in entries) {
            Destroy(entry.gameObject);
         }
         entries.Clear();

         secondTitleText.text = "";
         loadingText.text = "";
         errorText.text = "";
      }

      public void open () {
         clearEverything();
         setLoadingText();
         show();
         loadList();
      }

      private void loadList () {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<Map> maps = null;
            string error = "";
            try {
               maps = DB_Main.getMaps();
            } catch (Exception ex) {
               error = ex.Message;
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               loadingText.text = "";
               if (maps == null) {
                  errorText.text = error;
               } else {
                  secondTitleText.text = $"Map count - {maps.Count}";
                  foreach (Map map in maps) {
                     MapListEntry entry = Instantiate(entryPref, entryParent);
                     entry.set(map, () => UI.versionListPanel.open(map), () => deleteMap(map));
                     entries.Add(entry);
                  }
               }
            });
         });
      }

      public void deleteMap (Map map) {
         if (!MasterToolAccountManager.canAlterData()) {
            UI.errorDialog.displayUnauthorized("Your account type has no permissions to alter data");
            return;
         }

         if (MasterToolAccountManager.PERMISSION_LEVEL != AdminManager.Type.Admin && map.creatorID != MasterToolAccountManager.self.currentAccountID) {
            UI.errorDialog.displayUnauthorized("You are not the creator of this map");
            return;
         }

         UI.yesNoDialog.display(
            "Deleting a map",
            $"Are you sure you want to delete map {map.name} <b>completely</b>? All versions will be <b>permanently</b> lost.",
            () => deleteMapConfirm(map), null);
      }

      public void deleteMapConfirm (Map map) {
         clearEverything();
         setLoadingText();
         if (DrawBoard.loadedVersion != null && DrawBoard.loadedVersion.mapName.CompareTo(map.name) == 0) {
            DrawBoard.changeLoadedVersion(null);
         }
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            string error = null;
            try {
               DB_Main.deleteMap(map.name);
            } catch (Exception ex) {
               error = ex.Message;
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               if (error != null) {
                  loadingText.text = "";
                  errorText.text = error;
               } else {
                  open();
               }
            });
         });
      }

      public void close () {
         hide();
      }

      private void setLoadingText () {
         loadingText.text = "Loading...";
      }
   }
}
