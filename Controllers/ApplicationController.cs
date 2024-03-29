using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Data.SqlClient;
using System.Security.AccessControl;
using MiddlewareDatabaseAPI.Models;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;
using Application = MiddlewareDatabaseAPI.Models.Application;
using System.ComponentModel;
using Container = MiddlewareDatabaseAPI.Models.Container;

namespace MiddlewareDatabaseAPI.Controllers

{
    [RoutePrefix("api/somiod")]


    public class ApplicationController : SomiodController
    {

        [Route("")]
        [HttpGet]
        public IHttpActionResult GetAllApplications()
        {
            // Verify header somiod-discover: application 
            HttpRequestMessage request = Request;
            if (request.Headers.TryGetValues("somiod-discover", out IEnumerable<string> headerValues))
            {
                string somiodDiscoverHeaderValue = headerValues.FirstOrDefault();

                if (string.Equals(somiodDiscoverHeaderValue, "application", StringComparison.OrdinalIgnoreCase))
                {
                    // Header is present and has the correct value
                    List<string> listOfApplications = new List<string>();
                    string queryString = "SELECT name FROM Application";

                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connStr))
                        {
                            SqlCommand command = new SqlCommand(queryString, connection);
                            command.Connection.Open();

                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string name = (string)reader["name"];
                                    listOfApplications.Add(name);
                                }
                            }
                        }
                        return Ok(listOfApplications);
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
                return BadRequest("somiod-discover header is missing");
            }
        }

        [Route("{application}")]
        [HttpGet]
        public IHttpActionResult GetApplicationOrContainers(string application)
        {
            int id_app = GetAppId(application);

            // aplicação nao existe
            if (id_app == 0)
                return BadRequest("App doesn't exist");

            // Verify header somiod-discover: container
            HttpRequestMessage request = Request;
            if (request.Headers.TryGetValues("somiod-discover", out IEnumerable<string> headerValues))
            {
                string somiodDiscoverHeaderValue = headerValues.FirstOrDefault();

                if (string.Equals(somiodDiscoverHeaderValue, "container", StringComparison.OrdinalIgnoreCase))
                {
                    // Header is present and has the correct value
                    List<string> listOfContainers = new List<string>();

                    try
                    {
                        listOfContainers = GetListContainersOfApp(id_app);
                    }
                    catch (Exception)
                    {
                        return InternalServerError();
                    }

                    return Ok(listOfContainers);
                }
                else
                {
                    // Header is present but has an incorrect value
                    return BadRequest("Invalid value for somiod-discover header");
                }
            }
            else
            {
                // Header is not present
                string queryString = "SELECT * FROM Application WHERE name = @nameApllication";
                try
                {
                    using (SqlConnection connection = new SqlConnection(connStr))
                    {
                        SqlCommand command = new SqlCommand(queryString, connection);
                        command.Parameters.AddWithValue("nameApllication", application);
                        command.Connection.Open();

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                                if (reader.Read())
                                {
                                    //convert the registo da BD para Application
                                    Application app = new Application
                                    {
                                        id = (int)reader["id"],
                                        name = (string)reader["name"],
                                        creation_dt = (DateTime)reader["creation_dt"]
                                    };

                                    return Ok(app);
                                }
                            }
                        }

                    return NotFound();
                }
                catch (Exception)
                {
                    return InternalServerError();
                }
            }
        }

        [Route("")]
        [HttpPost]
        public IHttpActionResult PostApplication([FromBody] Application value)
        {
            // TODO - criar container automaticamente ?? 
            // creation_dt inserido automaticamente, apenas necessário nome da app

            if (value == null)
                return BadRequest("The request body is empty.");

            if (value.name == null || value.name == "")
                return BadRequest("The 'name' parameter is null.");

            bool flag = false;
            string nameValue;
            if (!UniqueName(value.name, "Application"))
            {
                flag = true;
                nameValue = NewName(value.name, "Application");
            }
            else
                nameValue = value.name;
            
                

            string queryString = "INSERT INTO Application VALUES (@name, @creation_dt)";

            try
            {
                using (SqlConnection connection = new SqlConnection(connStr))
                {

                    SqlCommand command = new SqlCommand(queryString, connection);
                    DateTime now = DateTime.UtcNow;
                    string isoDateTimeString = now.ToString("yyyy-MM-dd HH:mm:ss");
                    command.Parameters.AddWithValue("@name", nameValue);
                    command.Parameters.AddWithValue("@creation_dt", isoDateTimeString);

                    try
                    {
                        command.Connection.Open();

                        if (command.ExecuteNonQuery() == 0)
                        {
                            return NotFound();
                        }
                        else
                        {
                            return flag ? Ok("App " + nameValue + " was created - specified name in use") : Ok("App " + nameValue + " was created");
                        }

                    }catch (Exception ex)
                    {
                        return InternalServerError(ex);
                    }
                }

            }catch(Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("{application}")]
        [HttpPut]
        public IHttpActionResult PutApplication(string application, [FromBody] Application value)
        {

            int id = GetAppId(application);

            // aplicação nao existe
            if (id == 0)
                return BadRequest("App doesn't exist");

            if (value == null)
                return BadRequest("The request body is empty.");

            if (value.name == null || value.name == "")
                return BadRequest("The 'name' parameter is null.");
           

            bool flag = false;
            string nameValue;
            if (!UniqueName(value.name, "Application"))
            {
                flag = true;
                nameValue = NewName(value.name, "Application");
            } 
            else
                nameValue = value.name;

            string queryString = "UPDATE Application SET name=@name WHERE id=@idApp";

            try
            {

                using (SqlConnection connection = new SqlConnection(connStr))
                {

                    SqlCommand command = new SqlCommand(queryString, connection);
                    command.Parameters.AddWithValue("@idApp", id);
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
                            return flag ? Ok("App " + nameValue + " was edited - specified name in use") : Ok("App " + nameValue + " was edited");
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

        [Route("{application}")]
        [HttpDelete]
        public IHttpActionResult DeleteApplication(string application)
        {
            int id = GetAppId(application);

            // aplicação nao existe
            if (id == 0)
                return BadRequest("App doesn't exist");

            List<string> listOfContainers = new List<string>(); 
            listOfContainers = GetListContainersOfApp(id);

            foreach (string container in listOfContainers)
            {
                ContainerController containerController = new ContainerController();
                containerController.DeleteContainer(application, container);

            }


            string queryString = "DELETE FROM Application where id=@idApp";

            try
            {

                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    SqlCommand command = new SqlCommand(queryString, connection);
                    command.Parameters.AddWithValue("@idApp", id);

                    try
                    {
                        command.Connection.Open();
                        int rows = command.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            return Ok("Application " + application + " deleted");
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

        //===================================================PERGUNTAR À STORA===================================================

        [Route("{application}")]
        [HttpPost]
        public IHttpActionResult PostContainer(string application, [FromBody] Container value)
        {
            int idApp = GetAppId(application);

            // aplicação nao existe
            if (idApp == 0)
                return BadRequest("App doesn't exist");

            if (value == null)
                return BadRequest("The request body is empty.");

            if (value.name == null || value.name == "")
                return BadRequest("The 'name' parameter is null.");

            bool flag = false;
            string nameValue;
            if (!UniqueName(value.name , "Container"))
            {
                flag = true;
                nameValue = NewName(value.name, "Container");
            }
            else
                nameValue = value.name;

            string queryString = "INSERT INTO Container VALUES (@name, @creation_dt, @parent)";

            try
            {

                using (SqlConnection connection = new SqlConnection(connStr))
                {

                    SqlCommand command = new SqlCommand(queryString, connection);
                    command.Parameters.AddWithValue("@name", nameValue);
                    DateTime now = DateTime.UtcNow;
                    string isoDateTimeString = now.ToString("yyyy-MM-dd HH:mm:ss");
                    command.Parameters.AddWithValue("@creation_dt", isoDateTimeString);
                    command.Parameters.AddWithValue("@parent", idApp);

                    try
                    {
                        command.Connection.Open();
                        if (command.ExecuteNonQuery() == 0)
                        {
                            return NotFound();
                        }
                        else
                        {
                            return flag ? Ok("Container " + nameValue + " was created - specified name in use") : Ok("Container " + nameValue + " was created");
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

        private List<string> GetListContainersOfApp (int idApp)
        {
            List<string> listOfContainers = new List<string>();
            string queryString = "SELECT name FROM Container WHERE parent = @parentContainer";

            try
            {
                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    SqlCommand command = new SqlCommand(queryString, connection);
                    command.Parameters.AddWithValue("@parentContainer", idApp);
                    command.Connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string name = (string)reader["name"];
                            listOfContainers.Add(name);
                        }
                    }
                }
                
            }
            catch (Exception)
            {
                InternalServerError();
            }

            return listOfContainers;

        }
    }
}