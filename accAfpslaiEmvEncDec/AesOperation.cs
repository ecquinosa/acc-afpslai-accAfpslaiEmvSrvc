using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;

namespace accAfpslaiEmvEncDec
{
	//public class AesOperation
	//{
	//    //ref: https://www.c-sharpcorner.com/article/encryption-and-decryption-using-a-symmetric-key-in-c-sharp/

	//    private static string key = "z20ca7811t4e2266bbxq2ea4343a1711";

	//    public static string EncryptString(string plainText)
	//    {
	//        byte[] iv = new byte[16];
	//        byte[] array;

	//        using (Aes aes = Aes.Create())
	//        {
	//            aes.Key = Encoding.UTF8.GetBytes(key);
	//            aes.IV = iv;

	//            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

	//            using (MemoryStream memoryStream = new MemoryStream())
	//            {
	//                using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
	//                {
	//                    using (StreamWriter streamWriter = new StreamWriter((Stream)cryptoStream))
	//                    {
	//                        streamWriter.Write(plainText);
	//                    }

	//                    array = memoryStream.ToArray();
	//                }
	//            }
	//        }

	//        return Convert.ToBase64String(array);
	//    }

	//    public static string DecryptString(string cipherText)
	//    {
	//        byte[] iv = new byte[16];
	//        byte[] buffer = Convert.FromBase64String(cipherText);

	//        using (Aes aes = Aes.Create())
	//        {
	//            aes.Key = Encoding.UTF8.GetBytes(key);
	//            aes.IV = iv;
	//            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

	//            using (MemoryStream memoryStream = new MemoryStream(buffer))
	//            {
	//                using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read))
	//                {
	//                    using (StreamReader streamReader = new StreamReader((Stream)cryptoStream))
	//                    {
	//                        return streamReader.ReadToEnd();
	//                    }
	//                }
	//            }
	//        }
	//    }
	//}

	public class Aes256CbcEncrypter
	{
		//ref: https://gist.github.com/doncadavona/fd493b6ced456371da8879c22bb1c263

		//     //implementation
		//     Console.WriteLine("Hello, world!");

		//         // The sample encryption key. Must be 32 characters.


		//     // The sample text to encrypt and decrypt.
		//     string Text = "Here is some text to encrypt!";

		//     // Encrypt and decrypt the sample text via the Aes256CbcEncrypter class.
		//     string Encrypted = Aes256CbcEncrypter.Encrypt(Text, Key);
		//     string Decrypted = Aes256CbcEncrypter.Decrypt(Encrypted, Key);

		//     // Show the encrypted and decrypted data and the key used.
		//     Console.WriteLine("Original: {0}", Text);
		//Console.WriteLine("Key: {0}", Key);
		//Console.WriteLine("Encrypted: {0}", Encrypted);
		//Console.WriteLine("Decrypted: {0}", Decrypted);

		private static readonly Encoding encoding = Encoding.UTF8;
		private static string key = "2SDIP0VECWw6rSolNZxmWQMe2UfzuWKM";

		public static string Encrypt(string plainText)
		{
			try
			{
				RijndaelManaged aes = new RijndaelManaged();
				aes.KeySize = 256;
				aes.BlockSize = 128;
				aes.Padding = PaddingMode.PKCS7;
				aes.Mode = CipherMode.CBC;

				aes.Key = encoding.GetBytes(key);
				aes.GenerateIV();

				ICryptoTransform AESEncrypt = aes.CreateEncryptor(aes.Key, aes.IV);
				byte[] buffer = encoding.GetBytes(plainText);

				string encryptedText = Convert.ToBase64String(AESEncrypt.TransformFinalBlock(buffer, 0, buffer.Length));

				String mac = "";

				mac = BitConverter.ToString(HmacSHA256(Convert.ToBase64String(aes.IV) + encryptedText, key)).Replace("-", "").ToLower();

				var keyValues = new Dictionary<string, object>
				{
					{ "iv", Convert.ToBase64String(aes.IV) },
					{ "value", encryptedText },
					{ "mac", mac },
				};

				//JavaScriptSerializer serializer = new JavaScriptSerializer();
				//var myContent = Newtonsoft.Json.JsonConvert.SerializeObject(request);
				//RefreshTokenResultJSON jsonResult = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonResponseText, typeof(RefreshTokenResultJSON));

				//return Convert.ToBase64String(encoding.GetBytes(serializer.Serialize(keyValues)));
				return Convert.ToBase64String(encoding.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(keyValues)));
			}
			catch (Exception e)
			{
				throw new Exception("Error encrypting: " + e.Message);
			}
		}

		public static string Decrypt(string plainText)
		{
			try
			{
				RijndaelManaged aes = new RijndaelManaged();
				aes.KeySize = 256;
				aes.BlockSize = 128;
				aes.Padding = PaddingMode.PKCS7;
				aes.Mode = CipherMode.CBC;
				aes.Key = encoding.GetBytes(key);

				// Base 64 decode
				byte[] base64Decoded = Convert.FromBase64String(plainText);
				string base64DecodedStr = encoding.GetString(base64Decoded);

				// JSON Decode base64Str
				//JavaScriptSerializer ser = new JavaScriptSerializer();				
				//var payload = ser.Deserialize<Dictionary<string, string>>(base64DecodedStr);
				var payload = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(base64DecodedStr);

				aes.IV = Convert.FromBase64String(payload["iv"]);

				ICryptoTransform AESDecrypt = aes.CreateDecryptor(aes.Key, aes.IV);
				byte[] buffer = Convert.FromBase64String(payload["value"]);

				return encoding.GetString(AESDecrypt.TransformFinalBlock(buffer, 0, buffer.Length));
			}
			catch (Exception e)
			{
				throw new Exception("Error decrypting: " + e.Message);
			}
		}

		static byte[] HmacSHA256(String data, String key)
		{
			using (HMACSHA256 hmac = new HMACSHA256(encoding.GetBytes(key)))
			{
				return hmac.ComputeHash(encoding.GetBytes(data));
			}
		}
	}
}
