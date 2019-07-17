using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
   #region Public Variables
   public static QuestManager self;
   public GameObject questTemplate;
   public Transform questParent;
   public List<NPCQuestData> currentQuestList = new List<NPCQuestData>();
   public List<GameObject> questTemplateList = new List<GameObject>();
   #endregion

   private void Awake () {
      self = this;
   }

   public void RegisterQuest (NPCQuestData quest) {
      GameObject temp = Instantiate(questTemplate, questParent);
      temp.GetComponent<QuestTemplate>().questData = quest;

      temp.SetActive(true);
      currentQuestList.Add(quest);
      questTemplateList.Add(temp);
   }

   public void ClearQuest (NPCQuestData data) {
      var gameObj = questTemplateList.Find(_ => _.GetComponent<QuestTemplate>().questData == data);
      questTemplateList.Remove(gameObj);
      Destroy(gameObj);

      currentQuestList.Remove(data);
   }
}