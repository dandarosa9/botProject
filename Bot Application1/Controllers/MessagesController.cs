using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.Diagnostics;

namespace OutlookBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {

            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                // calculate something for us to return
                int length = (activity.Text ?? string.Empty).Length;

                string intentName = "no name found";
                string intentScore = "no score found";
                LUISobject eventLUIS = await GetEntityFromLUIS(activity.Text);
                Debug.WriteLine("event parsed from LUIS is below:");
                Debug.WriteLine(eventLUIS);
                if (eventLUIS.intents.Count() > 0)
                {
                    switch (eventLUIS.intents[0].intent)
                    {
                        case "CreateEvent":
                            intentName = eventLUIS.intents[0].intent;
                            intentScore = eventLUIS.intents[0].score;
                            break;
                        case "None":
                            intentName = eventLUIS.intents[0].intent;
                            intentScore = eventLUIS.intents[0].score;
                            break;
                        default:
                            intentName = "Couldn't score the intents correctly";
                            break;
                    }
                }

                // return our reply to the user
                Activity reply = activity.CreateReply($"You sent {activity.Text} which returned the intent: {intentName} and a score of: {intentScore}");
                await connector.Conversations.ReplyToActivityAsync(reply);

                // // return our reply to the user
                // Activity reply = activity.CreateReply($"You sent {activity.Text} which was {length} characters");
                // await connector.Conversations.ReplyToActivityAsync(reply);
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private async Task<LUISobject> GetEntityFromLUIS(string Query)
        {
            Query = Uri.EscapeDataString(Query);
            LUISobject Data = new LUISobject();
            using (HttpClient client = new HttpClient())
            {
                string RequestURI = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/e997d2c3-c4b6-4521-a058-71da36b4b298?subscription-key=e0f7d2fb61e74b4a95e266b711e18660&verbose=true&q=" + Query;
                HttpResponseMessage msg = await client.GetAsync(RequestURI);

                if (msg.IsSuccessStatusCode)
                {
                    var JsonDataResponse = await msg.Content.ReadAsStringAsync();
                    Data = JsonConvert.DeserializeObject<LUISobject>(JsonDataResponse);
                }
            }
            return Data;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}