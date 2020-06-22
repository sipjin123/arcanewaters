using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using System.Text;
using System;
using System.Security.Cryptography;

public class DBZipTester : MonoBehaviour {
   #region Public Variables

   public static string ServerDataText = "C:/TestText/ServerDataText.txt";
   public static string ClientDataText = "C:/TestText/ClientDataText.txt";
   public static string ClientDataZip = "C:/TestText/ClientDataZip.zip";
   public static string NewServerDataZip = "C:/TestText/ServerTestDataZip.zip";
   public static string ServerDataZip = "C:/XmlZipFile/ServerDataZip.zip";

   #endregion

   private void Update () {
      if (SystemInfo.deviceName == NubisDataFetchTest.DEVICE_NAME) {
         if (Input.GetKeyDown(KeyCode.Alpha5)) {
            byte[] rawdata = File.ReadAllBytes(ServerDataZip);
            string base64 = Convert.ToBase64String(rawdata);
            byte[] bytes = Convert.FromBase64String(base64);
            File.WriteAllBytes(NewServerDataZip, bytes);
         }
         if (Input.GetKeyDown(KeyCode.Alpha6)) {
            byte[] rawdata = File.ReadAllBytes(ServerDataZip);
            string base64 = Convert.ToBase64String(rawdata);
            byte[] bytes = Convert.FromBase64String(base64);
         }

         if (Input.GetKeyDown(KeyCode.Alpha1)) {
            D.editorLog("Writing using Server", Color.green);
            byte[] rawdata = File.ReadAllBytes(ServerDataZip);
            File.WriteAllBytes(ServerDataText, rawdata);

            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               DB_Main.writeZipData(rawdata, ((int)NubisRequestHandler.XmlSlotIndex.Default));

               UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
                  D.editorLog("DONE: " + rawdata.Length, Color.green);
               });
            });
         }

         if (Input.GetKeyDown(KeyCode.Alpha2)) {
            D.editorLog("Fetching using Client", Color.green);
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               /*
               string byteContent = Nubis.Controllers.Fetch_XmlZip_Bytes_v1Controller.fetchZipRawData();

               //string base64 = Convert.ToBase64String(rawdata);
               byte[] bytes = Convert.FromBase64String(byteContent);

               File.WriteAllBytes(ClientDataText, bytes);

               UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
                  D.editorLog("DONE: "+ byteContent.Length , Color.green);
               });*/
            });
         }

         //===============================================
         if (Input.GetKeyDown(KeyCode.Alpha3)) {
            D.editorLog("Zipping bytes using Client", Color.green);
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               byte[] newdata = File.ReadAllBytes(ClientDataText);
               File.WriteAllBytes(ClientDataZip, newdata);

               UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
                  D.editorLog("DONE: " + newdata.Length, Color.green);
               });
            });
         }
         /*
         if (Input.GetKeyDown(KeyCode.Alpha4)) {
            D.editorLog("Zipping bytes using Server", Color.green);
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               byte[] newdata = File.ReadAllBytes(ServerDataText);
               File.WriteAllBytes(NewServerDataZip, newdata);

               UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
                  D.editorLog("DONE: " + newdata.Length, Color.green);
               });
            });
         }*/
      }
   }

   #region Private Variables

   #endregion
}
