using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Bot_Application1.Models;
using Microsoft.ProjectOxford.Vision;

namespace Bot_Application1
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

                // return our reply to the user
                //Activity reply = activity.CreateReply($"You sent {activity.Text} which was {length} characters");
                Activity reply = activity.CreateReply();

                if (activity.Attachments?.Count > 0 && activity.Attachments.First().ContentType.StartsWith("image/"))
                {
                    if (activity.ChannelId == "facebook")
                    {
                        //handle image
                        ImageTemplate(reply, activity.Attachments.First().ContentUrl);
                    }
                    else
                    {
                        //辨視圖片
                        var url = activity.Attachments.First().ContentUrl;
                        await analyseImage(reply, url);
                    }
                }
                else
                {
                    var fbdata = JsonConvert.DeserializeObject<FBChannelModel>(activity.ChannelData.ToString());
                    if (fbdata.postback != null)
                    {
                        if (fbdata.postback.payload.StartsWith("Analyze>"))
                        {
                            var url = fbdata.postback.payload.Split('>')[1];
                            //辨視圖片
                            await analyseImage(reply, url);

                        }
                        else
                        {
                            reply.Text = $"echo fbdata.postback: {fbdata.postback.payload}";
                        }
                    }
                    else
                    {
                        reply.Text = $"echo: {activity.Text}";
                    }
                }


                await connector.Conversations.ReplyToActivityAsync(reply);
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private static async Task analyseImage(Activity reply, string url)
        {
            var client = new VisionServiceClient("2ab40d02f36e4d3d8e4120dc74022a7d");
            var result = await client.AnalyzeImageAsync(url, new VisualFeature[] { VisualFeature.Description });
            reply.Text = result.Description.Captions.First().Text;
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


        private void ImageTemplate(Activity reply, string url)
        {
            var element = new List<object>()
            {
                new
                {
                    title = "Cognitive services?",
                    subtitle = "Select from below",
                    image_url = url,
                    buttons = new List<object>()
                    {
                        new
                        {
                            type = "postback",
                            title = "辨識圖片",
                            payload= $"Analyze>{url}"
                        }
                    }
                }
            };

            reply.ChannelData = JObject.FromObject(new
            {
                attachment = new
                {
                    type = "template",
                    payload = new
                    {
                        template_type = "generic",
                        elements = element
                    }
                }
            });
        }
    }
}