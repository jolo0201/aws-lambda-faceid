using DotNetEnv;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class Database
{

    public static MySqlDataReader mExecuteReaderDbCon(string sql, string src = "mExecuteReaderDbCon")
    {

        try
        {
            using var connection = new MySqlConnection(Env.GetString("CONNECTION_STRING"));
            connection.Open();

            MySqlCommand command = new(sql, connection)
            {
                CommandTimeout = 0
            };
            return command.ExecuteReader();
        }
        catch (Exception)
        {
            MySqlDataReader? r = null;
            return r;
        }
    }

    public static string mExecuteReturnString(string script, string methodName = "")
    {
        string resultSet = "";

        try
        {
            if (script.Length != 0)
            {
                using var connection = new MySqlConnection(Env.GetString("CONNECTION_STRING"));
                connection.Open();

                MySqlCommand cmd = new(script, connection)
                {
                    CommandTimeout = 0
                };
                resultSet = (string)cmd.ExecuteScalar();

            }
        }
        catch (Exception ex)
        {
            resultSet = "";
            Console.WriteLine($"Error {methodName}: {script} {ex.Message}");
        }

        return resultSet;
    }

    public static DateTime GetLatestDateRaid(string id)
    {
        DateTime startDate = DateTime.Now;
        try
        {
            using var connection = new MySqlConnection(Env.GetString("CONNECTION_STRING"));
            connection.Open();

            MySqlCommand command = new($"SELECT COALESCE(raid_dnt,NOW()) As result FROM time_logger_fr WHERE device_id = {id} ORDER BY raid_dnt DESC LIMIT 1;", connection)
            {
                CommandTimeout = 0
            };

            MySqlDataReader readDate = command.ExecuteReader();

            if (readDate.HasRows == true)
            {
                while (readDate.Read())
                {
                    startDate = DateTime.Parse((string)readDate["result"]);
                }
            }
            else
            {
                startDate = DateTime.Parse("2021-01-01 00:00:00");
            }

        }
        catch (Exception)
        {
            return startDate;
        }

        return startDate;
    }


    public static DateTime ServerNow()
    {
        DateTime TimeNow;
        try
        {
            using var conn = new MySqlConnection(Env.GetString("CONNECTION_STRING"));
            conn.Open();
            MySqlCommand cmd_now = new("SELECT NOW();", conn)
            {
                CommandTimeout = 0
            };
            TimeNow = Convert.ToDateTime(cmd_now.ExecuteScalar());
            conn.Close();
        }
        catch (Exception)
        {
            return DateTime.Now;
        }

        return TimeNow;
    }

    public static void TimeLoggerFR(string emp_id, string work_code, string time, string dateString, string device_id, string source = "6", int ns = 0)
    {
        try
        {
            using var conn = new MySqlConnection(Env.GetString("CONNECTION_STRING"));
            conn.Open();
            var fSQL = @"
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

            Console.WriteLine("Successfully saved time log");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.ToString}");
        }
    }


}
