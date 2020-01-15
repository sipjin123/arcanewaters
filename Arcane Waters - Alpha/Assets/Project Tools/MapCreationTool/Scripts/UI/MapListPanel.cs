using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MapCreationTool
{
   public class MapListPanel : UIPanel
   {
      [SerializeField]
      private MapListEntry entryPref = null;
      [SerializeField]
      private Transform entryParent = null;
      [SerializeField]
      private Text countText = null;
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

         countText.text = "";
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
            List<MapDTO> maps = null;
            string error = "";
            try {
               maps = DB_Main.getMapDatas(false, false);
            } catch (Exception ex) {
               error = ex.Message;
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               loadingText.text = "";
               if (maps == null) {
                  errorText.text = error;
               } else {
                  foreach (MapDTO map in maps) {
                     MapListEntry entry = Instantiate(entryPref, entryParent);
                     entry.set(map, delete, openMap);
                     entries.Add(entry);
                  }
               }
            });
         });
      }

      public void openMap (string name) {
         UI.yesNoDialog.display(
            "Opening a map",
            $"Are you sure you want to open map {name}?\nAll unsaved progress will be permanently lost.",
             () => openMapConfirm(name),
             null);
      }

      public void openMapConfirm (string name) {
         clearEverything();
         setLoadingText();
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            MapDTO map = null;
            string dbError = null;
            try {
               map = DB_Main.getMapData(name, true, false);
            } catch (Exception ex) {
               dbError = ex.Message;
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               if (dbError != null) {
                  loadingText.text = "";
                  errorText.text = dbError;
               } else if (map == null) {
                  loadingText.text = "";
                  errorText.text = $"Could not find map {name} in the database";
               } else {
                  Overlord.instance.applyFileData(map.editorData, name);
                  hide();
               }
            });
         });
      }

      public void delete (string name) {
         UI.yesNoDialog.display(
           "Deleting a map",
           $"Are you sure you want to <b>permanently</b> delete map {name}?",
            () => deleteConfirm(name),
            null);
      }

      public void deleteConfirm (string name) {
         setLoadingText();
         MapListEntry entry = entries.FirstOrDefault(e => e.getName().CompareTo(name) == 0);
         if (entry != null) {
            Destroy(entry.gameObject);
            entries.Remove(entry);
         }

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            string dbError = null;
            try {
               DB_Main.deleteMapData(name);
            } catch (Exception ex) {
               dbError = ex.Message;
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               loadingText.text = "";
               if (dbError != null) {
                  errorText.text = dbError;
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
