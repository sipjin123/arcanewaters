using System;
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
      private Text updatedAtText = null;
      [SerializeField]
      private Text versionText = null;
      [SerializeField]
      private Text liveVersionText = null;
      [SerializeField]
      private Text creatorText = null;
      [SerializeField]
      private Button button = null;
      [SerializeField]
      private Button deleteButton = null;

      public void set (MapDTO map, Action<string> onButtonClick, Action onDelete, Action openMap) {
         nameText.text = map.name;
         createdAtText.text = map.createdAt.ToLocalTime().ToString();
         updatedAtText.text = map.updatedAt.ToLocalTime().ToString();
         versionText.text = map.version.ToString();
         liveVersionText.text = map.liveVersion?.ToString() ?? "-";
         creatorText.text = map.creatorName + '\n' + map.creatorID;

         button.onClick.RemoveAllListeners();
         button.onClick.AddListener(() => onButtonClick(map.name));

         deleteButton.onClick.RemoveAllListeners();
         deleteButton.onClick.AddListener(() => onDelete());

         Button btn = GetComponent<Button>();
         btn.onClick.RemoveAllListeners();
         btn.onClick.AddListener(() => openMap());
      }

      public void setAsLiveMap () {
         button.interactable = false;

         Text text = button.GetComponentInChildren<Text>();
         text.text = "LIVE!";
         text.fontStyle = FontStyle.BoldAndItalic;
      }

      public string getName () {
         return nameText.text;
      }
   }
}

