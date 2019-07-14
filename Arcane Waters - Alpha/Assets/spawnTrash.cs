using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class spawnTrash : MonoBehaviour {

    public GameObject spawnTrashObj;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            GameObject tempObj = GameObject.Instantiate(spawnTrashObj);
            NetworkServer.Spawn(tempObj);
        }
	
	}
}
