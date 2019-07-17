using System.Collections.Generic;
using UnityEngine;

public class NPCData : ScriptableObject
{
   #region Public Variables
   public List<ClickableText.Type> dialogueTypeList;

   public ClickableText.Type defaultDialogue;
   public List<NPCQuestData> npcQuestList;
   #endregion

   private void Awake () {
      dialogueTypeList = new List<ClickableText.Type>();
      dialogueTypeList.Add(defaultDialogue);
   }

   public void NoDialogues () {
      dialogueTypeList = new List<ClickableText.Type>();
      dialogueTypeList.Add(ClickableText.Type.None);
   }

   public void UnlockDialogue (NPCQuestData data, bool clearList) {
      if (clearList)
         dialogueTypeList.Clear();
      dialogueTypeList.Add(data.UnlockableDialogue);
   }
}