using System;
using MapCreationTool.Serialization;
using UnityEngine;
using UnityEngine.UI;

namespace MapCreationTool
{
   public class MapListEntry : MonoBehaviour
   {
      [SerializeField]
      private Image icon = null;
      [SerializeField]
      private Text nameText = null;
      [SerializeField]
      private Text markerText = null;
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

      public Map target { get; private set; }

      public void set (Map map, bool isChild) {
         target = map;

         nameText.text = map.name;
         markerText.enabled = isChild;
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

         setIconFor(map);
      }

      public void setIconFor (Map map) {
         switch (map.editorType) {
            case EditorType.Area:
               icon.sprite = ImageManager.getSprite(@"Assets/Resources/Sprites/Icons/Perks/faction_naturalists.png");
               break;
            case EditorType.Interior:
               icon.sprite = ImageManager.getSprite(@"Assets/Resources/Sprites/Icons/Perks/specialty_merchant.png");
               break;
            case EditorType.Sea:
               icon.sprite = ImageManager.getSprite(@"Assets/Resources/Sprites/Icons/Perks/specialty_sailor.png");
               break;
         }
      }

      public void setMarkerColor (Color color) {
         markerText.color = color;
         latestVersionButton.image.color = color;
      }
   }
}

