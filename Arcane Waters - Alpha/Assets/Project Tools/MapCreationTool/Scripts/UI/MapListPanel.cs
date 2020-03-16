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
      private Text loadingText = null;
      [SerializeField]
      private Text errorText = null;
      [SerializeField]
      private Toggle showMyMapsToggle = null;

      private List<MapListEntry> entries = new List<MapListEntry>();

      private bool showOnlyMyMaps = true;

      private void clearEverything () {
         foreach (MapListEntry entry in entries) {
            Destroy(entry.gameObject);
         }
         entries.Clear();

         loadingText.text = "";
         errorText.text = "";
      }

      public void open () {
         clearEverything();
         setLoadingText();
         show();
         loadList();

         showMyMapsToggle.SetIsOnWithoutNotify(showOnlyMyMaps);
         showMyMapsChanged();
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
                  foreach (Map map in maps) {
                     MapListEntry entry = Instantiate(entryPref, entryParent);
                     entry.set(map, () => UI.versionListPanel.open(map), () => deleteMap(map));
                     entry.gameObject.SetActive(
                        !showOnlyMyMaps ||
                        entry.target.creatorID == MasterToolAccountManager.self.currentAccountID);
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
         if (DrawBoard.loadedVersion != null && DrawBoard.loadedVersion.mapId == map.id) {
            DrawBoard.changeLoadedVersion(null);
         }
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            string error = null;
            try {
               DB_Main.deleteMap(map.id);
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

      public void showMyMapsChanged () {
         showOnlyMyMaps = showMyMapsToggle.isOn;
         foreach (MapListEntry entry in entries) {
            entry.gameObject.SetActive(
               !showOnlyMyMaps ||
               entry.target.creatorID == MasterToolAccountManager.self.currentAccountID);
         }
      }

      public void close () {
         hide();
      }

      private void setLoadingText () {
         loadingText.text = "Loading...";
      }
   }
}
