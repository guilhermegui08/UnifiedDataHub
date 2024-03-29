using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Data.SqlClient;
using System.Security.AccessControl;
using MiddlewareDatabaseAPI.Models;
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Drawing.Drawing2D;
using System.Xml.Linq;
using System.Web.Management;
using uPLibrary.Networking.M2Mqtt;
using System.Text;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading.Tasks;


namespace MiddlewareDatabaseAPI.Controllers
{
    [RoutePrefix("api/somiod")]
    public class ContainerController : SomiodController
    {

        [Route("{application}/{container}")]
        [HttpGet]
        public IHttpActionResult GetContainerOrAllDataOrAllSubscriptions(string application, string container)
        {

            int[] values = VerifyOwnership(application, container);
            if (values[0] == 0)
                return BadRequest("Application doesn't exist");
            if (values[0] != values[1])
                return BadRequest("Container doesn't belong to App");


            HttpRequestMessage request = Request;
            //Verificação se no cabeçalho do Header existe a opção somiod-discover
            if (request.Headers.TryGetValues("somiod-discover", out IEnumerable<string> headerValues))
            {

                string somiodDiscoverHeaderValue = headerValues.FirstOrDefault();

                //Verificação se no cabeçalho do Header a opção somiod-discover têm o valor data
                if (string.Equals(somiodDiscoverHeaderValue, "data", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        List<string> listOfDatas = new List<string>();
                        string queryString = "SELECT name FROM Data WHERE parent=(SELECT id FROM Container WHERE name=@nameContainer AND parent=(SELECT id FROM Application WHERE name=@nameApplication))";

                        using (SqlConnection connection = new SqlConnection(connStr))
                        {
                            SqlCommand command = new SqlCommand(queryString, connection);
                            connection.Open();
                            command.Parameters.AddWithValue("@nameContainer", container);
                            command.Parameters.AddWithValue("@nameApplication", application);


                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    listOfDatas.Add((string)reader["name"]);
                                }
                            }
                        }

                        return Ok(listOfDatas);

                    }
                    catch (Exception)
                    {
                        return InternalServerError();
                    }
                }    //Verificação se no cabeçalho do Header a opção somiod-discover têm o valor subscription
                else if (string.Equals(somiodDiscoverHeaderValue, "subscription", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        List<string> listOfSubscriptions = new List<string>();
                        string queryString = "SELECT name FROM Subscription WHERE parent=(SELECT id FROM Container WHERE name=@nameContainer AND parent=(SELECT id FROM Application WHERE name=@nameApplication))";

                        using (SqlConnection connection = new SqlConnection(connStr))
                        {
                            SqlCommand command = new SqlCommand(queryString, connection);
                            connection.Open();
                            command.Parameters.AddWithValue("@nameContainer", container);
                            command.Parameters.AddWithValue("@nameApplication", application);


                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    listOfSubscriptions.Add((string)reader["name"]);
                                }
                            }
                        }

