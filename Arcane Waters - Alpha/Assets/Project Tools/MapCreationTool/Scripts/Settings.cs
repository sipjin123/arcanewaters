using System;
using UnityEngine;

namespace MapCreationTool
{
   public class Settings
   {
      public static Keybindings keybindings = new Keybindings();

      public static void save () {
         PlayerPrefs.SetString("MapEditorSettings", serialize());
      }

      public static void load () {
         if (PlayerPrefs.HasKey("MapEditorSettings")) {
            load(PlayerPrefs.GetString("MapEditorSettings"));
         }
      }

      public static void load (string data) {
         SerializedSettings ss = JsonUtility.FromJson<SerializedSettings>(data);
         keybindings.applySerializedBindings(ss.bindings);
      }

      public static void setDefaults () {
         keybindings.setDefaults();
      }

      public static string serialize () {
         return JsonUtility.ToJson(new SerializedSettings {
            bindings = keybindings.serialize()
         });
      }

      [System.Serializable]
      private class SerializedSettings
      {
         public string bindings;
      }
   }
}