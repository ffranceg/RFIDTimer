using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using System.IO;

namespace RFIDTimer
{
    public static class Utilities
    {
        /// <summary>
        /// Convert ascii value to hex value
        /// </summary>
        /// <param name="hex"></param>
        /// <returns>Hex value</returns>
        public static string AsciiStringToHexString(string asciiString)
        {
            string hex = "";
            if (asciiString.Length > 0)
            {
                foreach (char c in asciiString)
                {
                    int tmp = c;
                    hex += String.Format("{0:x2}", (uint)System.Convert.ToUInt32(tmp.ToString()));
                }
                return hex.ToUpper();
            }
            else
            {
                return hex;
            }
        }

        /// <summary>
        /// Convert hex value to ascii value
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns>Ascii value</returns>
        public static string HexStringToAsciiString(string hexString)
        {
            string StrValue = "";
            if (hexString.Length > 0)
            {
                while (hexString.Length > 0)
                {
                    StrValue += System.Convert.ToChar(System.Convert.ToUInt32(hexString.Substring(0, 2), 16)).ToString();
                    hexString = hexString.Substring(2, hexString.Length - 2);
                }
                return StrValue;
            }
            else
            {
                return StrValue;
            }
        }

        /// <summary>
        /// Creates a byte array from the hexadecimal string. Each two characters are combined
        /// to create one byte. First two hexadecimal characters become first byte in returned array.
        /// Non-hexadecimal characters are ignored. 
        /// </summary>
        /// <param name="hexString">string to convert to byte array</param>
        /// <param name="discarded">number of characters in string ignored</param>
        /// <returns>byte array, in the same left-to-right order as the hexString</returns>
        public static byte[] GetBytes(string hexString, out int discarded)
        {
            discarded = 0;
            string newString = "";
            char c;
            // remove all none A-F, 0-9, characters
            for (int i = 0; i < hexString.Length; i++)
            {
                c = hexString[i];
                if (IsHexDigit(c))
                    newString += c;
                else
                    discarded++;
            }
            // if odd number of characters, discard last character
            if (newString.Length % 2 != 0)
            {
                discarded++;
                newString = newString.Substring(0, newString.Length - 1);
            }

            int byteLength = newString.Length / 2;
            byte[] bytes = new byte[byteLength];
            string hex;
            int j = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                hex = new String(new Char[] { newString[j], newString[j + 1] });
                bytes[i] = HexToByte(hex);
                j = j + 2;
            }
            return bytes;
        }

        /// <summary>
        /// Returns true is c is a hexadecimal digit (A-F, a-f, 0-9)
        /// </summary>
        /// <param name="c">Character to test</param>
        /// <returns>true if hex digit, false if not</returns>
        public static bool IsHexDigit(Char c)
        {
            int numChar;
            int numA = Convert.ToInt32('A');
            int num1 = Convert.ToInt32('0');
            c = Char.ToUpper(c);
            numChar = Convert.ToInt32(c);
            if (numChar >= numA && numChar < (numA + 6))
                return true;
            if (numChar >= num1 && numChar < (num1 + 10))
                return true;
            return false;
        }

        /// <summary>
        /// Converts 1 or 2 character string into equivalant byte value
        /// </summary>
        /// <param name="hex">1 or 2 character string</param>
        /// <returns>byte</returns>
        public static byte HexToByte(string hex)
        {
            if (hex.Length > 2 || hex.Length <= 0)
                throw new ArgumentException("hex must be 1 or 2 characters in length");
            byte newByte = byte.Parse(hex, System.Globalization.NumberStyles.HexNumber);
            return newByte;
        }

