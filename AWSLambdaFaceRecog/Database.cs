using DotNetEnv;
using MySql.Data.MySqlClient;
using System.Diagnostics;

class Database
{
   
    public static DateTime GetLatestDateRaid(string id)
    {
        DateTime serverTime;
        try
        {
            var getConnection = (Debugger.IsAttached) ? DotNetEnv.Env.GetString("CONNECTION_STRING") : Environment.GetEnvironmentVariable("CONNECTION_STRING");

            using var conn = new MySqlConnection($"{getConnection}");
            conn.Open();
            MySqlCommand cmdGetLatest = new($"SELECT COALESCE(raid_dnt,NOW()) As result FROM time_logger_fr WHERE device_id = {id} ORDER BY raid_dnt DESC LIMIT 1;", conn)
            {
                CommandTimeout = 0
            };
            var sTimeNow = cmdGetLatest.ExecuteScalar();
            DateTime serverTimeConverted = DateTime.Parse(sTimeNow.ToString());
            serverTime = (sTimeNow == null) ? DateTime.Now : serverTimeConverted;
            return serverTime;

        }
        catch (Exception ex)
        {
            Console.WriteLine("GetLatestDateRaid: " + ex.ToString());
            return DateTime.Now;
        }

    }


    public static DateTime ServerNow()
    {
        DateTime TimeNow;
        try
        {
            var getConnection = (Debugger.IsAttached) ? DotNetEnv.Env.GetString("CONNECTION_STRING") : Environment.GetEnvironmentVariable("CONNECTION_STRING");

            using var conn = new MySqlConnection($"{getConnection}");
            conn.Open();
            MySqlCommand cmd_now = new("SELECT NOW();", conn)
            {
                CommandTimeout = 0
            };
            var sTimeNow = cmd_now.ExecuteScalar();
            DateTime timeConverted = DateTime.Parse(sTimeNow.ToString());
            TimeNow = (sTimeNow == null) ? DateTime.Now : timeConverted;

            return TimeNow;
        }
        catch (Exception ex)
        {
            Console.WriteLine("ServerNow: " + ex.ToString());

            return DateTime.Now;
        }

      
    }

    public static void TimeLoggerFR(string emp_id, string work_code, string time, string dateString, string device_id, string source = "6", int ns = 0)
    {
        try
        {
            var getConnection = (Debugger.IsAttached) ? DotNetEnv.Env.GetString("CONNECTION_STRING") : Environment.GetEnvironmentVariable("CONNECTION_STRING");

            using var conn = new MySqlConnection($"{getConnection}");
            conn.Open();
            string fSQL = @"
                INSERT INTO time_logger_fr(
                    date,
                    emp_id,
                    time,
                    work_code_id,
                    device_id,
                    source,
                    ns)
                VALUES(
                    @dateString,
                    @emp_id,
                    @time,
                    @work_code,
                    @device_id,
                    @source,
                    @ns);";
            var commandFR = new MySqlCommand(fSQL, conn);
            {
                var withBlock = commandFR;
                withBlock.Parameters.AddWithValue("@dateString", dateString);
                withBlock.Parameters.AddWithValue("@emp_id", emp_id);
                withBlock.Parameters.AddWithValue("@work_code", work_code);
                withBlock.Parameters.AddWithValue("@time", time);
                withBlock.Parameters.AddWithValue("@source", source);
                withBlock.Parameters.AddWithValue("@device_id", device_id);
                withBlock.Parameters.AddWithValue("@ns", ns);
                withBlock.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.ToString}");
        }
    }


}
