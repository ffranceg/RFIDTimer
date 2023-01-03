using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace RFIDTimer
{
    public class SqliteDataAccess
    {
        public static List<EventModel> LoadPeople()
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                var output = cnn.Query<EventModel>("select * from Person", new DynamicParameters());
                return output.ToList();
            }
        }

        public static void SavePerson(EventModel events)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                cnn.Execute("insert into Person (FirstName, LastName) values (@FirstName, @LastName)", person);
            }
        }

        public interface IDbService
        {
            Task<T> GetAsync<T>(string command, object parms);
            Task<List<T>> GetAll<T>(string command, object parms);
            Task<int> EditData(string command, object parms);
        }












        private static string LoadConnectionString(string id = "Default")
        {
            //return ConfigurationManager.ConnectionStrings[id].ConnectionString;
        }
    }
}
