using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace INTERIoTEWS.ContextManager.ContextManagerREST.Util
{
    public class InputHandler
    {
        public InputHandler(string host)
        {
            Host = host;
        }

        public async Task SayHelloSituationIdentification(string data)
        {
            try
            {
                SendMessageByEmail();
            }
            catch (Exception ex)
            { }

            try
            {
                string url = "http://localhost:53269/api/deviceobservations/123";

                if (!Host.ToLower().Contains("localhost"))
                    url = "http://inter-iot-ews-situationidentificationmanagerrest-v0.azurewebsites.net/api/deviceobservations/123";

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "GET";
                /*
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(data);
                }*/
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var responseText = streamReader.ReadToEnd();
                }

                

            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[InputHandler] Error on[SayHelloSituationIdentification]:" + ex.Message + Environment.NewLine + "InnerException: " + ((ex.InnerException != null && ex.InnerException.Message != null) ? ex.InnerException.Message : "NULL"));
            }
        }

        public async Task ReceiveMessagesFromDeviceAsync(string data)
        {
            SayHelloSituationIdentification(data);

                System.Diagnostics.Trace.TraceInformation("[InputHandler] ReceiveMessagesFromDeviceAsync data received");

                JObject messageJson = JObject.Parse(data);

                string domain = GetDomainFromMessage(messageJson);

                ExecuteSemanticTranslationsAndSendToSituationIdentificationManager(domain, data);

                ManageContextData(domain, data, messageJson);
            
        }

        private async Task ManageContextData(string domain, string data, JObject messageJson)
        {
            // Not implemented for INTER-IoT: privacy of historical data (avoid storing in the cloud)

        }

        private async Task ExecuteSemanticTranslationsAndSendToSituationIdentificationManager(string domain, string data)
        {
            if (domain != "unknown")
            {
                JObject messageJson = JObject.Parse(data);
                JObject messageFormattedINTER_IoT_GraphSrtucture = AddINTER_IoT_GraphSrtucture(messageJson);
                SendFormattedDataToSituationIdentifier(messageFormattedINTER_IoT_GraphSrtucture);
            }
        }

        public string Host = "localhost";

        private void SendFormattedDataToSituationIdentifier(JObject messageFormattedINTER_IoT_GraphSrtucture)
        {
            try
            {
                System.Diagnostics.Trace.TraceInformation("[ContextManager] SendFormattedDataToSituationIdentifier: Before sending");

                string url = "http://localhost:53269/api/deviceobservations/123";

                if (!Host.ToLower().Contains("localhost"))
                    url = "http://inter-iot-ews-situationidentificationmanagerrest-v0.azurewebsites.net/api/deviceobservations/123";

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "PUT";

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(messageFormattedINTER_IoT_GraphSrtucture.ToString());
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var responseText = streamReader.ReadToEnd();
                }
                System.Diagnostics.Trace.TraceInformation("[InputHandler] SendFormattedDataToSituationIdentifier: After sending");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[InputHandler] Error on[SendFormattedDataToSituationIdentifier]:" + ex.Message + Environment.NewLine + "InnerException: " + ((ex.InnerException != null) ? ex.InnerException.Message : "NULL"));
            }
        }


        private JObject AddINTER_IoT_GraphSrtucture(JObject messageJson)
        {
            string withGraphs = @"
{
	'@graph': [
		{
			'@graph': [
				{
					'@id': 'InterIoTMsg:meta66b05c61-d687-45a3-b5fb-6864bbec3b69',
					'@type': [
						'InterIoTMsg:Platform_register',
						'InterIoTMsg:meta'
					],
					'InterIoTMsg:conversationID': 'conv99528eba-eb2d-47e8-9ee6-9dd40d19f89a',
					'InterIoTMsg:dateTimeStamp': '2017-05-22T22:19:30.281+02:00',
					'InterIoTMsg:messageID': 'msg7e484a2c-f959-486e-8da0-31143f457234'
				}
			],
			'@id': 'InterIoTMsg:metadata'
		},
		{
			'@graph': [
                " + messageJson.ToString() + @"
            ],
			'@id': 'InterIoTMsg:payload'
		}
	],
	'@context': {
		'InterIoTMsg': 'http://inter-iot.eu/message/',
		'InterIoT': 'http://inter-iot.eu/',
		'rdf': 'http://www.w3.org/1999/02/22-rdf-syntax-ns#',
		'rdfs': 'http://www.w3.org/2000/01/rdf-schema#',
		'xsd': 'http://www.w3.org/2001/XMLSchema#'
	}
}
            ";


            JObject result = JObject.Parse(withGraphs);

            return result;
        }


        private string GetDomainFromMessage(JObject messageJson)
        {
            string domain = "unknown";

            if (messageJson != null && messageJson["@type"] != null)
            {
                switch (messageJson["@type"].ToString())
                {
                    case "saref:Device":
                        domain = "health";
                        break;
                    case "edxl_cap:AlertMessage":
                    case "edxl_de:EDXLDistribution":
                        domain = "emergency";
                        break;
                    case "LogiTrans:TransportEvent":
                        domain = "logistics";
                        break;
                    default:
                        domain = "jsonld";
                        break;
                }
            }

            return domain;
        }

        private async Task SendMessageByEmail()
        {
            SmtpClient client = new SmtpClient();
            client.Port = 587;
            client.Host = "smtp.gmail.com";
            client.EnableSsl = true;
            client.Timeout = 10000;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Credentials = new System.Net.NetworkCredential("inter.iot.ews@gmail.com", "1nter1otews");

            string emailBody = @"
            Testing email submission! e-mail testing that the ContextManager received the posted data: " + Host + @"

Be kind whenever possible. It is always possible.
Remember that sometimes not getting what you want is a wonderful stroke of luck.
My religion is very simple. My religion is kindness.
Sleep is the best meditation.
Happiness is not something ready made. It comes from your own actions.
If you want others to be happy, practice compassion. If you want to be happy, practice compassion.
Love and compassion are necessities, not luxuries. Without them, humanity cannot survive.
This is my simple religion. There is no need for temples; no need for complicated philosophy. Our own brain, our own heart is our temple; the philosophy is kindness.
Our prime purpose in this life is to help others. And if you can't help them, at least don't hurt them.
The purpose of our lives is to be happy.

                "; 

            MailMessage mm = new MailMessage("inter.iot.ews@gmail.com", GetEmailsToNotify(), "[INTER-IoT-EWS] Dalai Lama says", emailBody);
            mm.BodyEncoding = UTF8Encoding.UTF8;
            mm.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;

            client.Send(mm);
        }

        private string GetEmailsToNotify()
        {
            // Here we can specify email addresses for each notification activity with channel 'email': ideally this would be implemented with a relational DB
            return "jonimoreira@gmail.com,inter.iot.ews@gmail.com";
        }
    }
}
