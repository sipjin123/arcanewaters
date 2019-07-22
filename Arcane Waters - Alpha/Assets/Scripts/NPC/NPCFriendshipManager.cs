using UnityEngine;

public class NPCFriendshipManager : MonoBehaviour
{
   #region Public Variables

   // Self reference
   public static NPCFriendshipManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   public int loadRelationship (int npcID) {
      return PlayerPrefs.GetInt("NPC_ID_" + npcID);
   }

   public void updateNPCRelationship (int npcID, int amount) {
      PlayerPrefs.SetFloat("NPC_ID_" + npcID, amount);
   }
}