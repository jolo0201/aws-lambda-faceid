using Amazon.Lambda.Core;
using Com.FirstSolver.Splash;
using DotNetEnv;
using Microsoft.VisualBasic;
using MySql.Data.MySqlClient;
using System.Net.Sockets;
using System.Text.RegularExpressions;
// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWSLambdaFaceRecog;

public class RaidFunction
{
    /// <summary>
    /// a lambda function to raid all face recognition devices
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static string RaidFunctionHandler(ILambdaContext context)
    {
        //Load ENV file configuration
        DotNetEnv.Env.Load();

        try
        {
            //Read active devices
            MySqlDataReader readDevice = Database.mExecuteReaderDbCon("SELECT * FROM config_devices WHERE d_status = 1;");
            if (readDevice.HasRows == true)
            {
                List<DeviceSettings> _devicesData = new();
                while (readDevice.Read()) 
                {
                    _devicesData.Add(new DeviceSettings()
                    {
                        IP = readDevice["ip_addr"].ToString(),
                        Port = readDevice["port"].ToString(),
                        SecretKey = readDevice["secret_key"].ToString(),
                        DeviceId = readDevice["id"].ToString(),
                        DeviceName = readDevice["device_name"].ToString()
                    }); // Add devices as objects

      
                }//while

                readDevice.Close(); //close reader

                foreach (DeviceSettings deviceSetting in _devicesData) //Loop devices
                {
                    //Check devices if active and connected
                    string detectDevice = Command.RunCommand("GetDeviceInfo()",
                                                    deviceSetting.IP,
                                                    deviceSetting.Port,
                                                    deviceSetting.SecretKey);

                    if (!detectDevice.Contains("success"))
                    {
                        context.Logger.Log($"Cannot connect to device: {deviceSetting.DeviceName}.");
                    }
                    else 
                    {
                        var endDate = Strings.Format(Database.ServerNow, "yyyy-MM-dd HH:mm:ss"); //Get Database Server time
                        var startDate = Strings.Format(Database.GetLatestDateRaid(deviceSetting.DeviceId), "yyyy-MM-dd HH:mm:ss"); //Get Latest Raided Date
                        string empId, fTime, fDate, fworkCode;
                      
                        using FaceId Client = new(deviceSetting.IP, int.Parse(deviceSetting.Port));
                        Client.SecretKey = deviceSetting.SecretKey;
                        string commandString = "GetRecord(start_time=" + '"' + startDate + '"' + " end_time=" + '"' + endDate + '"' + ")";
                        FaceId_ErrorCode ErrorCode = Client.Execute(commandString,
                                                                    out string? AnswerString,
                                                                    Command.DeviceCodePage);
                        if (ErrorCode == FaceId_ErrorCode.Success)
                        {
                            string Pattern = @"\b(time=.+" + Environment.NewLine + "(?:photo=\"[^\"]+\")*)";
                            MatchCollection matches = Regex.Matches(AnswerString, Pattern);
                            int dataCount;
                            if (matches != null)
                            {
                                dataCount = matches.Count;
                                foreach (Match match in matches.Cast<Match>())
                                {
                                    var stringS = Command.GetParameterValue(match.Groups[1].Value, "time", "id");
                                    string formattedDateString = stringS.Insert(10, " ");
                                    DateTime sampleDate = DateTime.Parse(formattedDateString);
                                    empId = Command.GetParameterValue(match.Groups[1].Value, "id", "name");
                                    fTime = Strings.Format(sampleDate, "HH:mm:ss");
                                    fDate = Strings.Format(sampleDate, "yyyy-MM-dd");
                                    fworkCode = Command.GetParameterValue(match.Groups[1].Value, "status", "authority");
                                    Database.TimeLoggerFR(empId, fworkCode, fTime, fDate, deviceSetting.DeviceId);
                                }
                                return $"Successfully raided ({dataCount}) device data.";
                            }
                            else
                            {
                                return $"Success but no matches.";
                            }//matches
                        }
                        else
                        {
                            Console.WriteLine("Error occurred.");
                            return "Error FaceId:" + ErrorCode.ToString();
                        }//Errorcode
                    }//detect device
                }//foreach
                return "Successfully raided device.";
            }
            else { 
                return "No device list found.";
            }//read device
        }//try
        catch (Exception ex)
        {
            context.Logger.LogError($"Error in Lambda function:{ex}");
            return "Error 404" + ex.ToString();
        }
    }
}
