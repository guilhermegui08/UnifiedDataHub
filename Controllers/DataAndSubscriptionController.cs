using System;
using MiddlewareDatabaseAPI.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Data.SqlClient;
using System.Security.AccessControl;
using static System.Net.Mime.MediaTypeNames;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Http.Results;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using System.IO;

namespace MiddlewareDatabaseAPI.Controllers
{
    [RoutePrefix("api/somiod")]
    public class DataAndSubscriptionController : SomiodController
    {

        [Route("{application}/{container}/data/{data}")]
        [HttpGet]
        public IHttpActionResult GetData(string application, string container, string data)
        {
            int[] values = VerifyOwnership(application, container);
            if (values[0] == 0)
                return BadRequest("Application doesn't exist");
            if (values[0] != values[1])
                return BadRequest("Container doesn't belong to App");

            if (VerifyDataOrSubContainer(container, data, true))
                return BadRequest("Data doesn't belong to Container");

            string queryString = "SELECT * FROM Data WHERE name = @data";

            using (SqlConnection connection = new SqlConnection(connStr))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                command.Parameters.AddWithValue("@data", data);

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
                        return Ok(d);
                    }
                }
            }
            return NotFound();
        }

        [Route("{application}/{container}/subscription/{subscription}")]
        [HttpGet]
        public IHttpActionResult GetSubscription(string application, string container, string subscription)
        {

            int[] values = VerifyOwnership(application, container);
            if (values[0] == 0)
                return BadRequest("Application doesn't exist");
            if (values[0] != values[1])
                return BadRequest("Container doesn't belong to App");

            if (VerifyDataOrSubContainer(container, subscription, false))
                return BadRequest("Subscription doesn't belong to Container");

            string queryString = "SELECT * FROM Subscription WHERE name = @subscription";

            using (SqlConnection connection = new SqlConnection(connStr))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                command.Parameters.AddWithValue("@subscription", subscription);

                command.Connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        Subscription s = new Subscription
                        {
                            id = (int)reader["id"],
                            name = (string)reader["name"],
                            event_mqqt = (string)reader["event"],
                            endpoint = (string)reader["endpoint"],
                            creation_dt = (DateTime)reader["creation_dt"],
                            parent = (int)reader["parent"]
                        };
                        return Ok(s);
                    }
                }
            }
            return NotFound();
        }

        public string[] PostData(Data value)
        {

            string queryString = "INSERT INTO Data (name, content, parent, creation_dt) VALUES (@name, @content, @parent, @creation_dt)";

            using (SqlConnection connection = new SqlConnection(connStr))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                command.Parameters.AddWithValue("@name", value.name);
                command.Parameters.AddWithValue("@content", value.content);
                command.Parameters.AddWithValue("@parent", value.parent);
                DateTime now = DateTime.UtcNow;
                string isoDateTimeString = now.ToString("yyyy-MM-dd HH:mm:ss");

                command.Parameters.AddWithValue("@creation_dt", isoDateTimeString);

                try
                {
                    command.Connection.Open();
                    int result = command.ExecuteNonQuery();
                    if (result == 0)
                    {
                        return new string[] { "0", "" };
                    }
                    else
                    {
                        return new string[] { "1", value.name };
                    }
                }
                catch (Exception)
                {
                    return new string[] { "-1", "" };
                }
            }
        }

        public string[] PostSubscription(Subscription value)
        {

            string nameValue;
            if (!UniqueName(value.name, "Subscription"))
            {
                nameValue = NewName(value.name, "Subscription");
            }
            else
                nameValue = value.name;

            string queryString = "INSERT INTO Subscription (name, event, endpoint, parent, creation_dt) VALUES (@name, @event, @endpoint, @parent, @creation_dt)";

            using (SqlConnection connection = new SqlConnection(connStr))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                command.Parameters.AddWithValue("@name", nameValue);
                command.Parameters.AddWithValue("@event", value.event_mqqt);
                command.Parameters.AddWithValue("@endpoint", value.endpoint);
                command.Parameters.AddWithValue("@parent", value.parent);
                DateTime now = DateTime.UtcNow;
                string isoDateTimeString = now.ToString("yyyy-MM-dd HH:mm:ss");
                command.Parameters.AddWithValue("@creation_dt", isoDateTimeString);

                try
                {
                    command.Connection.Open();
                    int result = command.ExecuteNonQuery();
                    if (result == 0)
                    {
                        return new string[] { "0", "" };
                    }
                    else
                    {
                        return new string[] { "1", nameValue };
                    }
                }
                catch (Exception)
                {
                    return new string[] { "-1", "" };
                }
            }
        }

        [Route("{application}/{container}/data/{data}")]
        [HttpDelete]
        public IHttpActionResult DeleteData(string application, string container, string data)
        {

            int[] values = VerifyOwnership(application, container);
            if (values[0] == 0)
                return BadRequest("Application doesn't exist");
            if (values[0] != values[1])
                return BadRequest("Container doesn't belong to App");

            if (VerifyDataOrSubContainer(container, data, true))
                return BadRequest("Data doesn't belong to Container");

            Data d = getData(data);

            try
            {
                string queryString = "DELETE Data WHERE name=@name";

                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    SqlCommand command = new SqlCommand(queryString, connection);
                    command.Parameters.AddWithValue("@name", data);

                    try
                    {
                        command.Connection.Open();
                        int rows = command.ExecuteNonQuery();
                        if (rows > 0)
                        {
                            
                            List<string> listOfEndpoints = new List<string>();
                            string queryString2 = "SELECT endpoint FROM Subscription WHERE parent=@parent AND event='2'";

                            using (SqlConnection connection2 = new SqlConnection(connStr))
                            {
                                SqlCommand command2 = new SqlCommand(queryString2, connection2);
                                connection2.Open();
                                command2.Parameters.AddWithValue("@parent", values[2]);


                                using (SqlDataReader reader = command2.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        listOfEndpoints.Add((string)reader["endpoint"]);
                                    }
                                }
                            }

                            string topic = "api/somiod/" + application + "/" + container;
                            string xmlContent = "<Data>\r\n   <event>deletion<envent>\r\n   <content>" + d.content + "</content>\r\n    <creation_dt>" + d.creation_dt.ToString() + "</creation_dt>\r\n " +
                                "   <id>" + d.id.ToString() + "</id>\r\n    <name>" + d.name + "</name>\r\n    <parent>" + d.parent.ToString() + "</parent>\r\n</Data>";
                            byte[] msg = Encoding.UTF8.GetBytes(xmlContent);

                            string httpPattern = @"^http:\/\/";
                            string httpsPattern = @"^https:\/\/";
                            string mqttPattern = @"^mqtt:\/\/";

                            foreach (string endpoint in listOfEndpoints)
                            {
                                if (Regex.IsMatch(endpoint, mqttPattern))
                                {
                                    //string ipAddressPattern = @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}?:(\d+)";
                                    string ipAddressPattern = @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b";
                                    Match ipAddressMatch = Regex.Match(endpoint, ipAddressPattern);

                                    if (ipAddressMatch.Success)
                                    {
                                        string ipAddress = ipAddressMatch.Value;
                                        MqttClient client = new MqttClient(ipAddress);
                                        client.Connect(Guid.NewGuid().ToString());
                                        if (client.IsConnected)
                                        {
                                            client.Publish(topic, msg);
                                        }
                                    }
                                }
                                if (Regex.IsMatch(endpoint, httpPattern) || Regex.IsMatch(endpoint, httpsPattern))
                                {
                                    if (IsServerAvailable(endpoint))
                                    {
                                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
                                        request.Method = "POST";
                                        byte[] byteArray = Encoding.UTF8.GetBytes(xmlContent);
                                        request.ContentLength = byteArray.Length;
                                        using (Stream dataStream = request.GetRequestStream())
                                        {
                                            dataStream.Write(byteArray, 0, byteArray.Length);
                                        }
                                    }
                                }
                            }

                            return Ok("Data " + data + " deleted");
                        }
                        else
                        {
                            return NotFound();
                        }
                    }
                    catch (Exception)
                    {
                        return InternalServerError();
                    }
                }
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        [Route("{application}/{container}/subscription/{subscription}")]
        [HttpDelete]
        public IHttpActionResult DeleteSubscription(string application, string container, string subscription)
        {

            int[] values = VerifyOwnership(application, container);
            if (values[0] == 0)
                return BadRequest("Application doesn't exist");
            if (values[0] != values[1])
                return BadRequest("Container doesn't belong to App");

            if (VerifyDataOrSubContainer(container, subscription, false))
                return BadRequest("Subscription doesn't belong to Container");

            try
            {
                string queryString = "DELETE Subscription WHERE name=@name";

                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    SqlCommand command = new SqlCommand(queryString, connection);
                    command.Parameters.AddWithValue("@name", subscription);

                    try
                    {
                        command.Connection.Open();
                        int rows = command.ExecuteNonQuery();
                        if (rows > 0)
                        {
                            return Ok("Subscription " + subscription + " deleted");
                        }
                        else
                        {
                            return NotFound();
                        }
                    }
                    catch (Exception)
                    {
                        return InternalServerError();
                    }
                }
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        private bool VerifyDataOrSubContainer(string container, string data, bool flag)
        {
            int idCont = 0, idDataOrSubParent = 0;
            string queryCont = "SELECT id FROM Container WHERE name = @nameContainer";
            string queryDataOrSubparent = flag ? "SELECT parent FROM Data WHERE name = @name" : "SELECT parent FROM Subscription WHERE name = @name";

            using (SqlConnection connection = new SqlConnection(connStr))
            {

                SqlCommand commandCont = new SqlCommand(queryCont, connection);
                commandCont.Parameters.AddWithValue("@nameContainer", container);
                commandCont.Connection.Open();

                try
                {
                    using (SqlDataReader reader = commandCont.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            idCont = (int)reader["id"];
                        }
                    }
                }
                catch (Exception)
                {
                    InternalServerError();
                }


                commandCont.Connection.Close();

                SqlCommand commandDataP = new SqlCommand(queryDataOrSubparent, connection);
                commandDataP.Parameters.AddWithValue("@name", data);
                commandDataP.Connection.Open();

                try
                {
                    using (SqlDataReader reader = commandDataP.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            idDataOrSubParent = (int)reader["parent"];
                        }
                    }
                }
                catch (Exception)
                {
                    InternalServerError();
                }

            }

            if (idDataOrSubParent == idCont)
            {
                return false;
            }

            return true;
        }

    }
}