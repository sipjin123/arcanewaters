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
         newQuestTemplate.editButton.onClick.AddListener(() => {
            questToolPanel.gameObject.SetActive(true);
            questToolPanel.loadPanel(new QuestData());
         });
      });
      questToolPanel.gameObject.SetActive(false);
   }

   public void loadData (List<QuestXMLContent> questXml) {
      foreach (QuestXMLContent questGroup in questXml) {
         QuestDataGroupTemplate newQuestTemplate = Instantiate(questGroupTemplate.gameObject, questGroupHolder).GetComponent<QuestDataGroupTemplate>();
         newQuestTemplate.editButton.onClick.AddListener(() => {
            questToolPanel.gameObject.SetActive(true);
            questToolPanel.loadPanel(questGroup.xmlData);
         });
      }
   }

   #region Private Variables

   #endregion
}
