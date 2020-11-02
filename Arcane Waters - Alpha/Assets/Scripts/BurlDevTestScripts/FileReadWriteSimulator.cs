using UnityEngine;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;

public class FileReadWriteSimulator : MonoBehaviour {
   #region Public Variables

   // Determines if writing and has begun
   public bool isWriter;
   public bool hasBegun;

   // Debug
   string debugger = "";

   // Address
   public string path = "";
   public string directoryName = "/TestData/";
   public string fileName = "ReaderTest.txt";

   // Timers
   public float writeTimer;
   public float newTimer = 0;
   public float overAllTimer = 0;

   // Read stats
   public float readCount = 0;
   public float cantReadCount = 0;
   public float readPercent = 0;

   // Write Stats
   public float writeCount = 0;
   public float cantWriteCount = 0;
   public float writePercent = 0;
   
   #endregion

   private void Begin () {
      writeTimer = 0;
      path = Application.persistentDataPath;
      hasBegun = true;
      InvokeRepeating("processPerFrame", 0, .5f);
   }

   private void processPerFrame () {
      if (hasBegun) {
         if (isWriter) {
            writeTimer += Time.deltaTime;
            try {
               File.WriteAllText(path + directoryName + fileName, writeTimer.ToString("f2"));
               D.debug("Writing");
               writeCount++;
            } catch {
               cantWriteCount++;
               D.debug("Cant write");
            }
            writePercent = (cantWriteCount / writeCount) * 100;
         } else {
            readStuff();
         }
      }
   }

   private void readStuff () {
      try {
         FileStream fs = new FileStream(path + "/" + directoryName + fileName,
                                          FileMode.OpenOrCreate,
                                          FileAccess.ReadWrite,
                                          FileShare.None);
         StreamReader sr = new StreamReader(fs, Encoding.UTF8);
         string content = sr.ReadToEnd();
         fs.Close();
         debugger = content;
         D.debug("Reading");
         readCount++;
      } catch {
         cantReadCount++;
         D.debug("Cant Read");
      }

      readPercent = (cantReadCount / readCount) * 100;
   }

   private void read2 () {
      try {
         BinaryFormatter bf = new BinaryFormatter();
         FileStream file = File.Open(path + "/" + directoryName + fileName, FileMode.Open);
         object content = bf.Deserialize(file);
         debugger = content.ToString();
         file.Close();
      } catch {
         D.debug("Cant Read");
      }
   }

   private void OnGUI () {
      if (GUILayout.Button("BeginWrite")) {
         isWriter = true;
         Begin();
      }
      if (GUILayout.Button("BeginRead")) {
         isWriter = false;
         Begin();
      }
      if (GUILayout.Button("Clear Timers")) {
         readCount = 0;
         cantReadCount = 0;

         writeCount = 0;
         cantWriteCount = 0;
      }

      GUILayout.Space(10);
      GUILayout.Box("Time: " + overAllTimer.ToString("f2"));
      
      if (!isWriter) {
         GUILayout.Box("ReadData: " + debugger);

         GUILayout.Space(10);
         GUILayout.Box("Read Loss %: " + readPercent.ToString("f2"));
         GUILayout.Box("ReadCount: " + readCount, GUILayout.Width(200));
         GUILayout.Box("CantReadCount: " + cantReadCount);
      } else {
         GUILayout.Box("WriteData: " + writeTimer);

         GUILayout.Space(10);
         GUILayout.Box("Write Loss %: " + writePercent.ToString("f2"));
         GUILayout.Box("WriteCount: " + writeCount, GUILayout.Width(200));
         GUILayout.Box("CantWriteCount: " + cantWriteCount);
      }

      /*
      if (GUILayout.Button("Save Pref: " + newTimer)) {
         PlayerPrefs.SetFloat("TEST_PREF", newTimer);
      }*/
      //GUILayout.Box("Fetched Pref :: " + PlayerPrefs.GetFloat("TEST_PREF", 0));
   }

   #region Private Variables

   #endregion
}
