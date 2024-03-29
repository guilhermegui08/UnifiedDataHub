using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RestSharp;
using System.Xml;
using System.Xml.Linq;
using System.Security.AccessControl;

namespace TestApplication
{
    public partial class AppTester : Form
    {
        string baseURI = @"http://localhost:50591";
        RestClient client = null;
        public AppTester()
        {
            InitializeComponent();
            client = new RestClient(baseURI);
        }

        private void btnGetApps_Click(object sender, EventArgs e)
        {
            getAllApps();
        }

        private void getAllApps()
        {
            var request = new RestRequest("/api/somiod", Method.Get);
            request.RequestFormat = DataFormat.Xml;
            request.AddHeader("somiod-discover", "application");
            request.AddHeader("Accept", "application/xml");

            var response = client.Execute(request);

            if (response.IsSuccessful)
            {
                richTextBoxApps.Clear();

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(response.Content);

                if (xmlDoc.DocumentElement.ChildNodes.Count == 0)
                {
                    richTextBoxApps.AppendText("No Applications");
                    return;
                }

                foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
                {
                    richTextBoxApps.AppendText(node.InnerText + Environment.NewLine);
                }

            }
            else
            {
                MessageBox.Show("Error getting all applications name");
            }
        }

        private void btnGetApp_Click(object sender, EventArgs e)
        {
            if (textBoxNameApp.Text == "")
            {
                MessageBox.Show("No application specified");
                return;
            }

            var request = new RestRequest("/api/somiod/{application}", Method.Get);
            request.AddUrlSegment("application", textBoxNameApp.Text);
            request.RequestFormat = DataFormat.Xml;
            request.AddHeader("Accept", "application/xml");

            var response = client.Execute(request);
            int id;

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(response.Content);

            if (response.IsSuccessful)
            {
               
                foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "creation_dt":
                            textBoxCDT.Text = node.InnerText;
                            break;
                        case "id":
                            int.TryParse(node.InnerText, out id);
                            textBoxID.Text = id.ToString();
                            break;
                        case "name":
                            textBoxName.Text = node.InnerText;
                            break;
                    }
                }
            }
            else
            {
                if (xmlDoc.DocumentElement.InnerText == "" || xmlDoc.DocumentElement.InnerText == null)
                    MessageBox.Show("Error getting " + textBoxNameApp.Text + " information");
                else
                    MessageBox.Show(xmlDoc.DocumentElement.InnerText);
            }
        }

        private void btnEditApp_Click(object sender, EventArgs e)
        {
            if (textBoxNameApp.Text == "" || textBoxID.Text == "" || textBoxCDT.Text == "")
            {
                MessageBox.Show("No application loaded");
                return;
            }


            if (textBoxName.Text == "")
            {
                MessageBox.Show("No name specified");
                return;
            }

            var request = new RestRequest("/api/somiod/{application}", Method.Put);
            request.AddUrlSegment("application", textBoxNameApp.Text);
            request.AddParameter("application/xml", createXmlDocument(textBoxName.Text).OuterXml, ParameterType.RequestBody);
            request.AddHeader("Content-type", "application/xml");
            request.AddHeader("Accept", "application/xml");

            var response = client.Execute(request);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(response.Content);

            if (response.IsSuccessful)
            {
                textBoxNameApp.Clear();
                getAllApps();
                MessageBox.Show(xmlDoc.DocumentElement.InnerText);
            }
            else
            {
                if (xmlDoc.DocumentElement.InnerText == "" || xmlDoc.DocumentElement.InnerText == null)
                    MessageBox.Show("Error editing " + textBoxNameApp.Text + " application");
                else
                    MessageBox.Show(xmlDoc.DocumentElement.InnerText);
            }

        }

        private void btnCreateApp_Click(object sender, EventArgs e)
        {

            if (textBoxNameApp.Text == "" && textBoxID.Text == "" && textBoxCDT.Text == "")
            {
                MessageBox.Show("No application loaded");
                return;
            }

            var request = new RestRequest("/api/somiod", Method.Post);
            request.AddParameter("application/xml", createXmlDocument(textBoxName.Text).OuterXml, ParameterType.RequestBody);
            request.AddHeader("Accept", "application/xml");

            var response = client.Execute(request);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(response.Content);

            if (response.IsSuccessful)
            {
                getAllApps();
                MessageBox.Show(xmlDoc.DocumentElement.InnerText);
                textBoxID.Clear();
                textBoxCDT.Clear();
            }
            else
            {
                if (xmlDoc.DocumentElement.InnerText == "" || xmlDoc.DocumentElement.InnerText == null)
                    MessageBox.Show("Error creating " + textBoxNameApp.Text + " application");
                else
                    MessageBox.Show(xmlDoc.DocumentElement.InnerText);
            }
        }

        private void btnDeleteApp_Click(object sender, EventArgs e)
        { 

            if (textBoxName.Text == "")
            {
                MessageBox.Show("No name specified");
                return;
            }

            var request = new RestRequest("/api/somiod/{application}", Method.Delete);
            request.AddUrlSegment("application", textBoxName.Text);
            request.AddHeader("Accept", "application/xml");

            var response = client.Execute(request);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(response.Content);

            if (response.IsSuccessful)
            {
                ClearTextBoxes();
                textBoxNameApp.Clear();
                getAllApps();
                MessageBox.Show(xmlDoc.DocumentElement.InnerText);
            }
            else
            {
                if (xmlDoc.DocumentElement.InnerText == "" || xmlDoc.DocumentElement.InnerText == null)
                    MessageBox.Show("Error deleting " + textBoxNameApp.Text + " application");
                else
                    MessageBox.Show(xmlDoc.DocumentElement.InnerText);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearTextBoxes();
        }

        private void ClearTextBoxes()
        {
            textBoxID.Clear();
            textBoxName.Clear();
            textBoxCDT.Clear();
        }

        private XmlDocument createXmlDocument (string name)
        {
            XmlDocument xmlDoc = new XmlDocument();

            XmlElement rootElement = xmlDoc.CreateElement("Application");
            rootElement.SetAttribute("xmlns:i", "http://www.w3.org/2001/XMLSchema-instance");
            rootElement.SetAttribute("xmlns", "http://schemas.datacontract.org/2004/07/MiddlewareDatabaseAPI.Models");
            xmlDoc.AppendChild(rootElement);
            XmlElement nameElement = xmlDoc.CreateElement("name");
            nameElement.InnerText = name;
            rootElement.AppendChild(nameElement);

            return xmlDoc;
        }

    }
}
