using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace IndiaTango.Models
{
	public static class Tools
	{
		public static string GenerateMD5HashFromFile(string filename)
		{
			FileStream file = new FileStream(filename, FileMode.Open);
			StringBuilder builder = new StringBuilder();
			MD5 hasher = new MD5CryptoServiceProvider();

		    byte[] data = new byte[file.Length];
		    file.Read(data, 0, data.Length);

		    byte[] buffer = hasher.ComputeHash(data);
			file.Close();

			foreach (Byte b in buffer)
				builder.Append(b.ToString("x2"));

			return builder.ToString();
		}
	}
}
