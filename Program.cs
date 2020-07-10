using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = Utf8Json.JsonSerializer;

namespace TestAPI {
    class Program {
        public class Item {
            [JsonProperty("id")] public int Id { get; set; }

            [JsonProperty("progress_status")] public string Progress { get; set; }

            [JsonProperty("project_id")] public int ProjectId { get; set; }

            public Item(string progress) {
                this.Id = Id;
                Progress = progress;
                this.ProjectId = ProjectId;
            }

            [JsonConstructor]
            public Item(int id, string progress, int projectId) {
                Id = id;
                Progress = progress;
                ProjectId = projectId;
            }

            public int GetId() {
                return Id;
            }
        }

        public static async Task<Item> FileUpload(string urlUpload, FileStream fileStream, string fileName) {
            using (var content = new MultipartFormDataContent()) {
                content.Headers.ContentType.MediaType = "multipart/form-data";

                Action<string, string> setHeader = (key, value) => {
                    content.Add(new StringContent(value) {
                        Headers =
                        {
                            ContentDisposition = new ContentDispositionHeaderValue("form-data")
                            {
                                Name = key
                            }
                        }
                    });
                };

                setHeader("name", "test263.csv");
                setHeader("mailru_needs_upload", "false");
                setHeader("description", "\"\"");
                setHeader("mailru_accounts", "[]");
                setHeader("yandex_needs_upload", "false");
                setHeader("yandex_accounts", "[]");

                content.Add(new StreamContent(fileStream), "file", fileName);

                using (var client = new HttpClient()) {
                    var request = new HttpRequestMessage {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(urlUpload),
                        Headers =
                        {
                            {"x-token", "4950fc7e-55be-44ea-8832-58ebbd0cd436"}
                        },
                        Content = content
                    };

                    using (var message = await client.SendAsync(request)) {
                        return JsonConvert.DeserializeObject<Item>(await message.Content.ReadAsStringAsync());
                    }
                }
            }
        }

        public static async Task<bool> GetResultProcess(string processUrl, int id) {
            using (var client = new HttpClient()) {
                var request = new HttpRequestMessage {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(processUrl),
                    Headers =
                    {
                        {"x-token", "4950fc7e-55be-44ea-8832-58ebbd0cd436"}
                    },
                };

                using (var massage = await client.SendAsync(request)) {
                    string response = null;
                    var result = await massage.Content.ReadAsStringAsync();
                    var list = JsonSerializer.Deserialize<dynamic>(result);
                    foreach (var item in list) {
                        if (item["id"] == id) {
                            response = item["progress_status"];
                        }
                    }

                    if (response == "success") {
                        return true;
                    }
                    return false;
                }
            }
        }

        public static async Task<List<dynamic>> GetAdAnalytics(string[] analysticsUrl, int id) {
            using (var client = new HttpClient()) {

                List<dynamic> results = new List<dynamic>();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var postData = "?input_parameters=" +
                               Encoding.UTF8.GetString(JsonSerializer.Serialize(new { ad_campaign_id = id }));

                for (int i = 0; i < analysticsUrl.Length; i++) {
                    var request = new HttpRequestMessage {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(analysticsUrl[i] + postData),
                        Headers =
                        {
                            {"x-token", "4950fc7e-55be-44ea-8832-58ebbd0cd436"}
                        },
                    };

                    using (var massage = await client.SendAsync(request)) {
                        results.Add(JsonSerializer.Deserialize<dynamic>(await massage.Content.ReadAsStringAsync()));
                    }
                }
                return results;
            }
        }

        public static dynamic GetDataFromAdAnalytics(List<dynamic> analyticsList) {
            var result = new {
                sOts = analyticsList[0]["data"]["data"][0]["values"][0],
                uOts = analyticsList[1]["data"]["data"][0]["values"][0],
                detail = (analyticsList[2]["data"]["rows"] is List<object> list) ? list.Select(row => ((Dictionary<string, object>)row)["values"]).ToList()
                    : null
            };
            return result;
        }

        static void Main(string[] args) {
            const string urlUpload = "https://export.cloud.getshopster.net/adapp/campaigns/create_with_roll_table/";
            const string processUrl = "https://export.cloud.getshopster.net/adapp/campaigns/";

            string[] analyticsUrl = new string[]
            {
                    "https://analytics-service.client.getshopster.net/report_api/query_report/dev/outdoor_ad_campaigns_ots_score",
                    "https://analytics-service.client.getshopster.net/report_api/query_report/dev/outdoor_ad_campaigns_ots_unique_score/",
                "https://analytics-service.client.getshopster.net/report_api/query_report/dev/outdoor_ad_campaigns_detail_table",
            };

            /*
            const string fileName = "test105.csv";

            using (var fileStream = new FileStream(@"C:\--\" + fileName, FileMode.Open)) {
                var s = FileUpload(urlUpload, fileStream, fileName).Result;
                Item item = s;
                Console.WriteLine(item.GetId());
            }
            */

            //var b = GetResultProcess(processUrl, 247).Result;
            //Console.WriteLine(b);

            //var s = GetAdAnalytics(analyticsUrl, 247).Result;
            //Console.WriteLine(s);

        }
    }
}

