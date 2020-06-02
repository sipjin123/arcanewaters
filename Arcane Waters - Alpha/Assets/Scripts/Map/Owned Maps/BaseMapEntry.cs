using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BaseMapEntry : MonoBehaviour
{

   public void setData (string name, UnityAction onClick) {
      GetComponentInChildren<Text>().text = name;

      GetComponent<Button>().onClick.RemoveAllListeners();
      GetComponent<Button>().onClick.AddListener(onClick);
   }
}
