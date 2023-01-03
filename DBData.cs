using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Management.Instrumentation;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace RFIDTimer
{
    public class DBData
    {
        #region Database Alias
        // Events EV
        // Runners RU
        // EventRunner ER
        // Timings TG
        // NumRaceEpc NC
        // Rankings RK
        // RelayRace RR
        // Categories CT
        #endregion

        public const string CNS_SQLite = "Data Source=.\\Data\\RFIDTimer.db;Version=3;";
        public static DataTable ExecuteRead(string query, Dictionary<string, object> args)
        {
            if (string.IsNullOrEmpty(query.Trim()))
                return null;

            using (var con = new SQLiteConnection(CNS_SQLite))
            {
                con.Open();
                using (var cmd = new SQLiteCommand(query, con))
                {
                    foreach (KeyValuePair<string, object> entry in args)
                    {
                        cmd.Parameters.AddWithValue(entry.Key, entry.Value);
                    }
                    var da = new SQLiteDataAdapter(cmd);
                    var dt = new DataTable();
                    da.Fill(dt);
                    da.Dispose();
                    con.Close();
                    return dt;
                }
            }
        }
        public static DataTable ExecuteRead(string query)
        {
            if (string.IsNullOrEmpty(query.Trim()))
                return null;

            using (var con = new SQLiteConnection(CNS_SQLite))
            {
                con.Open();
                using (var cmd = new SQLiteCommand(query, con))
                {
                    var da = new SQLiteDataAdapter(cmd);
                    var dt = new DataTable();
                    da.Fill(dt);
                    da.Dispose();
                    con.Close();
                    return dt;
                }
            }
        }
        public static int ExecuteWrite(string query, Dictionary<string, object> args, string connectionString = CNS_SQLite)
        {
            int numberOfRowsAffected;

            //setup the connection to the database
            using (var con = new SQLiteConnection(connectionString))
            {
                con.Open();
                //open a new command
                using (var cmd = new SQLiteCommand(query, con))
                {
                    //set the arguments given in the query
                    foreach (var pair in args)
                    {
                        cmd.Parameters.AddWithValue(pair.Key, pair.Value);
                    }
                    //execute the query and get the number of row affected
                    numberOfRowsAffected = cmd.ExecuteNonQuery();
                }
                con.Close();
                return numberOfRowsAffected;
            }
        }

        #region SQLEvent
        //Getting All Event
        public static DataTable GetAllEvent()
        {
            var query = "SELECT * FROM Events EV";
            DataTable dtEvents = ExecuteRead(query);
            if (dtEvents == null || dtEvents.Rows.Count == 0)
            {
                return null;
            }
            return dtEvents;
        }

        //Getting Event by Date
        public static DataTable GetEventsByDate(DateTime startFrom)
        {
            var query = "SELECT * FROM Events EV where DateEvent >= @DateEvent";
            var args = new Dictionary<string, object>
            {
                {"@DateEvent", startFrom}
            };
            DataTable dtEvents = ExecuteRead(query);
            if (dtEvents == null || dtEvents.Rows.Count == 0)
            {
                return null;
            }
            return dtEvents;
        }

        //Getting Event by Id
        public static EventModel GetEventById(int idevent)
        {
            var query = "SELECT * FROM Events EV WHERE IDEvent = @IDEvent";
            var args = new Dictionary<string, object>
            {
                {"@IDEvent", idevent}
            };
            DataTable dtEvents = ExecuteRead(query, args);
            if (dtEvents == null || dtEvents.Rows.Count == 0)
            {
                return null;
            }
            var events = new EventModel
            {
                IDEvent = Convert.ToInt32(dtEvents.Rows[0]["IDEvent"]),
                DateEvent = (DateTime)dtEvents.Rows[0]["DateEvent"],
                DescEvent = Convert.ToString(dtEvents.Rows[0]["DescEvent"]),
                LenghtEv = Convert.ToInt32(dtEvents.Rows[0]["LenghtEv"]),
                TypeEv = Convert.ToString(dtEvents.Rows[0]["TypeEv"]),
                ShortCirc = (Boolean)dtEvents.Rows[0]["ShortCirc"]
            };
            return events;
        }
        //Add Event
        public static int AddEvent(EventModel events)
        {
            const string query = "INSERT INTO Events(DateEvent, DescEvent,LenghtEv,TypeEv,ShortCirc) VALUES(@DateEvent, @DescEvent,@LenghtEv,@TypeEv,@ShortCirc)";
            //here we are setting the parameter values that will be actually 
            //replaced in the query in Execute method
            var args = new Dictionary<string, object>
            {
                {"@DateEvent", events.DateEvent},
                {"@DescEvent", events.DescEvent},
                {"@LenghtEv", events.LenghtEv},
                {"@TypeEv", events.TypeEv},
                {"@ShortCirc", events.ShortCirc}
            };
            return ExecuteWrite(query, args, "");
        }

        //Editing Event
        public static int EditEvent(EventModel events)
        {
            const string query = "UPDATE Events SET DateEvent = @DateEvent, DescEvent = @DescEvent, LenghtEv=@LenghtEv, TypeEv=@TypeEv, ShortCirc=@ShortCirc WHERE IDEvent = @IDEvent";
            //here we are setting the parameter values that will be actually 
            //replaced in the query in Execute method
            var args = new Dictionary<string, object>
            {
                {"@IDEvent", events.IDEvent},
                {"@DateEvent", events.DateEvent},
                {"@DescEvent", events.DescEvent},
                {"@LenghtEv", events.LenghtEv},
                {"@TypeEv", events.TypeEv},
                {"@ShortCirc", events.ShortCirc}
            };
            return ExecuteWrite(query, args);
        }

        //Deleting Event
        public static int DeleteEvent(EventModel events)
        {
            const string query = "Delete from Events WHERE IDEvent = @IDEvent";
            //here we are setting the parameter values that will be actually 
            //replaced in the query in Execute method
            var args = new Dictionary<string, object>
                {
                    {"@IDEvent", events.IDEvent}
                };
            return ExecuteWrite(query, args, "");
        }
        #endregion

        #region SQLRunner
        // Getting Runners by Event_id and other runner for race registration
        public static DataTable GetAllRunnerByEventId(int idevent) 
        {
            var query = @"select RU.IDRun,RU.Name,RU.BirthYear,RU.Sex,RU.Email,EV.DescEvent,EV.TypeEv,ER.RaceNumber, coalesce(ER.Event_id,0) as SelEvent, ER.Category_id,CT.DesCat from Runners RU
                        left join EventRunner ER on RU.IDRun = ER.Runner_id and ER.Event_id=@IDEvent
                        left join Events EV on EV.IDEvent = ER.event_id 
                        left join Categories CT on CT.IDCat = ER.Category_id
                        order by RU.Name;";
            var args = new Dictionary<string, object>
            {
                {"@IDEvent", idevent}
            };
            DataTable dtRunners = ExecuteRead(query, args);
            return dtRunners;
        }
        #endregion
    }
    public class EventModel : ViewModelBase
    {
        public Int64 IDEvent { get; set; }
        public DateTime? DateEvent { get; set; }
        public String DescEvent { get; set; }
        public Int64? LenghtEv { get; set; }
        public String TypeEv { get; set; }
        public Boolean? ShortCirc { get; set; }
    }

    public class CategoryModel : ViewModelBase
    {
        public Int64 IDCat { get; set; }
        public String DesCat { get; set; }
        public String SexCat { get; set; }
        public Int64? YearFrom { get; set; }
        public Int64? YearTo { get; set; }
        public Boolean? ShortCircCat { get; set; }
    }

    public class EventRunnerModel : ViewModelBase
    {
        public Int64? Event_id { get; set; }
        public Int64? Runner_id { get; set; }
        public Int64? Category_id { get; set; }
        public Int64? RaceNumber { get; set; }
    }

    public class NumRaceEpcModel : ViewModelBase
    {
        public Int64? Number { get; set; }
        public String EPC { get; set; }
        public Boolean? InUse { get; set; }
    }

    public class RankingModel : ViewModelBase
    {
        public Int64 RankID { get; set; }
        public Int64? Event_id { get; set; }
        public Int64? RunID { get; set; }
        public DateTime? ElapsedTime { get; set; }
        public Int64? Category_id { get; set; }
        public Int64? Team { get; set; }
    }

    public class RelayRaceModel : ViewModelBase
    {
        public Int64 RRID { get; set; }
        public Int64? Event_id { get; set; }
        public Int64? RunID { get; set; }
        public Int64? Team { get; set; }
    }

    public class RunnerModel : ViewModelBase
    {
        public Int64 IDRun { get; set; }
        public String Name { get; set; }
        public Int64? BirthYear { get; set; }
        public String Sex { get; set; }
        public String Email { get; set; }

    }

    public partial class TimingModel : ViewModelBase
    {
        public Int64 IDTime { get; set; }
        public Int64? Event_id { get; set; }
        public Int64? Runnner_id { get; set; }
        public Int64? RaceNumber { get; set; }
        public String EPC { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime? ElapsedTime { get; set; }
        public Boolean? Modified { get; set; }

    }
    public class ViewModelBase : INotifyPropertyChanged
    {
        internal void OnPropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}







