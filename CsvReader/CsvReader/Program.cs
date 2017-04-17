using CsvReader.EntityObjects;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvReader
{
    class Program
    {
        private static string AppLocation = System.AppDomain.CurrentDomain.BaseDirectory;


        private static string filelocation = Path.Combine(AppLocation, "Files");
        private static OdbcConnection oConn;
        private static OdbcCommand oCmd;
        static void Main(string[] args)
        {
            //  we are going to run this test for EACH file below.
            //  These files have 1, 10 & 100 thousand fake addresses each
            string[] files = { "1.csv", "1k.csv", "10k.csv", "100k.csv"};

            //  This is the connection string for accessing the local database
            var connectionstring = @"Data Source=(localdb)\mssqllocaldb;Initial Catalog=BulkInsertTest;Integrated Security=True";
            Stopwatch sw = new Stopwatch();

            foreach (var filename in files)
            {
                if (filename !="100k.csv")
                {
                    sw.Start();
                    EFInsertsTest(filename);
                    sw.Stop();
                    Console.WriteLine($"Total for EF Inserts {sw.Elapsed} - {filename}");
                    sw.Reset();

                    sw.Start();
                    EFInsertsNoChangeTrackingTest(filename);
                    sw.Stop();
                    Console.WriteLine($"Total for EF Inserts w/0 Change Tracking {sw.Elapsed} - {filename}");
                    sw.Reset();

                    sw.Start();
                    EFInsertsNoChangeTrackingOrValidateTest(filename);
                    sw.Stop();
                    Console.WriteLine($"Total for EF Inserts w/0 Change Tracking Or Validation {sw.Elapsed} - {filename}");
                    sw.Reset();
                }

                sw.Start();
                AdoInsertsTest(filename, connectionstring);
                sw.Stop();
                Console.WriteLine($"Total for ADO.NET Inserts {sw.Elapsed} - {filename}");
                sw.Reset();

                sw.Start();
                AdoSqlBulkCopyTest(connectionstring, filename);
                sw.Stop();
                Console.WriteLine($"Total for SqlBulkCopy Inserts {sw.Elapsed} - {filename}");
                sw.Reset();

                sw.Start();
                AdoSqlBulkCopyTestTableLockTest(connectionstring, filename);
                sw.Stop();
                Console.WriteLine($"Total for SqlBulkCopy Inserts W/Table Lock {sw.Elapsed} - {filename}");
                sw.Reset();
                Console.WriteLine("");
            }
        }

        #region Tests
        private static void EFInsertsTest(string filename)
        {
            //  Stream and reader to read in the CSV file.
            var strm = new FileStream(Path.Combine(filelocation, filename), FileMode.Open);
            var rdr = new StreamReader(strm);

            //  Skip column headings line...
            var line = rdr.ReadLine();

            //  ready
            while (!rdr.EndOfStream)
            {
                // read each line in
                line = rdr.ReadLine();

                //  parse each item into an element of an array
                var literals = ExtractLiterals(line);

                //  build sql statement and execute
                var newAddr = new Address()
                {
                    FirstName = literals[0],
                    LastName = literals[1],
                    Address1 = literals[2],
                    City = literals[3],
                    State = literals[4],
                    zip = literals[5]
                };

                using (var ctx = new BulkInsertTestCtx())
                {
                    ctx.Addresses.Add(newAddr);
                    ctx.SaveChanges();
                }
            }

            // cleanup
            rdr.Close();
            strm.Close();
        }
        private static void EFInsertsNoChangeTrackingTest(string filename)
        {
            //  Stream and reader to read in the CSV file.
            var strm = new FileStream(Path.Combine(filelocation, filename), FileMode.Open);
            var rdr = new StreamReader(strm);

            //  Skip column headings
            var line = rdr.ReadLine();

            //  ready
            while (!rdr.EndOfStream)
            {
                // read each line in
                line = rdr.ReadLine();

                //  parse each item into an element of an array
                var literals = ExtractLiterals(line);

                //  build sql statement and execute
                var newAddr = new Address()
                {
                    FirstName = literals[0],
                    LastName = literals[1],
                    Address1 = literals[2],
                    City = literals[3],
                    State = literals[4],
                    zip = literals[5]
                };
                using (var ctx = new BulkInsertTestCtx())
                {
                    //  Turn off Auto Detect changes
                    ctx.Configuration.AutoDetectChangesEnabled = false;
                    ctx.Addresses.Add(newAddr);
                    ctx.SaveChanges();
                }
            }

            // cleanup
            rdr.Close();
            strm.Close();
        }
        private static void EFInsertsNoChangeTrackingOrValidateTest(string filename)
        {
            //  Stream and reader to read in the CSV file.
            var strm = new FileStream(Path.Combine(filelocation, filename), FileMode.Open);
            var rdr = new StreamReader(strm);

            //  Skip column headings
            var line = rdr.ReadLine();

            //  ready
            while (!rdr.EndOfStream)
            {
                // read each line in
                line = rdr.ReadLine();

                //  parse each item into an element of an array
                var literals = ExtractLiterals(line);

                //  build sql statement and execute
                var newAddr = new Address()
                {
                    FirstName = literals[0],
                    LastName = literals[1],
                    Address1 = literals[2],
                    City = literals[3],
                    State = literals[4],
                    zip = literals[5]
                };
                using (var ctx = new BulkInsertTestCtx())
                {
                    // Turn off Auto Detect changes AND Validate On Save
                    ctx.Configuration.AutoDetectChangesEnabled = false;
                    ctx.Configuration.ValidateOnSaveEnabled = false;
                    ctx.Addresses.Add(newAddr);
                    ctx.SaveChanges();
                }
            }

            // cleanup
            rdr.Close();
            strm.Close();
        }
        private static void AdoInsertsTest(string filename, string connectionstring)
        {
            //  Stream and reader to read in the CSV file.
            var strm = new FileStream(Path.Combine(filelocation, filename), FileMode.Open);
            var rdr = new StreamReader(strm);

            //  Sql Connection and Command to insert the records
            var con = new SqlConnection(connectionstring);
            var cmd = con.CreateCommand();

            //  Skip column headings
            var line = rdr.ReadLine();

            //  ready
            con.Open();
            while (!rdr.EndOfStream)
            {
                // read each line in
                line = rdr.ReadLine();

                //  parse each item into an element of an array
                var literals = ExtractLiterals(line);

                //  build sql statement and execute
                var sql = $"insert into addresses select '{literals[0]}', '{literals[1]}', '{literals[2]}', '{literals[3]}', '{literals[4]}', '{literals[5]}'";
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }

            // cleanup
            rdr.Close();
            strm.Close();
            con.Close();
            cmd.Dispose();
            con.Dispose();
        }
        private static void AdoSqlBulkCopyTest(string connectionstring, string filename)
        {
            //  Create a SqlBulkCopy instance - Optimized to stream 
            //  massive amounts of records to the SQL Server
            SqlBulkCopy cpy = new SqlBulkCopy(connectionstring);
            cpy.BatchSize = 1000;
            cpy.DestinationTableName = "Addresses";
            cpy.WriteToServer(GetCsvReader(filename));
        }
        private static void AdoSqlBulkCopyTestTableLockTest(string connectionstring, string filename)
        {
            SqlBulkCopy cpy = new SqlBulkCopy(connectionstring, SqlBulkCopyOptions.TableLock);
            cpy.BatchSize = 1000;
            cpy.DestinationTableName = "Addresses";
            cpy.WriteToServer(GetCsvReader(filename));
        }
        #endregion

        private static OdbcDataReader GetCsvReader(string file)
        {
            //  This is the connection string for CSV file ODBC driver.
            //  Sets of a 
            string strConnString = $"Driver={{Microsoft Text Driver (*.txt; *.csv)}};Dbq={filelocation};Extensions=asc,csv,tab,txt;Persist Security Info=False";

            //  Connect...
            oConn = new OdbcConnection(strConnString);
            oCmd = oConn.CreateCommand();

            oCmd.CommandType = System.Data.CommandType.Text;

            //  Very specific way to select * from csv file  (Square brackets around it)
            oCmd.CommandText = $"select * from [{file}]";  //  For Visual Studio < 2015 ->  string.Format("Select * from [{0}]", file)
            oConn.Open();
            return oCmd.ExecuteReader();
        }
        private static string[] ExtractLiterals(string line)
        {
            //  This logic ASSUMES that each item in your CSV files has a delimiter of "
            //  If yours does not, you'll have to toy with this thing.
            //  With a comma delimited file...no need to do so...
            //  UNLESS your data fields themselves have a comma in them...mine do.
            List<string> literals = new List<string>();
            string buffer = string.Empty;
            bool literalStart = false;
            foreach (var item in line)
            {
                //  if we aren't already in the middle of a literal and we spot "
                //  mark to start adding to buffer
                if (!literalStart && item == '\"')
                {
                    literalStart = true;
                }
                //  if we are in the middle of a literal and we spot anything except "
                //  add it to the buffer
                else if (literalStart && item != '\"')
                {
                    if (item == '\'')
                    {
                        buffer += "''";
                    }
                    else
                    {
                        buffer += item;
                    }
                }
                //  if we get to here and find a \", time to end the literal and reset
                //  until we find the start of the next one.
                else if (item == '\"')
                {
                    literalStart = false;
                    literals.Add(buffer);
                    buffer = string.Empty;
                }
            }
            return literals.ToArray();
        }
    }
}
