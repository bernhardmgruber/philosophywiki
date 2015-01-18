using Common;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SqlServer
{
	sealed class SqlDatabase : IDisposable
	{
		private const string connectionString = @"Data Source=(LocalDB)\v11.0;AttachDbFilename=D:\wiki.mdf;Integrated Security=True;";

		private SqlConnection connection;

		public SqlDatabase()
		{
			connection = new SqlConnection(connectionString);
			connection.Open();
		}
		public void Load(string file, bool parallel = false)
		{
			//const int maxThreads = 8;
			const int batchSize = 64;
			//sem = new Semaphore(maxThreads, maxThreads);
			using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
			using (var reader = new StreamReader(stream))
			{
				string line;
				while (!reader.EndOfStream)
				{
					var commands = new List<string>();
					for (int i = 0; ((line = reader.ReadLine()) != null) && i < batchSize; i++)
						commands.Add(line);

					Utils.UpdateProgress(stream);
					RunCommand(String.Join(";", commands), parallel);
				}
			}

			//for (int i = 0; i < maxThreads; i++)
			//	sem.WaitOne();
			//sem.Dispose();
		}

		//private Semaphore sem;

		private void RunCommand(string sql, bool parallel)
		{
			//Action cmd = () =>
			//{
				try
				{
					//using (var connection = new SqlConnection(connectionString))
					//{
						//connection.Open();

						var command = connection.CreateCommand();
						command.CommandText = sql;
						command.ExecuteNonQuery();
					//}
				}
				catch (SqlException e)
				{
					Console.WriteLine("\nSql command failed: " + sql);
					Console.WriteLine(e);
				}
				//finally
				//{
				//	sem.Release(1);
				//}
			//};

			//sem.WaitOne();

			//if (parallel)
			//	Task.Run(cmd);
			//else
			//	cmd();
		}

		public void Dispose()
		{
			connection.Close();
		}
	}
}
