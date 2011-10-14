using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace IndiaTango.Models
{
	public static class Tools
	{
		public static string GenerateMD5HashFromFile(string filename)
		{
			var file = new FileStream(filename, FileMode.Open);
			var builder = new StringBuilder();
			MD5 hasher = new MD5CryptoServiceProvider();

		    var data = new byte[file.Length];
		    file.Read(data, 0, data.Length);

		    var buffer = hasher.ComputeHash(data);
			file.Close();

			foreach (Byte b in buffer)
				builder.Append(b.ToString("x2"));

			return builder.ToString();
		}
	}
}
