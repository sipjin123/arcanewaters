using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class DeliverQuest : ScriptableObject
{

    public List<DeliverData> deliveryList;
}
[Serializable]
public class DeliverData
{
    public Item itemToDeliver;
    public int quantity;
}