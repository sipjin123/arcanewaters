using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
   #region Public Variables

   // Self Reference
   public static QuestManager self;

   // UI Template to instantiate
   public GameObject questTemplate;

   // Quest List Holder
   public Transform questParent;

   // List of current active quests
   public List<NPCQuestData> currentQuestList = new List<NPCQuestData>();

   // List of current quest templates instantiated
   public List<GameObject> questTemplateList = new List<GameObject>();

   #endregion

   private void Awake () {
      self = this;
   }

   public void registerQuest (NPCQuestData quest) {
      GameObject questUITemplate = Instantiate(questTemplate, questParent);
      questUITemplate.GetComponent<QuestTemplate>().questData = quest;

      questUITemplate.SetActive(true);
      currentQuestList.Add(quest);
      questTemplateList.Add(questUITemplate);
   }

   public void clearQuest (NPCQuestData data) {
      GameObject questUITemplate = questTemplateList.Find(_ => _.GetComponent<QuestTemplate>().questData == data);
      questTemplateList.Remove(questUITemplate);
      Destroy(questUITemplate);

      currentQuestList.Remove(data);
   }
}