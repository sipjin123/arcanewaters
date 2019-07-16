using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class NPCFriendshipManager : MonoBehaviour
{


    public static NPCFriendshipManager self;
    private void Awake()
    {
        self = this;
    }

    public float LoadRelationship(string npcName)
    {
        return PlayerPrefs.GetFloat("NPC_NAME_" + npcName);
        return 100;
    }

    public float LoadRelationship(float npcID)
    {
        return PlayerPrefs.GetFloat("NPC_ID_" + npcID);
        return 100;
    }

    public void UpdateNPCRelationship(string npcID, float amount)
    {
        PlayerPrefs.SetFloat("NPC_NAME_" + npcID,amount);
    }
    public void UpdateNPCRelationship(float npcID, float amount)
    {
        PlayerPrefs.SetFloat("NPC_ID_" + npcID,amount);
    }



}
