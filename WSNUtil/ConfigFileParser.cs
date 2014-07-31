using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace WSNUtil
{
    public static partial class Variables
    {
        private static class ConfigFileParser
        {
            public static int RetrieveInt(string fieldName, string iniFile)
            {
                StreamReader Reader = new StreamReader(iniFile);
                int ToReturn = -1;
                bool Found = false;
                while (!Reader.EndOfStream)
                {
                    string CurrLine = Reader.ReadLine();
                    string[] EqualsSplit = CurrLine.Split(new char[] { '#', ';' })[0].Split('=');
                    if (EqualsSplit.Length != 2) // No equals sign or too many equals signs
                        continue;
                    if (EqualsSplit[0].Trim().ToUpper() == fieldName.ToUpper() && int.TryParse(EqualsSplit[1].Trim(), out ToReturn))
                    {
                        Found = true;
                        break;
                    }
                }
                Reader.Close();
                if (!Found)
                    throw new Exception(string.Format("Error reading config file, could not find a valid value for {0}", fieldName));
                return ToReturn;
            }

            public static float RetrieveFloat(string fieldName, string iniFile)
            {
                StreamReader Reader = new StreamReader(iniFile);
                float ToReturn = -1;
                bool Found = false;
                while (!Reader.EndOfStream)
                {
                    string CurrLine = Reader.ReadLine();
                    string[] EqualsSplit = CurrLine.Split(new char[] { '#', ';' })[0].Split('=');
                    if (EqualsSplit.Length != 2) // No equals sign or too many equals signs
                        continue;
                    if (EqualsSplit[0].Trim().ToUpper() == fieldName.ToUpper() && float.TryParse(EqualsSplit[1].Trim(), out ToReturn))
                    {
                        Found = true;
                        break;
                    }
                }
                Reader.Close();
                if (!Found)
                    throw new Exception(string.Format("Error reading config file, could not find a valid value for {0}", fieldName));
                return ToReturn;
            }

            public static IPAddress RetrieveIP(string fieldName, string iniFile)
            {
                StreamReader Reader = new StreamReader(iniFile);
                IPAddress ToReturn = null;
                bool Found = false;

                while (!Reader.EndOfStream)
                {
                    string CurrLine = Reader.ReadLine();
                    string[] EqualsSplit = CurrLine.Split(new char[] { '#', ';' })[0].Split('=');
                    if (EqualsSplit.Length != 2) // No equals sign or too many equals signs
                        continue;
                    if (EqualsSplit[0].Trim().ToUpper() == fieldName.ToUpper() && IPAddress.TryParse(EqualsSplit[1].Trim(), out ToReturn))
                    {
                        Found = true;
                        break;
                    }
                }
                Reader.Close();
                if (!Found)
                    throw new Exception(string.Format("Error reading config file, could not find a valid value for {0}", fieldName));
                return ToReturn;
            }

            public static string RetrieveString(string fieldName, string iniFile)
            {
                StreamReader Reader = new StreamReader(iniFile);
                string ToReturn = null;
                bool Found = false;

                while (!Reader.EndOfStream)
                {
                    string CurrLine = Reader.ReadLine();
                    string[] EqualsSplit = CurrLine.Split(new char[] { '#', ';' })[0].Split('=');
                    if (EqualsSplit.Length != 2) // No equals sign or too many equals signs
                        continue;
                    if (EqualsSplit[0].Trim().ToUpper() == fieldName.ToUpper())
                    {
                        Found = true;
                        ToReturn = EqualsSplit[1].Trim();
                        break;
                    }
                }
                Reader.Close();
                if (!Found)
                    throw new Exception(string.Format("Error reading config file, could not find a valid value for {0}", fieldName));
                return ToReturn;
            }
        } // End Sub Class
    } // End Class
} // End Namespace
