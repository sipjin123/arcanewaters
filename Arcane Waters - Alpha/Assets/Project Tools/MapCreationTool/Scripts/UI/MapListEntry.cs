using System;
using MapCreationTool.Serialization;
using UnityEngine;
using UnityEngine.UI;

namespace MapCreationTool
{
   public class MapListEntry : MonoBehaviour
   {
      private const string DOWN_TRIANGLE = "▼";
      private const string LEFT_TRIANGLE = "▶";
      private const string BULLET = "◆";

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
      private Button detailsButton = null;
      [SerializeField]
      private Button deleteButton = null;
      [SerializeField]
      private Button arrowButton = null;

      public Map target { get; private set; }

      public int? childOf { get; private set; }

      public void set (Map map, int? childOf) {
         target = map;
         this.childOf = childOf;

         nameText.text = map.name;
         createdAtText.text = map.createdAt.ToLocalTime().ToShortDateString();
         liveVersionText.text = map.publishedVersion != -1 ? map.publishedVersion.ToString() : "-";
         creatorText.text = map.creatorName;

         versionsButton.onClick.RemoveAllListeners();
         versionsButton.onClick.AddListener(() => UI.versionListPanel.open(map));

         deleteButton.onClick.RemoveAllListeners();
         deleteButton.onClick.AddListener(() => UI.mapList.deleteMap(map));

         detailsButton.onClick.RemoveAllListeners();
         detailsButton.onClick.AddListener(() => UI.mapDetailsPanel.open(map));

         latestVersionButton.onClick.RemoveAllListeners();
         latestVersionButton.onClick.AddListener(() => UI.mapList.openLatestVersion(map));

         arrowButton.onClick.RemoveAllListeners();
         arrowButton.onClick.AddListener(() => UI.mapList.toggleExpandMap(map));
      }

      public void setExpandable (bool expandable, bool child) {
         if (child) {
            arrowButton.interactable = false;
            arrowButton.GetComponentInChildren<Text>().text = "";
            nameText.text = nameText.text.Replace(BULLET + "   ", "");
            nameText.text = BULLET + "   " + nameText.text;
         } else {
            arrowButton.interactable = expandable;
            arrowButton.GetComponentInChildren<Text>().text = expandable ? LEFT_TRIANGLE : "";
         }
      }

      public void setExpanded (bool expanded) {
         arrowButton.GetComponentInChildren<Text>(true).text = expanded ? DOWN_TRIANGLE : LEFT_TRIANGLE;
      }
   }
}

