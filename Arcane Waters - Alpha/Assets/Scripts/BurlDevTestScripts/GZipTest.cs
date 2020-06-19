using UnityEngine;
using System.IO;
using System.Text;
using System;
using System.IO.Compression;

public class GZipTest : MonoBehaviour {
   #region Public Variables

   public static string SERVER_FILE_SOURCE = "C:/XmlTextFiles/";
   public static string SERVER_ZIP_DESTINATION = "C:/TestOutput/test.zip";

   public static string CLIENT_ZIP_SOURCE = Application.streamingAssetsPath + "/XmlZip/XmlContent2.zip";
   public static string CLIENT_FILE_DESTINATION = Application.streamingAssetsPath + "/XmlTexts2/";

   #endregion

   delegate void ProgressDelegate (string sMessage);

   private void OnGUI () {
      if (GUILayout.Button("Write Zip")) {
         D.editorLog("Write Zip", Color.magenta);

         commandZip();
      }

      if (GUILayout.Button("Upload Zip Actual")) {
         D.editorLog("Start zip upload");
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            byte[] zipData = File.ReadAllBytes(SERVER_ZIP_DESTINATION);
            DB_Main.writeZipData(zipData, 2);

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               D.editorLog("Done");
            });
         });
      }
      if (GUILayout.Button("Download Zip Database")) {
         D.editorLog("Start zip download");
         fetchSqlZipData();
      }
      if (GUILayout.Button("Extract new Zip from Database")) {
         D.editorLog("Start zip extract");
         DecompressToDirectory(CLIENT_ZIP_SOURCE, CLIENT_FILE_DESTINATION, (fileName) => { D.editorLog("Compressing {0}..." + fileName); }); 
      }
   }

   private void fetchSqlZipData () {
      D.debug("Fetching xml zip");
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

         string returnCode = NubisTranslator.Fetch_XmlZip_Bytes_v1Controller.fetchZipRawData(2);
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Debug.Log("Zip download Complete: " + returnCode);
            byte[] bytes = Convert.FromBase64String(returnCode);
            File.WriteAllBytes(CLIENT_ZIP_SOURCE, bytes);
         });
      });
   }

   private void commandZip () {
      CompressDirectory(SERVER_FILE_SOURCE, SERVER_ZIP_DESTINATION, (fileName) => { Console.WriteLine("Compressing {0}...", fileName); });
   }

   static void CompressFile (string sDir, string sRelativePath, GZipStream zipStream) {
      //Compress file name
      char[] chars = sRelativePath.ToCharArray();
      zipStream.Write(BitConverter.GetBytes(chars.Length), 0, sizeof(int));
      foreach (char c in chars)
         zipStream.Write(BitConverter.GetBytes(c), 0, sizeof(char));
      D.editorLog("Relative path was: " + sRelativePath);
      //Compress file content
      byte[] bytes = File.ReadAllBytes(Path.Combine(sDir, sRelativePath));
      zipStream.Write(BitConverter.GetBytes(bytes.Length), 0, sizeof(int));
      zipStream.Write(bytes, 0, bytes.Length);
   }

   static bool DecompressFile (string sDir, GZipStream zipStream, ProgressDelegate progress) {
      //Decompress file name
      byte[] bytes = new byte[sizeof(int)];
      int Readed = zipStream.Read(bytes, 0, sizeof(int));
      if (Readed < sizeof(int))
         return false;

      int iNameLen = BitConverter.ToInt32(bytes, 0);
      bytes = new byte[sizeof(char)];
      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < iNameLen; i++) {
         zipStream.Read(bytes, 0, sizeof(char));
         char c = BitConverter.ToChar(bytes, 0);
         sb.Append(c);
      }
      string sFileName = sb.ToString();
      if (progress != null)
         progress(sFileName);

      //Decompress file content
      bytes = new byte[sizeof(int)];
      zipStream.Read(bytes, 0, sizeof(int));
      int iFileLen = BitConverter.ToInt32(bytes, 0);

      bytes = new byte[iFileLen];
      zipStream.Read(bytes, 0, bytes.Length);

      string sFilePath = Path.Combine(sDir, sFileName);
      string sFinalDir = Path.GetDirectoryName(sFilePath);
      if (!Directory.Exists(sFinalDir))
         Directory.CreateDirectory(sFinalDir);

      using (FileStream outFile = new FileStream(sFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
         outFile.Write(bytes, 0, iFileLen);

      return true;
   }

   static void CompressDirectory (string sInDir, string sOutFile, ProgressDelegate progress) {
      string[] sFiles = Directory.GetFiles(sInDir, "*.*", SearchOption.AllDirectories);
      int iDirLen = sInDir[sInDir.Length - 1] == Path.DirectorySeparatorChar ? sInDir.Length : sInDir.Length + 0;
      D.editorLog(""+(sInDir[sInDir.Length - 1] == Path.DirectorySeparatorChar));
      using (FileStream outFile = new FileStream(sOutFile, FileMode.Create, FileAccess.Write, FileShare.None))
      using (GZipStream str = new GZipStream(outFile, CompressionMode.Compress))
         foreach (string sFilePath in sFiles) {
            D.editorLog("Checking file: " + sFilePath, Color.yellow);
            string sRelativePath = sFilePath.Substring(iDirLen);
            if (progress != null)
               progress(sRelativePath);
            CompressFile(sInDir, sRelativePath, str);
         }
   }

   static void DecompressToDirectory (string sCompressedFile, string sDir, ProgressDelegate progress) {
      using (FileStream inFile = new FileStream(sCompressedFile, FileMode.Open, FileAccess.Read, FileShare.None))
      using (GZipStream zipStream = new GZipStream(inFile, CompressionMode.Decompress, true))
         while (DecompressFile(sDir, zipStream, progress)) ;
   }

   #region Private Variables

   #endregion
}
