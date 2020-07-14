using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class QuestDataToolScene : MonoBehaviour {
   #region Public Variables

   // Reference to the tool panel
   public QuestDataToolPanel questToolPanel;

   // Quest template prefab and parent
   public Transform questGroupHolder;
   public QuestDataGroupTemplate questGroupTemplate;

   // Creates a new template
   public Button createNewTemplate;

   #endregion

   private void Awake () {
      createNewTemplate.onClick.AddListener(() => {
         QuestDataGroupTemplate newQuestTemplate = Instantiate(questGroupTemplate.gameObject, questGroupHolder).GetComponent<QuestDataGroupTemplate>();
         newQuestTemplate.isActiveToggle.isOn = false;
         newQuestTemplate.editButton.onClick.AddListener(() => {
            questToolPanel.gameObject.SetActive(true);
            questToolPanel.loadPanel(new QuestData());
         });
      });
      questToolPanel.gameObject.SetActive(false);
   }

   public void loadData (List<QuestXMLContent> questXml) {
      questGroupHolder.gameObject.DestroyChildren();
      foreach (QuestXMLContent questGroup in questXml) {
         QuestDataGroupTemplate newQuestTemplate = Instantiate(questGroupTemplate.gameObject, questGroupHolder).GetComponent<QuestDataGroupTemplate>();
         newQuestTemplate.nameText.text = questGroup.xmlData.questGroupName;
         newQuestTemplate.xmlId = questGroup.xmlId;
         newQuestTemplate.indexText.text = questGroup.xmlId.ToString();
         newQuestTemplate.isActiveToggle.isOn = questGroup.isEnabled;

         Sprite sprite = ImageManager.getSprite(questGroup.xmlData.iconPath);
         if (sprite != null) {
            newQuestTemplate.itemIcon.sprite = sprite;
         } else {
            newQuestTemplate.itemIcon.sprite = ImageManager.self.blankSprite;
         }

         newQuestTemplate.editButton.onClick.AddListener(() => {
            questToolPanel.gameObject.SetActive(true);
            questToolPanel.loadPanel(questGroup.xmlData);
         });
      }
   }

   #region Private Variables

   #endregion
}