                        return Ok(listOfSubscriptions);

                    }
                    catch (Exception)
                    {
                       return InternalServerError();
                    }
                }
                else
                {
                    return BadRequest("Invalid value for somiod-discover header");
                }
            }
            else
            {
                try
                {
                    string queryString = "SELECT * FROM Container WHERE name = @nameContainer AND parent=(SELECT id FROM Application WHERE name=@nameApplication)";

                    using (SqlConnection connection = new SqlConnection(connStr))
                    {
                        SqlCommand command = new SqlCommand(queryString, connection);
                        connection.Open();
                        command.Parameters.AddWithValue("@nameContainer", container);
                        command.Parameters.AddWithValue("@nameApplication", application);


                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Container cont = new Container
                                {
                                    id = (int)reader["id"],
                                    name = (string)reader["name"],
                                    creation_dt = (DateTime)reader["creation_dt"],
                                    parent = (int)reader["parent"]
                                };
                                return Ok(cont);
                            }
                        }
                    }
                    return NotFound();
                }
                catch (Exception ex)
                {
                    return InternalServerError(ex);
                }
            }

        }

        
        [Route("{application}/{container}")]
        [HttpPut]
        public IHttpActionResult PutContainer(string application, string container, [FromBody] Container value)
        {
            int[] values = VerifyOwnership(application, container);
            if (values[0] == 0)
                return BadRequest("Application doesn't exist");
            if (values[0] != values[1])
                return BadRequest("Container doesn't belong to App");

            if (value == null)
                return BadRequest("The request body is empty.");

            if (value.name == null || value.name == "")
                return BadRequest("The 'name' parameter is null.");

            bool flag = false;
            string nameValue;
            if (!UniqueName(value.name, "Container"))
            {
                flag = true;
                nameValue = NewName(value.name, "Container");
            }
            else
                nameValue = value.name;

            string queryString = "UPDATE Container SET name=@name WHERE id=@idCont";

            try
            {
                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    SqlCommand command = new SqlCommand(queryString, connection);
                    command.Parameters.AddWithValue("@idCont", values[2]);
                    command.Parameters.AddWithValue("@name", nameValue);

                    try
                    {
                        command.Connection.Open();
                        if (command.ExecuteNonQuery() == 0)
                        {
                            return NotFound();
                        }
                        else
                        {                          
                            return flag ? Ok("Container " + nameValue + " was edited - specified name in use") : Ok("Container " + nameValue + " was edited");
                        }

                    }
                    catch (Exception) 
                    {
                        return InternalServerError();
                    }
                
                }

            }catch(Exception)
            {
                return InternalServerError();
            }
        }

        [Route("{application}/{container}")]
        [HttpDelete]
        public IHttpActionResult DeleteContainer(string application, string container)
        {

            int[] values = VerifyOwnership(application, container);
            if (values[0] == 0)
                return BadRequest("Application doesn't exist");
            if (values[0] != values[1])
                return BadRequest("Container doesn't belong to App");

            DeleteDataOrSubscription(container, "Data");
            DeleteDataOrSubscription(container, "Subscription");

            try
            {
                

                using (SqlConnection connection = new SqlConnection(connStr))
                {
                        string queryString = "DELETE FROM Container WHERE name=@name";
                        SqlCommand command = new SqlCommand(queryString, connection);
                        command.Parameters.AddWithValue("@name", container);

                        try
                        {
                            command.Connection.Open();
                            int rows = command.ExecuteNonQuery();

                            if (rows > 0)
                            {
                                return Ok("Container " + container + " deleted");
                            }
                            else
                            {
                               return NotFound();
                            }
                        }
                        catch (Exception ex)
                        {
                            return InternalServerError(ex);
                        }
                }

            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
            

        }

        public void DeleteDataOrSubscription(string container, string table)
        {
            string queryDeleteData = "DELETE FROM " + table + " WHERE parent=(SELECT id FROM Container WHERE name=@nameContainer)";

            using (SqlConnection connection = new SqlConnection(connStr))
            {
                SqlCommand commandDelete = new SqlCommand(queryDeleteData, connection);
                commandDelete.Parameters.AddWithValue("@nameContainer", container);

                try
                {
                    commandDelete.Connection.Open();
                    commandDelete.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    InternalServerError(ex);
                }
            }
        }

        [Route("{application}/{container}")]
        [HttpPost]
        public IHttpActionResult PostDataOrSubscription(string application, string container, [FromBody] DataOrSubscription value)
        {
            if (value == null)
                return BadRequest("The request body is empty.");

            if (value.name == "" || value.name == null)
                return BadRequest("The 'name' parameter is empty.");

            if (value.res_type == "" || value.res_type == null)
                return BadRequest("The 'res_type' parameter is null. Must be either 'data' or 'subscription'");

            int[] values = VerifyOwnership(application, container);
            if (values[0] == 0)
                return BadRequest("Application doesn't exist");
            if (values[0] != values[1])
                return BadRequest("Container doesn't belong to App");

            value.parent = values[2];
            DataAndSubscriptionController controller = new DataAndSubscriptionController();
            string[] result;
            bool flag = true;

            if (value.res_type == "data")
            {
                if (value.content == null || value.content == "")
                    return BadRequest("Error - Trying to create Data with empty content");

                string nameValue;
                if (!UniqueName(value.name, "Data"))
                {

                    nameValue = NewName(value.name, "Data");
                }
                else
                    nameValue = value.name;

                result = controller.PostData(new Data { name = nameValue, content = value.content, parent = value.parent });
                flag = false;

                try
                {
                    Data d = getData(nameValue);

                    List<string> listOfEndpoints = new List<string>();
                    string queryString = "SELECT endpoint FROM Subscription WHERE parent=@parent AND event='1'";

                    using (SqlConnection connection = new SqlConnection(connStr))
                    {
                        SqlCommand command = new SqlCommand(queryString, connection);
                        connection.Open();
                        command.Parameters.AddWithValue("@parent", values[2]);


                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                listOfEndpoints.Add((string)reader["endpoint"]);
                            }
                        }
                    }

                    string topic = "api/somiod/" + application + "/" + container;
                    string xmlContent = "<Data>\r\n   <event>criation<envent>\r\n   <content>" + d.content + "</content>\r\n    <creation_dt>" + d.creation_dt.ToString() + "</creation_dt>\r\n " +
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

                }
                catch (Exception)
                {
                    return InternalServerError();
                }

            }
            else if (value.res_type == "subscription")
            {

                if (value.event_mqtt != "1" && value.event_mqtt != "2")
                    return BadRequest("Error - Trying to create Subscription with wrong value for event (1,2)");

                if (value.endpoint == null || value.endpoint == "")
                    return BadRequest("Error - Trying to create Subscription with empty endpoint");

                Subscription sub = new Subscription { name = value.name, event_mqqt = value.event_mqtt, endpoint = value.endpoint, parent = value.parent };

                result = controller.PostSubscription(sub);
            }
            else
            {
                return BadRequest("Invalid res_type");
            }

            switch (result[0])
            {
                case "-1":
                    return InternalServerError();
                case "0":
                    return NotFound();
                default:
                    return flag ? Ok("Subscription " + result[1] + " created") : Ok("Data " + result[1] + " created");
            }
        }

    }
}