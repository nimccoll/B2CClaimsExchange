﻿using AADB2C.GraphService;
using AADB2C.UserMigration.Models;
//using Microsoft.WindowsAzure.Storage;
//using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AADB2C.UserMigration
{
    class Program
    {
        public static string Tenant = ConfigurationManager.AppSettings["b2c:Tenant"];
        public static string ClientId = ConfigurationManager.AppSettings["b2c:ClientId"];
        public static string ClientSecret = ConfigurationManager.AppSettings["b2c:ClientSecret"];
        public static string MigrationFile = ConfigurationManager.AppSettings["MigrationFile"];
        public static string BlobStorageConnectionString = ConfigurationManager.AppSettings["BlobStorageConnectionString"];

        static void Main(string[] args)
        {

            if (args.Length <= 0)
            {
                Console.WriteLine("Please enter a command as the first argument.");
                Console.WriteLine("\t1                 : Migrate social and local accounts with password");
                Console.WriteLine("\t2                 : Migrate social and local accounts with random password");
                Console.WriteLine("\t3 Email-address  : Get user by email address");
                Console.WriteLine("\t4 Display-name   : Get user by display name");
                Console.WriteLine("\t5                : User migration cleanup");
                return;
            }

            try
            {
                switch (args[0])
                {
                    case "1":
                        MigrateUsersWithPasswordAsync().Wait();
                        break;
                    case "2":
                        MigrateUsersWithRandomPasswordAsync().Wait();
                        break;
                    case "3":
                        if (args.Length == 2)
                        {
                            B2CGraphClient b2CGraphClient = new B2CGraphClient(Program.Tenant, Program.ClientId, Program.ClientSecret);
                            string JSON = b2CGraphClient.SearcUserBySignInNames(args[1]).Result;

                            Console.WriteLine(JSON);
                            GraphAccounts users = GraphAccounts.Parse(JSON);

                        }
                        else
                        {
                            Console.WriteLine("Email address parameter is missing");
                        }
                        break;
                    case "4":
                        if (args.Length == 2)
                        {
                            B2CGraphClient b2CGraphClient = new B2CGraphClient(Program.Tenant, Program.ClientId, Program.ClientSecret);
                            string JSON = b2CGraphClient.SearchUserByDisplayName(args[1]).Result;

                            Console.WriteLine(JSON);
                            GraphAccounts users = GraphAccounts.Parse(JSON);

                        }
                        else
                        {
                            Console.WriteLine("Display name parameter is missing");
                        }
                        break;
                    case "5":
                        UserMigrationCleanupAsync().Wait();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                if (ex.InnerException != null)
                {
                    Console.WriteLine(ex.InnerException.Message);
                }
                else
                {
                    Console.WriteLine(ex.Message);
                }
            }
            finally
            {
                Console.ResetColor();
            }

            Console.ReadLine();
        }

        /// <summary>
        /// Migrate users with their password
        /// </summary>
        /// <returns></returns>
        static async Task MigrateUsersWithPasswordAsync()
        {
            string appDirecotyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string dataFilePath = Path.Combine(appDirecotyPath, Program.MigrationFile);

            // Check file existence 
            if (!File.Exists(dataFilePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"File '{dataFilePath}' not found");
                Console.ResetColor();
                return;
            }

            // Read the data file and convert to object
            LocalAccountsModel users = LocalAccountsModel.Parse(File.ReadAllText(dataFilePath));

            // Create B2C graph client object 
            B2CGraphClient b2CGraphClient = new B2CGraphClient(Program.Tenant, Program.ClientId, Program.ClientSecret);

            int successes = 0;
            int fails = 0;
            foreach (var item in users.Users)
            {
                bool success = await b2CGraphClient.CreateAccount(
                    users.userType,
                    item.signInName,
                    item.issuer,
                    item.issuerUserId,
                    item.email,
                    item.password,
                    item.displayName,
                    item.firstName,
                    item.lastName,
                    item.extension_jdrfConsId,
                    false);

                if (success)
                    successes += 1;
                else
                    fails += 1;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\r\nUsers migration report:\r\n\tSuccesses: {successes}\r\n\tFails: {fails} ");
            Console.ResetColor();
        }

        /// <summary>
        /// Migrate users with random password
        /// </summary>
        /// <returns></returns>
        static async Task MigrateUsersWithRandomPasswordAsync()
        {
            string appDirecotyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string dataFilePath = Path.Combine(appDirecotyPath, Program.MigrationFile);

            // Check file existence 
            if (!File.Exists(dataFilePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"File '{dataFilePath}' not found");
                Console.ResetColor();
                return;
            }

            // Read the data file and convert to object
            LocalAccountsModel users = LocalAccountsModel.Parse(File.ReadAllText(dataFilePath));

            // Create B2C graph client object 
            B2CGraphClient b2CGraphClient = new B2CGraphClient(Program.Tenant, Program.ClientId, Program.ClientSecret);

            //// Parse the connection string and return a reference to the storage account.
            //CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Program.BlobStorageConnectionString);

            //// Create the table client.
            //CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            //// Retrieve a reference to the table.
            //CloudTable table = tableClient.GetTableReference("users");

            //// Create the table if it doesn't exist.
            //table.CreateIfNotExists();

            //// Create the batch operation.
            //TableBatchOperation batchOperation = new TableBatchOperation();

            int successes = 0;
            int fails = 0;

            foreach (var item in users.Users)
            {
                bool success = await b2CGraphClient.CreateAccount(users.userType,
                    item.signInName,
                    item.issuer,
                    item.issuerUserId,
                    item.email,
                    item.password,
                    item.displayName,
                    item.firstName,
                    item.lastName,
                    item.extension_jdrfConsId,
                    true);

                //// Create a new customer entity.
                //// Note: Azure Blob Table query is case sensitive, always set the email to lower case
                //TableEntity user = new TableEntity("B2CMigration", item.email.ToLower());

                //// Create the TableOperation object that inserts the customer entity.
                //TableOperation insertOperation = TableOperation.InsertOrReplace(user);

                //// Execute the insert operation.
                //table.Execute(insertOperation);

                if (success)
                    successes += 1;
                else
                    fails += 1;

            }


            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\r\nUsers migration report:\r\n\tSuccesses: {successes}\r\n\tFails: {fails} ");
            Console.ResetColor();
        }

        /// <summary>
        /// Migration clean up
        /// </summary>
        /// <returns></returns>
        static async Task UserMigrationCleanupAsync()
        {
            string appDirecotyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string dataFilePath = Path.Combine(appDirecotyPath, Program.MigrationFile);

            // Check file existence 
            if (!File.Exists(dataFilePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"File '{dataFilePath}' not found");
                Console.ResetColor();
                return;
            }

            // Read the data file and convert to object
            LocalAccountsModel users = LocalAccountsModel.Parse(File.ReadAllText(dataFilePath));

            // Create B2C graph client object 
            B2CGraphClient b2CGraphClient = new B2CGraphClient(Program.Tenant, Program.ClientId, Program.ClientSecret);

            foreach (var item in users.Users)
            {
                Console.WriteLine($"Deleting user '{item.email}'");
                await b2CGraphClient.DeleteAADUserBySignInNames(item.email);
            }
        }
    }

}
