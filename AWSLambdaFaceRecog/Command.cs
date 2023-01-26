using Com.FirstSolver.Splash;
using Microsoft.VisualBasic;
using System.Net.Sockets;


class Command
{
    public const int DeviceCodePage = 65001;
    public static string RunCommand(string command, string deviceIP, string devicePort, string secretKey)
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



    public static string GetParameterValue(string str, string startIdentifier, string endIdentifier)
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
        return Result;
    }

}

