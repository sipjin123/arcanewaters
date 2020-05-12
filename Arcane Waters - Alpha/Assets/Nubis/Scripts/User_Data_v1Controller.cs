#if NUBIS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Nubis.Controllers
{
   public class User_Data_v1Controller
   {

      public static string userData (int usrId) {

         try {
            // Connect to the server.
            string connString = DB_Main.buildConnectionString(DB_Main.RemoteServer);

            if (String.IsNullOrEmpty(connString)) return string.Empty;

            using (MySqlConnection connection = new MySqlConnection(connString)) {

               connection.Open();

               using (MySqlCommand command = new MySqlCommand(
                  "SELECT usrGold, accGems, usrGender, usrName, bodyType, hairType, hairColor1, hairColor2, eyesType, eyesColor1, eyesColor2, wpnId, armId " +
                  "FROM users " +
                  "JOIN accounts " +
                  "USING (accId) " +
                  "WHERE usrId=@usrId",
                  connection)) {

                  command.Parameters.AddWithValue("@usrId", usrId);

                  using (MySqlDataReader reader = command.ExecuteReader()) {

                     while (reader.Read()) {
                        int usrGold = reader.GetInt32("usrGold");
                        int accGems = reader.GetInt32("accGems");
                        int usrGender = reader.GetInt32("usrGender");
                        string usrName = reader.GetString("usrName");
                        int bodyType = reader.GetInt32("bodyType");
                        int hairType = reader.GetInt32("hairType");
                        int hairColor1 = reader.GetInt32("hairColor1");
                        int hairColor2 = reader.GetInt32("hairColor2");
                        int eyesType = reader.GetInt32("eyesType");
                        int eyesColor1 = reader.GetInt32("eyesColor1");
                        int eyesColor2 = reader.GetInt32("eyesColor2");
                        int wpnId = reader.GetInt32("wpnId");
                        int armId = reader.GetInt32("armId");
                        string result = $"usrGold:{usrGold}[space]accGems:{accGems}[space]usrGender:{usrGender}[space]usrName:{usrName}[space]bodyType:{bodyType}[space]hairType:{hairType}[space]hairColor1:{hairColor1}[space]hairColor2:{hairColor2}[space]eyesType:{eyesType}[space]eyesColor1:{eyesColor1}[space]eyesColor2:{eyesColor2}[space]wpnId:{wpnId}[space]armId:{armId}\n";
                        return result;
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