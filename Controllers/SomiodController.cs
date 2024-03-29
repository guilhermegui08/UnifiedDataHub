using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using MiddlewareDatabaseAPI.Models;

namespace MiddlewareDatabaseAPI.Controllers
{
    public abstract class SomiodController : ApiController
    {
        protected string connStr = Properties.Settings.Default.ConnStr;
        protected int GetAppId(string applicationName)
        {
            int id = 0;
            string queryApp = "SELECT id FROM Application WHERE name = @nameApplication";

            using (SqlConnection connection = new SqlConnection(connStr))
            {

                SqlCommand commandApp = new SqlCommand(queryApp, connection);
                commandApp.Parameters.AddWithValue("@nameApplication", applicationName);
                commandApp.Connection.Open();

                try
                {
                    using (SqlDataReader reader = commandApp.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            id = (int)reader["id"];
                        }
                    }
                }
                catch (Exception)
                {
                    InternalServerError();
                }

            }

            return id;
        }

        protected bool UniqueName(string nameValue, string table)
        {
            List<string> listOfApplications = new List<string>();
            string helpQuerryString = "SELECT name FROM " + table;

            try
            {
                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    SqlCommand command = new SqlCommand(helpQuerryString, connection);
                    command.Connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            listOfApplications.Add((string)reader["name"]);
                        }
                    }
                }
            }
            catch (Exception)
            {
                InternalServerError();
            }

            foreach (string name in listOfApplications)
            {
                if (name == nameValue)
                    return false;
            }

            return true;
        }

        protected string NewName(string nameValue, string table)
        {
            Random random = new Random();
            const string chars = "abcdefghijklmnopqrstuvwxyz";

            char[] word = new char[4];

            bool flag = true;
            while (flag)
            {

                for (int i = 0; i < 4; i++)
                {
                    word[i] = chars[random.Next(chars.Length)];
                }

                flag = !UniqueName(new String(word), table);
            }
            return nameValue + "_" + new String(word);
        }

        protected int[] VerifyOwnership(string application, string container)
        {
            int idApp = 0, idContParent = 0, idCont = 0;
            string queryApp = "SELECT id FROM Application WHERE name = @nameApplication";
            string queryCont = "SELECT id, parent FROM Container WHERE name = @nameContainer";

            using (SqlConnection connection = new SqlConnection(connStr))
            {
                SqlCommand commandApp = new SqlCommand(queryApp, connection);
                commandApp.Parameters.AddWithValue("@nameApplication", application);
                commandApp.Connection.Open();

                try
                {
                    using (SqlDataReader reader = commandApp.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            idApp = (int)reader["id"];
                        }
                    }
                }
                catch (Exception)
                {
                    InternalServerError();
                }
                commandApp.Connection.Close();

                if (idApp == 0)
                {
                    return new int[] { 0, 0, 0 };
                }

                SqlCommand commandCont = new SqlCommand(queryCont, connection);
                commandCont.Parameters.AddWithValue("@nameContainer", container);
                commandCont.Connection.Open();

                try
                {
                    using (SqlDataReader reader = commandCont.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            idContParent = (int)reader["parent"];
                            idCont = (int)reader["id"];
                        }
                    }
                }
                catch (Exception)
                {
                    InternalServerError();
                }
                commandCont.Connection.Close();

            }

            return new int[] { idApp, idContParent, idCont };
        }

        protected Data getData(string name)
        {
            string queryString = "SELECT * FROM Data WHERE name = @data";
            using (SqlConnection connection = new SqlConnection(connStr))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                command.Parameters.AddWithValue("@data", name);

                command.Connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        Data d = new Data
                        {
                            id = (int)reader["id"],
                            name = (string)reader["name"],
                            content = (string)reader["content"],
                            creation_dt = (DateTime)reader["creation_dt"],
                            parent = (int)reader["parent"]
                        };
                        return d;
                    }
                }
            }
            return null;
        }

        protected bool IsServerAvailable(string endpoint)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
                request.Timeout = 5000; // Set a timeout value

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}