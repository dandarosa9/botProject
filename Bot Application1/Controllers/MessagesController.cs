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

                string intentName = "no entityName found";
                string intentScore = "no score found";
                string entityName = "no entityName found";
                string entityType = "no entityType found";
                string paramName = "no paramName found";
                string paramType = "no paramType found";
                string prompt = "How can I help you?";
                string needed = "I still need this information from you: ";

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
                            paramName = "CreateEvent";
                            break;
                        default:
                            intentName = "Couldn't score the intents correctly";
                            break;
                    }
                }
                int entityCount = eventLUIS.entities.Count();
                if (entityCount > 0)
                {
                    
                    // string[,] parsedEntities = new string[entityCount,2];
                    for (int count = 0; count < eventLUIS.entities.Count()-1; count++)
                    {
                        string[] parsedEntities = new string[entityCount - 1];
                        entityName = eventLUIS.entities[count].entity;
                        entityType = eventLUIS.entities[count].type;
                        Debug.WriteLine("parsedEntities from LUIS is below:");
                        Debug.WriteLine(entityName + ", " + entityType);

                        parsedEntities[count] = "Type: " + entityType + "   Name: " + entityName;
                        // parsedEntities[count, 1] = entityName;
                        // parsedEntities[count, 2] = entityType;
                        Debug.WriteLine(parsedEntities[count]);
                    }
                    
                    // Debug.WriteLine("parsedEntities from LUIS is below:");
                }
                Debug.WriteLine("intentName: " + intentName);

                // LUIS thinks the intent is None
                if (intentName.Equals("None"))
                {
                    Activity basicReply = activity.CreateReply($"{prompt}");
                    await connector.Conversations.ReplyToActivityAsync(basicReply);
                }
                else
                {
                    // LUIS returned a dialog field
                    if (!eventLUIS.dialog.Equals(null))
                    {
                        // If LUIS determines all input criteria has been parsed from message
                        if (eventLUIS.dialog.status.Equals("Finished"))
                        {
                            prompt = "Parsed information successfully!";
                            needed = "Here is what we have: ";
                        }
                        // If LUIS determines it needs more input criteria, it will ask a question
                        else if (eventLUIS.dialog.status.Equals("Question"))
                        {
                            prompt = eventLUIS.dialog.prompt;
                            paramName = eventLUIS.dialog.parameterName;
                            paramType = eventLUIS.dialog.parameterType;
                        }

                        // return our reply to the user
                        //Activity reply = activity.CreateReply($"Your input returned the intent: {intentName} and a score of: {intentScore} . \nThe Entity we retrieved is type: {entityType} and the name is {entityName}");
                        Activity reply2 = activity.CreateReply($"{prompt}");
                        await connector.Conversations.ReplyToActivityAsync(reply2);
                        if (!paramType.Equals("no paramType found"))
                        {
                            Activity reply3 = activity.CreateReply($"{needed}{paramType}");
                            await connector.Conversations.ReplyToActivityAsync(reply3);
                        }
                        else if (eventLUIS.dialog.status.Equals("Finished"))
                        {
                            Activity reply3 = activity.CreateReply($"{needed} Sample var 1, Sample var 2");
                            await connector.Conversations.ReplyToActivityAsync(reply3);
                        }
                        
                                  
                    }
                }

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