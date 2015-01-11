using Common;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer
{
	sealed class SqlDatabase : IDisposable
	{
		private const string connectionString = @"Data Source=(LocalDB)\v11.0;AttachDbFilename=D:\wiki.mdf;Integrated Security=True;";

		private readonly SqlConnection connection;

		public SqlDatabase()
		{
			connection = new SqlConnection(connectionString);
			connection.Open();
		}

		public void Load(string file)
		{
			using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
			using (var reader = new StreamReader(stream))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					Utils.UpdateProgress(stream);
					var command = connection.CreateCommand();
					command.CommandText = line;
					try
					{
						command.ExecuteNonQuery();
					}
					catch (SqlException e)
					{
						Console.WriteLine("\nSql command failed: " + line);
						Console.WriteLine(e);
					}
				}
			}
		}

		public void Dispose()
		{
			connection.Close();
		}
	}
}