        /// <summary>
        /// Check whether the string is in hex or decimal
        /// ex: 0xFF or 12
        /// </summary>
        /// <param name="hex">string in hex or decimal</param>
        /// <returns>Returns the converted integer value if convertion is success else throws exception</returns>
        public static UInt32 CheckHexOrDecimal(string hex)
        {
            int prelen = 0;
            UInt32 decValue = 0;
            if (hex.EndsWith("0x") || hex.EndsWith("0X"))
            {
                throw new Exception("Not a valid hex number");
            }
            // If string is prefixed with 0x
            if (hex.StartsWith("0x") || hex.StartsWith("0X"))
            {
                prelen = 2;
                // Strikeout the 0x and extract the remaining string
                string hexstring = hex.Substring(prelen);
                // Check whether the string is valid hex number
                if (UInt32.TryParse(hexstring, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out decValue))
                {
                    decValue = UInt32.Parse(hexstring, System.Globalization.NumberStyles.HexNumber);
                    return decValue;
                }
                else
                {
                    throw new Exception("Not a valid hex number");
                }
            }
            else
            {
                // If the string is not prefixed with 0x
                // Check whether the string is valid decimal number
                if (AreAllValidNumericChars(hex))
                {
                    return Convert.ToUInt32(hex);
                }
                else
                {
                    throw new Exception("Not a valid number");
                }
            }
        }

        /// <summary>
        /// Check whether the string has valid numbers or no
        /// </summary>
        /// <param name="str">string</param>
        /// <returns>True: if string is valid number else False</returns>
        public static bool AreAllValidNumericChars(string str)
        {
            foreach (char c in str)
            {
                if (!Char.IsNumber(c)) return false;
            }
            return true;
        }

        /// <summary>
        /// Check whether the string has only double values or no
        /// </summary>
        /// <param name="str">string, accepts only - and . special characters</param>
        /// <returns>True: if string is double else False</returns>
        public static bool DoubleCharChecker(string str)
        {
            foreach (char c in str)
            {
                if (c.Equals('-'))
                    return true;

                else if (c.Equals('.'))
                    return true;

                else if (Char.IsNumber(c))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Check whether the hex string is prefix with 0x
        /// </summary>
        /// <param name="str">string, accepts only x </param>
        /// <returns>True: if string is double else False</returns>
        public static bool HexStringChecker(string str)
        {
            int hexNumber;
            foreach (char c in str)
            {
                if (c.Equals('x'))
                    return true;

                else if (int.TryParse(c.ToString(), NumberStyles.HexNumber, CultureInfo.CurrentCulture, out hexNumber))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Remove prefix 0x in a hexstring
        /// </summary>
        /// <param name="hexstring">hex string</param>
        /// <returns>hex string with 0x removed</returns>
        public static string RemoveHexstringPrefix(string hexstring)
        {
            int prelen = 0;
            if (hexstring.EndsWith("0x") || hexstring.EndsWith("0X"))
            {
                throw new Exception("Not a valid hex number");
            }
            if (hexstring.StartsWith("0x") || hexstring.StartsWith("0X"))
            {
                prelen = 2;
                hexstring = hexstring.Substring(prelen);
            }
            return hexstring;
        }

        /// <summary>
        /// Adds an ACL entry on the specified file for the specified account to 
        /// give every user of the local machine rights to modify configuration file
        /// </summary>
        /// <param name="fileName"></param>
        public static void SetEditablePermissionOnConfigFile(string fileName)
        {
            // Get a FileSecurity object that represents the 
            // current security settings.
            FileSecurity accessControl = File.GetAccessControl(fileName);

            // Give every user of the local machine rights to modify configuration file
            var userGroup = new NTAccount("BUILTIN\\Users");
            var userIdentityReference = userGroup.Translate(typeof(SecurityIdentifier));

            // Add the FileSystemAccessRule to the security settings.
            accessControl.SetAccessRule(
                new FileSystemAccessRule(userIdentityReference,
                                         FileSystemRights.FullControl,
                                         AccessControlType.Allow));
            // Set the new access settings.
            File.SetAccessControl(fileName, accessControl);
        }
    }

    /// <summary>
    /// Convertor to convert read power values between slider and textbox
    /// </summary>
    public class ReadPowerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                double dblValue = (double)value;
                return Math.Round(dblValue, 1);
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                try
                {
                    if (value.ToString() != "")
                    {
                        double ret = double.Parse(value.ToString());
                        return Math.Round(ret, 1);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            return 0;
        }
    }

}
