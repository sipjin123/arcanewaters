using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.UI;
using TMPro;

public class AsyncDBTest : MonoBehaviour
{
   private List<string> events = new List<string>();

   private void OnGUI () {
      // Print the current time and events
      GUI.TextField(new Rect(Vector2.zero, new Vector2(400, 600)), Time.time + Environment.NewLine + string.Join(Environment.NewLine, events));
   }

   private void Start () {
      testMethod();
   }

   private async void testMethod () {
      events.Add("Calling test database method: " + Time.time + "|" + Time.realtimeSinceStartup);
      string result = await Task.Run(() => {
         string r = null;
         for (int i = 0; i < 10; i++) {
            r = DB_Main.exec(DB_Main.getMaps, true).First().name;
         }
         return r;
      });
      events.Add("Database returned result - " + result + ":" + Time.time + "|" + Time.realtimeSinceStartup);
      FloatingCanvas.instantiateAt(CharacterScreen.self.transform.position + Vector3.up * 2f).GetComponentInChildren<TextMeshProUGUI>().text = result;
   }
}