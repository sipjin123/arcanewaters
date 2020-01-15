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
      private Text dateText = null;
      [SerializeField]
      private Button deleteButton = null;

      public void set (MapDTO map, Action<string> onDelete, Action<string> openMap) {
         nameText.text = map.name;
         dateText.text = map.updatedAt.ToLocalTime().ToString();

         deleteButton.onClick.RemoveAllListeners();
         deleteButton.onClick.AddListener(() => onDelete(map.name));

         Button btn = GetComponent<Button>();
         btn.onClick.RemoveAllListeners();
         btn.onClick.AddListener(() => openMap(map.name));
      }

      public string getName () {
         return nameText.text;
      }
   }
}

