using System;
using MapCreationTool.Serialization;
using UnityEngine;
using UnityEngine.UI;

namespace MapCreationTool
{
   public class VersionListEntry : MonoBehaviour
   {
      [SerializeField]
      private Text versionText = null;
      [SerializeField]
      private Text createdAtText = null;
      [SerializeField]
      private Text updatedAtText = null;
      [SerializeField]
      private Button publishButton = null;
      [SerializeField]
      private Button deleteButton = null;

      public MapVersion target { get; private set; }

      public void set (MapVersion mapVersion, Action onPublishClick, Action onDelete, Action onOpen) {
         target = mapVersion;

         versionText.text = mapVersion.version.ToString();
         createdAtText.text = mapVersion.createdAt.ToLocalTime().ToString();
         updatedAtText.text = mapVersion.updatedAt.ToLocalTime().ToString();

         bool live = mapVersion.map.publishedVersion != null && mapVersion.map.publishedVersion == mapVersion.version;

         publishButton.interactable = !live;
         publishButton.GetComponentInChildren<Text>().text = live ? "LIVE!" : "PUBLISH";

         publishButton.onClick.RemoveAllListeners();
         publishButton.onClick.AddListener(() => onPublishClick());

         deleteButton.interactable = onDelete != null;
         if (onDelete != null) {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(() => onDelete());
         }

         GetComponent<Button>().onClick.RemoveAllListeners();
         GetComponent<Button>().onClick.AddListener(() => onOpen());
      }


   }
}