using Amazon.Lambda.Core;
using Com.FirstSolver.Splash;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualBasic;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Ocsp;
using System.Data;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text.RegularExpressions;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWSLambdaFaceRecog;

public class RaidFunction
{
    private const int DeviceCodePage = 65001;
    private const string connectionString = "server=localhost;port=3306;user=admin;password=demopass;database=chronos";

    private class DeviceSettings
    {
        public string? IP { get; set; }
        public string? Port { get; set; }
        public string? SecretKey { get; set; } 
        public string? id { get; set; }
    }
    /// <summary>
    /// A simple function that takes dsds string and does a ToUpper
    /// </summary>
    /// <param name="input"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public static string RaidFunctionHandler(ILambdaContext context)
    {

        try
        {
            //context.Logger.Log($"Lambda function has been invoked log");
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            MySqlCommand command = new($"SELECT * FROM config_devices WHERE d_status = 1;", connection)
            {
                CommandTimeout = 0
            };

            MySqlDataReader readDevice = command.ExecuteReader();
    
            if (readDevice.HasRows == true)
            {
                List<DeviceSettings> _clog = new();
                while (readDevice.Read()) 
                {
                    _clog.Add(new DeviceSettings()
                    {
                        IP = readDevice["ip_addr"].ToString(),
                        Port = readDevice["port"].ToString(),
                        SecretKey = readDevice["secret_key"].ToString(),
                        id = readDevice["id"].ToString()
                    }); // Add objects

      
                }
                readDevice.Close();

                foreach (DeviceSettings deviceSetting in _clog)
                {
                    string detectDevice = RunCommand("GetDeviceInfo()",
                                                    deviceSetting.IP,
                                                    deviceSetting.Port,
                                                    deviceSetting.SecretKey);

                    if (!detectDevice.Contains("success"))
                    {
                        context.Logger.Log("Cannot connect to device.");
                        return "Cannot connect to device.";
                    }
                    else 
                    {
                        var endDate = Strings.Format(DateTime.Now, "yyyy-MM-dd HH:mm:ss");
                        var startDate = Strings.Format(GetLatestDateRaid(deviceSetting.id), "yyyy-MM-dd HH:mm:ss");
                        string empId, fTime, fDate, fworkCode;

                        using FaceId Client = new(deviceSetting.IP, int.Parse(deviceSetting.Port));
                        Client.SecretKey = deviceSetting.SecretKey;
                        string commandString = "GetRecord(start_time=" + '"' + startDate + '"' + " end_time=" + '"' + endDate + '"' + ")";
                        FaceId_ErrorCode ErrorCode = Client.Execute(commandString,
                                                                    out string? AnswerString,
                                                                    DeviceCodePage);
                        if (ErrorCode == FaceId_ErrorCode.Success)
                        {
                            string Pattern = @"\b(time=.+" + System.Environment.NewLine + "(?:photo=\"[^\"]+\")*)";
                            MatchCollection matches = Regex.Matches(AnswerString, Pattern);

                            if (matches != null)
                            {
                                foreach (Match match in matches)
                                {
                                    var stringS = GetParameterValue(match.Groups[1].Value, "time", "id");
                                    string formattedDateString = stringS.Insert(10, " ");
                                    DateTime sampleDate = DateTime.Parse(formattedDateString);
                                    empId = GetParameterValue(match.Groups[1].Value, "id", "name");
                                    fTime = Strings.Format(sampleDate, "HH:mm:ss");
                                    fDate = Strings.Format(sampleDate, "yyyy-MM-dd");
                                    fworkCode = GetParameterValue(match.Groups[1].Value, "status", "authority");
                                    TimeLoggerFR(empId, fworkCode, fTime, fDate, deviceSetting.id);
                                }

                                Console.WriteLine(matches.Count);
                            }
                            return "Successfully raided device.";
                        }
                        else
                        {
                            Console.WriteLine("Error occurred.");
                            return "Error FaceId:" + ErrorCode.ToString();
                        }

                    }

                }

                return "Successfully raided device.";
            }
            else { 
                return "No Rows found.";
            }

      
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"There was an error encountered in this Lambda function.");
            return "Error 404" + ex.ToString();
        }
    }


    private static string RunCommand(string command, string deviceIP, string devicePort, string secretKey)
    {
        string result = "";
        try
        {
            using FaceId Client = new(deviceIP, int.Parse(devicePort));
            Client.SecretKey = secretKey;
            FaceId_ErrorCode ErrorCode = Client.Execute(command, out string answer, DeviceCodePage);

            if (ErrorCode == FaceId_ErrorCode.Success)
                result = answer;
            else
            {
                result = $"Error: Cannot connect to device.";

            }
        }
        catch (SocketException ex)
        {
            result = "Error SocketException:" + ex.ToString();
            return result;
        }
        catch (Exception ey)
        {
            result = "Error Exception:" + ey.ToString();
            return result;
        }

        return result;
    }


    private static MySqlDataReader mExecuteReaderDbCon(string sql, string src = "mExecuteReaderDbCon")
    {
      
        try
        {
            using var connection = new MySqlConnection(connectionString);
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

    private static string mExecuteReturnString(string script, string methodName = "")
    {
        string resultSet = "";

        try
        {
            if (script.Length != 0)
            {
                using var connection = new MySqlConnection(connectionString);
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

    private static DateTime GetLatestDateRaid(string id)
    {
        DateTime startDate = DateTime.Now;
        try
        {
            using var connection = new MySqlConnection(connectionString);
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

    private static string GetParameterValue(string str, string startIdentifier, string endIdentifier)
    {
        str = str.Replace("Return(", "");
        str = str.Replace("\"", "'");
        str = str.Remove(str.Length - 1);
        string myStr = str;

        StringReader S = new(myStr); string Result = "";
        try
        {
            while ((S.Peek() != -1))
            {
                string Line = S.ReadLine();
                {
                    var withBlock = Line.ToLower();
                    string sSource = Line.Replace(" ", "");
                    string sDelimStart = startIdentifier + "='";
                    string sDelimEnd = "'" + endIdentifier;


                    int nIndexStart = sSource.IndexOf(sDelimStart);
                    int nIndexEnd = sSource.IndexOf(sDelimEnd);

                    string res = Strings.Mid(sSource, nIndexStart + sDelimStart.Length + 1, nIndexEnd - nIndexStart - sDelimStart.Length); // Crop the text between
                    Result = res;
                }
            }
        }
        catch (Exception)
        {
        }

        S.Close();
        S.Dispose();
        S = null;

        return Result;
    }

    private static string InsertSpace(string str, int index)
    {
        string result;
        int i = index;

        while (i < str.Length)
        {
            str = str.Insert(i, "");
            i += 10;
        }

        result = str;
        return result;
    }

    private static void TimeLoggerFR(string emp_id, string work_code, string time, string dateString, string device_id, string source = "6", int ns = 0)
    {
        try
        {
            using var conn = new MySqlConnection(connectionString);
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


    private void RaidRecord(List<DeviceSettings> device, ILambdaContext context)
    {
       
    }


}
