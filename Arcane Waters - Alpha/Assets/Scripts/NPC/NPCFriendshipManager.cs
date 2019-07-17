using UnityEngine;

public class NPCFriendshipManager : MonoBehaviour
{
   #region Public Variables
   public static NPCFriendshipManager self;
   #endregion
   private void Awake () {
      self = this;
   }

   public float LoadRelationship (string npcName) {
      return PlayerPrefs.GetFloat("NPC_NAME_" + npcName);
   }

   public float LoadRelationship (float npcID) {
      return PlayerPrefs.GetFloat("NPC_ID_" + npcID);
   }

   public void UpdateNPCRelationship (string npcID, float amount) {
      PlayerPrefs.SetFloat("NPC_NAME_" + npcID, amount);
   }

   public void UpdateNPCRelationship (float npcID, float amount) {
      PlayerPrefs.SetFloat("NPC_ID_" + npcID, amount);
   }
}