//#define NUBIS
#if NUBIS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Nubis.Controllers
{
   public class Fetch_Xml_Version_v1Controller
   {

      public static string fetchXmlVersion () {

         try {
            // Connect to the server.
            string connString = DB_Main.buildConnectionString(DB_Main.RemoteServer);

            if (String.IsNullOrEmpty(connString)) return string.Empty;

            using (MySqlConnection connection = new MySqlConnection(connString)) {

               connection.Open();

               using (MySqlCommand command = new MySqlCommand(
                  "SELECT version FROM arcane.xml_status where id = 1",
                  connection)) {

                  using (MySqlDataReader reader = command.ExecuteReader()) {

                     while (reader.Read()) {
                        string version = reader.GetString("version");
                        return version;
                     }
                  }

               }
            }

         } catch {
            return string.Empty;
         }

         return string.Empty;
      }

   }
}
#endif