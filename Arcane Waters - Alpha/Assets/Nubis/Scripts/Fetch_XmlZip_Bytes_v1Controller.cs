//#define NUBIS
#if NUBIS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using UnityEngine;

namespace Nubis.Controllers
{
   public class Fetch_XmlZip_Bytes_v1Controller
   {
      public static string fetchZipRawData () {
         UInt32 FileSize;
         byte[] rawData;

         try {
            // Connect to the server.
            string connString = DB_Main.buildConnectionString(DB_Main.RemoteServer);

            if (String.IsNullOrEmpty(connString)) return string.Empty;

            using (MySqlConnection connection = new MySqlConnection(connString)) {

               connection.Open();

               string query = "SELECT * FROM xml_status where id = 1";

               using (MySqlCommand command = new MySqlCommand(query,connection)) {
                  using (MySqlDataReader dataReader = command.ExecuteReader()) {

                     while (dataReader.Read()) {
                        FileSize = dataReader.GetUInt32(dataReader.GetOrdinal("dataSize"));
                        rawData = new byte[FileSize];

                        dataReader.GetBytes(dataReader.GetOrdinal("xmlZipData"), 0, rawData, 0, (int) FileSize);

                        string textData = Convert.ToBase64String(rawData);
                        return textData;
                     }
                  }
               }
            }
         } catch (Exception ex) {
            Debug.LogError(ex.Message);
            return string.Empty;
         }

         return "";
      }
   }
} 
#endif