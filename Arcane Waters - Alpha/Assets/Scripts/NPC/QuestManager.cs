using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
   #region Public Variables

   // Self Reference
   public static QuestManager self;

   public List<NPCData> npcDataList;

   #endregion

   private void Awake () {
      self = this;
   }
}