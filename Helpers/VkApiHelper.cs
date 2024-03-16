using Newtonsoft.Json;
using SW.Core.DataLayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;

namespace SW.Frontend.Helpers
{
    public static class VkApiHelper
    {
        public static void GetVKGroup(string Id, ExternalWriter existWriter)
        {
            var vkGroupUrl = "https://api.vk.com/method/groups.getById?group_id={0}&fields=description";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format(vkGroupUrl, Id));
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                var jsonResponse = JsonConvert.DeserializeObject<dynamic>(reader.ReadToEnd());
                if (jsonResponse["error"] != null)
                    throw new Exception("Group not found");
                existWriter.VkId = jsonResponse.response[0].gid.ToString();
                existWriter.Title = jsonResponse.response[0].name.ToString();
                //existWriter.Description = jsonResponse.response[0].description.ToString();
            }
        }

        public static void GetVKUser(string Id, ExternalWriter existWriter)
        {
            var vkUserUrl = "https://api.vk.com/method/users.get?user_ids={0}&fields=screen_name,%20sex,%20bdate%20%28birthdate%29,%20city,%20country,%20timezone,%20photo,%20photo_medium,%20photo_big,%20has_mobile,%20contacts,%20education,%20online,%20counters,%20relation,%20last_seen,%20activity,%20can_write_private_message,%20can_see_all_posts,%20can_post,%20universities";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format(vkUserUrl, Id));
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                var jsonResponse = JsonConvert.DeserializeObject<dynamic>(reader.ReadToEnd());
                if (jsonResponse["error"] != null)
                    throw new Exception("User not found");
                existWriter.VkId = jsonResponse.response[0].uid.ToString();
                existWriter.Title = jsonResponse.response[0].first_name.ToString() + " " + jsonResponse.response[0].last_name.ToString();
            }
        }
    }
}