using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace AdminApp
{
    public partial class AdminApp : Form
    {
        MqttClient mClient = new MqttClient("127.0.0.1");
        string[] topic = { "updateOffices" };
        List<string> topicArray = new List<string>();
        string baseURI = @"http://localhost:50591";
        string app = "LibraryAdmin";
        RestClient client = null;
        public AdminApp()
        {
            InitializeComponent();
            client = new RestClient(baseURI);
            createLibrary();
            getAllOffices();
            this.FormClosing += ClientApp_FormClosing;
            mClient.Connect(Guid.NewGuid().ToString());
            if (mClient.IsConnected)
                mClient.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
        }

        private void ClientApp_FormClosing(object sender, FormClosingEventArgs e)
        {
            var request = new RestRequest("/api/somiod/{application}", Method.Delete);
            request.AddUrlSegment("application", app);
            client.Execute(request);
            if (mClient.IsConnected)
            {
                foreach (string topic in topicArray)
                {
                    string[] topicUnsubscribe = { topic };
                    mClient.Unsubscribe(topicUnsubscribe);
                }
                mClient.Unsubscribe(topic);
                mClient.Disconnect();
            }
                
        }

        private void Client_MqttMsgPublishReceived(object sender, uPLibrary.Networking.M2Mqtt.Messages.MqttMsgPublishEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate () {
                string message = Encoding.UTF8.GetString(e.Message);
                MessageBox.Show("An Office has been Reserved");
                getAllOffices();
            });
        }

        private void createLibrary()
        {
            var requestVerify = new RestRequest("/api/somiod/{application}", Method.Get);
            requestVerify.AddUrlSegment("application", app);
            requestVerify.RequestFormat = DataFormat.Xml;
            requestVerify.AddHeader("Accept", "application/xml");

            var responseVerify = client.Execute(requestVerify);

            if (!responseVerify.IsSuccessful) {
                var request = new RestRequest("/api/somiod", Method.Post);
                request.AddParameter("application/xml", createXmlDocument("LibraryAdmin", true).OuterXml, ParameterType.RequestBody);

                var response = client.Execute(request);

                if (response.IsSuccessful)
                {
                    Console.WriteLine("Library Created!");
                }
                else
                {
                    Console.WriteLine("Error - Library not Created!");
                }
            }
            
        }

        private void getAllOffices()
        {
            var mainRequest = new RestRequest("/api/somiod/{application}", Method.Get);
            mainRequest.AddUrlSegment("application", app);
            mainRequest.RequestFormat = DataFormat.Xml;
            mainRequest.AddHeader("somiod-discover", "container");
            mainRequest.AddHeader("Accept", "application/xml");

            var mainResponse = client.Execute(mainRequest);

            if (mainResponse.IsSuccessful)
            {
                XmlDocument mainXmlDoc = new XmlDocument();
                mainXmlDoc.LoadXml(mainResponse.Content);

                richTextBoxOpenOffices.Clear();
                richTextBoxOccupiedOffices.Clear();
                comboBoxDeleteOffices.Items.Clear();
                comboBoxVacantOffice.Items.Clear();
                comboBoxChangeOffice.Items.Clear();

                if (mainXmlDoc.DocumentElement.ChildNodes.Count == 0)
                {
                    richTextBoxOpenOffices.AppendText("No Open Offices");
                    richTextBoxOccupiedOffices.AppendText("No Occupied Offices");
                    return;
                }

                foreach (XmlNode containerNode in mainXmlDoc.DocumentElement.ChildNodes)
                {

                    comboBoxDeleteOffices.Items.Add(containerNode.InnerText);
                    comboBoxChangeOffice.Items.Add(containerNode.InnerText);

                    var containerRequest = new RestRequest("/api/somiod/{application}/{container}", Method.Get);
                    containerRequest.AddUrlSegment("application", app);
                    containerRequest.AddUrlSegment("container", containerNode.InnerText);
                    containerRequest.RequestFormat = DataFormat.Xml;
                    containerRequest.AddHeader("somiod-discover", "data");
                    containerRequest.AddHeader("Accept", "application/xml");

                    var containerResponse = client.Execute(containerRequest);

                    if (containerResponse.IsSuccessful)
                    {
                        XmlDocument containerXmlDoc = new XmlDocument();
                        containerXmlDoc.LoadXml(containerResponse.Content);

                        if (containerXmlDoc.DocumentElement.ChildNodes.Count == 0)
                        {
                            richTextBoxOpenOffices.AppendText(containerNode.InnerText + Environment.NewLine);
                        }
                        else
                        {
                            comboBoxVacantOffice.Items.Add(containerNode.InnerText);
                            richTextBoxOccupiedOffices.AppendText(containerNode.InnerText + Environment.NewLine);
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Error getting data for container {containerNode.InnerText}");
                    }
                }
            }
            else
            {
                MessageBox.Show("Error getting Offices");
            }
        }


        private void buttonCreate_Click(object sender, EventArgs e)
        {
            if (textBoxCreateNameOffice.Text == "")
            {
                MessageBox.Show("Office name not specified");
                return;
            }

            var request = new RestRequest("/api/somiod/{application}", Method.Post);
            request.AddUrlSegment("application", app);
            request.AddParameter("application/xml", createXmlDocument(textBoxCreateNameOffice.Text, false).OuterXml, ParameterType.RequestBody);

            var response = client.Execute(request);

            if (response.IsSuccessful)
            {

                byte[] msg = Encoding.UTF8.GetBytes(textBoxCreateNameOffice.Text + " Created");
                mClient.Publish(topic[0], msg);

                var requestSubCreation = new RestRequest("/api/somiod/{application}/{container}", Method.Post);
                requestSubCreation.AddUrlSegment("application", app);
                requestSubCreation.AddUrlSegment("container", textBoxCreateNameOffice.Text);
                String xmlCreation = createXmlDocumentSub(textBoxCreateNameOffice.Text + "-SubCreation", "mqtt://127.0.0.1", "1").OuterXml;
                requestSubCreation.AddParameter("application/xml", xmlCreation, ParameterType.RequestBody);

                var responseSubCreation = client.Execute(requestSubCreation);

                if (responseSubCreation.IsSuccessful)
                {
                    Console.WriteLine("Sub Creation Created - " + textBoxCreateNameOffice.Text);
                    string[] topic = { "api/somiod/" + app + "/" + textBoxCreateNameOffice.Text };


                    if (!mClient.IsConnected)
                    {
                        Console.WriteLine("Failed to connect to mqtt");
                    }
                    else
                    {

                        byte[] qosLevels = { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE };
                        mClient.Subscribe(topic, qosLevels);
                        topicArray.Add(topic.ToString());
                    }
                }
                else
                {
                    Console.WriteLine("Error creating sub creation" + textBoxCreateNameOffice.Text);
                }
                
                getAllOffices();
                MessageBox.Show("Created " + textBoxCreateNameOffice.Text);
                textBoxCreateNameOffice.Clear();

            }
            else
            {
                MessageBox.Show("Error creating " + textBoxCreateNameOffice.Text);
            }

        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            if (comboBoxDeleteOffices.SelectedItem == null || comboBoxDeleteOffices.SelectedItem.ToString() == "No Offices")
            {
                MessageBox.Show("Invalid Office");
                return;
            }


            var request = new RestRequest("/api/somiod/{application}/{container}", Method.Delete);
            request.AddUrlSegment("application", app);
            request.AddUrlSegment("container", comboBoxDeleteOffices.SelectedItem.ToString());
            request.AddHeader("Content-type", "application/xml");

            string container = comboBoxDeleteOffices.SelectedItem.ToString();

            var response = client.Execute(request);

            if (response.IsSuccessful)
            {
                byte[] msg = Encoding.UTF8.GetBytes(container + " deleted");
                mClient.Publish(topic[0], msg);
                string[] unsubscribe = { "api/somiod/" + app + "/" + container };
                topicArray.Remove(unsubscribe.ToString());
                mClient.Unsubscribe(unsubscribe);
                MessageBox.Show("Deleted " + container);
                getAllOffices();
                
            }
            else
            {
                MessageBox.Show("Error deleting " + comboBoxDeleteOffices.SelectedItem.ToString());
            }
        }

        private void buttonVacantOffice_Click(object sender, EventArgs e)
        {
            if (comboBoxVacantOffice.SelectedItem == null || comboBoxVacantOffice.SelectedItem.ToString() == "No Occupied Offices")
            {
                MessageBox.Show("Invalid Office");
                return;
            }

            var mainRequest = new RestRequest("/api/somiod/{application}/{container}", Method.Get);
            mainRequest.AddUrlSegment("application", app);
            mainRequest.AddUrlSegment("container", comboBoxVacantOffice.SelectedItem.ToString());
            mainRequest.RequestFormat = DataFormat.Xml;
            mainRequest.AddHeader("somiod-discover", "data");
            mainRequest.AddHeader("Accept", "application/xml");

            var mainResponse = client.Execute(mainRequest);

            if (mainResponse.IsSuccessful)
            {
                XmlDocument mainXmlDoc = new XmlDocument();
                mainXmlDoc.LoadXml(mainResponse.Content);

                foreach (XmlNode containerNode in mainXmlDoc.DocumentElement.ChildNodes)
                {
                    var request = new RestRequest("/api/somiod/{application}/{container}/data/{data}", Method.Delete);
                    request.AddUrlSegment("application", app);
                    request.AddUrlSegment("container", comboBoxVacantOffice.SelectedItem.ToString());
                    request.AddUrlSegment("data", containerNode.InnerText);

                    var response = client.Execute(request);

                    string container = comboBoxVacantOffice.SelectedItem.ToString();

                    if (response.IsSuccessful)
                    {
                        byte[] msg = Encoding.UTF8.GetBytes(container + " vacated");
                        mClient.Publish(topic[0], msg);

                        MessageBox.Show(comboBoxVacantOffice.SelectedItem.ToString() + " vacated");
                        getAllOffices();
                    }
                    else
                    {
                        MessageBox.Show("Error vacating " + comboBoxVacantOffice.SelectedItem.ToString());
                    }
                }
            }
            else
            {
                MessageBox.Show("Error vacating " + comboBoxVacantOffice.SelectedItem.ToString());
            }

        }

        private XmlDocument createXmlDocument(string name, bool flag)
        {
            XmlDocument xmlDoc = new XmlDocument();

            XmlElement rootElement = flag ? xmlDoc.CreateElement("Application") : xmlDoc.CreateElement("Container");
            rootElement.SetAttribute("xmlns:i", "http://www.w3.org/2001/XMLSchema-instance");
            rootElement.SetAttribute("xmlns", "http://schemas.datacontract.org/2004/07/MiddlewareDatabaseAPI.Models");
            xmlDoc.AppendChild(rootElement);
            XmlElement nameElement = xmlDoc.CreateElement("name");
            nameElement.InnerText = name;
            rootElement.AppendChild(nameElement);

            return xmlDoc;
        }

        private XmlDocument createXmlDocumentSub(string name, string endpoint, string event_mqtt)
        {
            XmlDocument xmlDoc = new XmlDocument();

            XmlElement rootElement = xmlDoc.CreateElement("DataOrSubscription");
            rootElement.SetAttribute("xmlns:i", "http://www.w3.org/2001/XMLSchema-instance");
            rootElement.SetAttribute("xmlns", "http://schemas.datacontract.org/2004/07/MiddlewareDatabaseAPI.Models");
            xmlDoc.AppendChild(rootElement);
            XmlElement endpointElement = xmlDoc.CreateElement("endpoint");
            endpointElement.InnerText = endpoint;
            rootElement.AppendChild(endpointElement);
            XmlElement eventElement = xmlDoc.CreateElement("event_mqtt");
            eventElement.InnerText = event_mqtt;
            rootElement.AppendChild(eventElement);
            XmlElement nameElement = xmlDoc.CreateElement("name");
            nameElement.InnerText = name;
            rootElement.AppendChild(nameElement);
            XmlElement resTypeElement = xmlDoc.CreateElement("res_type");
            resTypeElement.InnerText = "subscription";
            rootElement.AppendChild(resTypeElement);

            Console.WriteLine(xmlDoc.OuterXml);

            return xmlDoc;
        }

        private void buttonChangeOfficeName_Click(object sender, EventArgs e)
        {
            if (comboBoxChangeOffice.SelectedItem == null || comboBoxChangeOffice.SelectedItem.ToString() == "No Offices")
            {
                MessageBox.Show("Invalid Office");
                return;
            }

            if (textBoxChangeOffice.Text == "")
            {
                MessageBox.Show("No name specified");
                return;
            }

            var request = new RestRequest("/api/somiod/{application}/{container}", Method.Put);
            request.AddUrlSegment("application", app);
            request.AddUrlSegment("container", comboBoxChangeOffice.SelectedItem.ToString());
            request.AddParameter("application/xml", createXmlDocument(textBoxChangeOffice.Text,false).OuterXml, ParameterType.RequestBody);

            var response = client.Execute(request);

            string container = comboBoxChangeOffice.SelectedItem.ToString();

            if (response.IsSuccessful)
            {
                byte[] msg = Encoding.UTF8.GetBytes(container + " name changed");
                mClient.Publish(topic[0], msg);
                getAllOffices();
                MessageBox.Show("Office name changed");
                textBoxChangeOffice.Clear();
            }
            else
            {
                MessageBox.Show("Error changing " + comboBoxChangeOffice.SelectedItem.ToString() + " to " + textBoxChangeOffice.Text);
            }
        }
    }
}
