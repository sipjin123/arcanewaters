using System;
using MapCreationTool.Serialization;
using UnityEngine;
using UnityEngine.UI;

namespace MapCreationTool
{
   public class MapListEntry : MonoBehaviour
   {
      [SerializeField]
      private Text nameText = null;
      [SerializeField]
      private Text createdAtText = null;
      [SerializeField]
      private Text liveVersionText = null;
      [SerializeField]
      private Text creatorText = null;
      [SerializeField]
      private Button latestVersionButton = null;
      [SerializeField]
      private Button versionsButton = null;
      [SerializeField]
      private Button renameButton = null;
      [SerializeField]
      private Button deleteButton = null;

      public Map target { get; private set; }
      public void set (Map map, Action onLatestVersion, Action onVersionListClick, Action onRename, Action onDelete) {
         target = map;

         nameText.text = map.name;
         createdAtText.text = map.createdAt.ToLocalTime().ToShortDateString();
         liveVersionText.text = map.publishedVersion?.ToString() ?? "-";
         creatorText.text = map.creatorName;

         versionsButton.onClick.RemoveAllListeners();
         versionsButton.onClick.AddListener(() => onVersionListClick());

         deleteButton.onClick.RemoveAllListeners();
         deleteButton.onClick.AddListener(() => onDelete());

         renameButton.onClick.RemoveAllListeners();
         renameButton.onClick.AddListener(() => onRename());

         latestVersionButton.onClick.RemoveAllListeners();
         latestVersionButton.onClick.AddListener(() => onLatestVersion());
      }
   }
}

