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
using System.Collections.Generic;
using System.Configuration;

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
               
                // START OF AUTHENTICATION CODE
                StateClient stateClient = activity.GetStateClient();
                BotState botState = new BotState(stateClient);
                BotData botData = null;
                if (botState != null)
                {
                    botData = await botState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                }
                string token;
                if (botData == null || (token = botData.GetProperty<string>("AccessToken")) == null)
                {
                    Activity replyToConversation = activity.CreateReply();
                    replyToConversation.Recipient = activity.From;
                    replyToConversation.Type = "message";
                    replyToConversation.Attachments = new List<Attachment>();
                    List<CardAction> cardButtons = new List<CardAction>();
                    CardAction plButton = new CardAction()
                    {
                        //Value = $"https://{ConfigurationManager.AppSettings["OutlookServiceProviderBaseUrl"]}/api/login?userid=default-user",
                        Value = "https://localhost:3979/api/login?userid=default-user",
                        Type = "signin",
                        Title = "Authentication Required"
                    };
                    cardButtons.Add(plButton);

                    SigninCard plCard = new SigninCard("Please login to Office 365 in order to use NetJets Capstone Outlook Bot", cardButtons);
                    Attachment plAttachment = plCard.ToAttachment();
                    replyToConversation.Attachments.Add(plAttachment);
                    var reply = await connector.Conversations.SendToConversationAsync(replyToConversation);
                    Debug.WriteLine("Reply from");
                    Debug.WriteLine(reply);

                    return Request.CreateResponse(HttpStatusCode.OK);
                }
                // END OF AUTHENTICATION CODE

                // LUIS chat logic starts here
                int length = (activity.Text ?? string.Empty).Length;
                
                string IntentName = "no EntityName found";
                string IntentScore = "no score found";
                string EntityName = "no EntityName found";
                string EntityType = "no EntityType found";
                string ParamName = "no ParamName found";
                string ParamType = "no ParamType found";
                string Prompt = "How can I help you?";
                string Needed = "I still need this information from you: ";
                
                LUISobject eventLUIS = await GetEntityFromLUIS(activity.Text);
                Debug.WriteLine("event parsed from LUIS is below:");
                Debug.WriteLine(eventLUIS);
                if (eventLUIS.intents.Count() > 0)
                {
                    switch (eventLUIS.intents[0].intent)
                    {
                        case "CreateEvent":
                            IntentName = eventLUIS.intents[0].intent;
                            IntentScore = eventLUIS.intents[0].score;
                            break;
                        case "None":
                            IntentName = eventLUIS.intents[0].intent;
                            IntentScore = eventLUIS.intents[0].score;
                            ParamName = "CreateEvent";
                            break;
                        default:
                            IntentName = "Couldn't score the intents correctly";
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
                        EntityName = eventLUIS.entities[count].entity;
                        EntityType = eventLUIS.entities[count].type;
                        Debug.WriteLine("parsedEntities from LUIS is below:");
                        Debug.WriteLine(EntityName + ", " + EntityType);
                
                        parsedEntities[count] = "Type: " + EntityType + "   Name: " + EntityName;
                        // parsedEntities[count, 1] = EntityName;
                        // parsedEntities[count, 2] = EntityType;
                        Debug.WriteLine(parsedEntities[count]);
                    }
                    
                    // Debug.WriteLine("parsedEntities from LUIS is below:");
                }
                Debug.WriteLine("IntentName: " + IntentName);
                
                // LUIS thinks the intent is None
                if (IntentName.Equals("None"))
                {
                    Activity basicReply = activity.CreateReply($"{Prompt}");
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
                            Prompt = "Parsed information successfully!";
                            Needed = "Here is what we have: ";
                        }
                        // If LUIS determines it needs more input criteria, it will ask a question
                        else if (eventLUIS.dialog.status.Equals("Question"))
                        {
                            Prompt = eventLUIS.dialog.prompt;
                            ParamName = eventLUIS.dialog.parameterName;
                            ParamType = eventLUIS.dialog.parameterType;
                        }
                
                        // return our reply to the user
                        //Activity reply = activity.CreateReply($"Your input returned the intent: {IntentName} and a score of: {IntentScore} . \nThe Entity we retrieved is type: {EntityType} and the name is {EntityName}");
                        Activity reply2 = activity.CreateReply($"{Prompt}");
                        await connector.Conversations.ReplyToActivityAsync(reply2);
                        if (!ParamType.Equals("no ParamType found"))
                        {
                            Activity reply3 = activity.CreateReply($"{Needed}{ParamType}");
                            await connector.Conversations.ReplyToActivityAsync(reply3);
                        }
                        else if (eventLUIS.dialog.status.Equals("Finished"))
                        {
                            Activity reply3 = activity.CreateReply($"{Needed} Sample var 1, Sample var 2");
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
