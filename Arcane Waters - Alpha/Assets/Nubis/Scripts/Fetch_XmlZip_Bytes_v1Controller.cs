//#define NUBIS
#if NUBIS
using System;
using MySql.Data.MySqlClient;
using UnityEngine;
#endif

namespace NubisTranslator {
   public class Fetch_XmlZip_Bytes_v1Controller {
      public static string fetchZipRawData (int slot) {
         #if NUBIS
         UInt32 FileSize;
         byte[] rawData;

         try {
            using (MySqlConnection connection = DB_Main.getConnection()) {
               connection.Open();

               string query = "SELECT * FROM xml_status where id = " + slot;
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
         #endif
         return "";
      }
   }
} 