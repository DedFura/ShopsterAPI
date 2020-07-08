using System;
using System.Collections.Generic;
using System.IO;
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
        public static class FormUpload {
            private static readonly Encoding encoding = Encoding.UTF8;

            public static HttpWebResponse MultipartFormDataPost(string postUrl, string userAgent,
                Dictionary<string, object> postParameters) {
                string formDataBoundary = String.Format("----------{0:N}", Guid.NewGuid());
                string contentType = "multipart/form-data; boundary=" + formDataBoundary;

                byte[] formData = GetMultipartFormData(postParameters, formDataBoundary);

                return PostForm(postUrl, userAgent, contentType, formData);
            }

            private static HttpWebResponse PostForm(string postUrl, string userAgent, string contentType,
                byte[] formData) {
                HttpWebRequest request = WebRequest.Create(postUrl) as HttpWebRequest;

                if (request == null) {
                    throw new NullReferenceException();
                }

                // Set up the request properties.
                request.Method = "POST";
                request.ContentType = contentType;
                // request.UserAgent = userAgent;
                // request.CookieContainer = new CookieContainer();
                request.ContentLength = formData.Length;
                request.Headers.Add("x-token", "4950fc7e-55be-44ea-8832-58ebbd0cd436");

                using (Stream requestStream = request.GetRequestStream()) {
                    requestStream.Write(formData, 0, formData.Length);
                    requestStream.Flush();
                }

                return request.GetResponse() as HttpWebResponse;
            }

            private static byte[] GetMultipartFormData(Dictionary<string, object> postParameters, string boundary) {
                Stream formDataStream = new System.IO.MemoryStream();
                bool needsCLRF = false;

                foreach (var param in postParameters) {
                    // Thanks to feedback from commenters, add a CRLF to allow multiple parameters to be added.
                    // Skip it on the first parameter, add it to subsequent parameters.
                    if (needsCLRF)
                        formDataStream.Write(encoding.GetBytes("\r\n"), 0, encoding.GetByteCount("\r\n"));

                    needsCLRF = true;

                    if (param.Value is FileParameter fileToUpload) {
                        // Add just the first part of this param, since we will write the file data directly to the Stream
                        string header = string.Format(
                            "--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n",
                            boundary,
                            param.Key,
                            fileToUpload.Name ?? param.Key,
                            fileToUpload.ContenType ?? "application/vnd.ms-excel");

                        formDataStream.Write(encoding.GetBytes(header), 0, encoding.GetByteCount(header));

                        // Write the file data directly to the Stream, rather than serializing it to a string.
                        formDataStream.Write(fileToUpload.File, 0, fileToUpload.File.Length);
                    } else {
                        string postData = string.Format(
                            "--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}",
                            boundary,
                            param.Key,
                            param.Value);
                        formDataStream.Write(encoding.GetBytes(postData), 0, encoding.GetByteCount(postData));
                    }
                }

                // Add the end of the request.  Start with a newline
                string footer = "\r\n-" + boundary + "-\r\n";
                formDataStream.Write(encoding.GetBytes(footer), 0, encoding.GetByteCount(footer));

                // Dump the Stream into a byte[]


                formDataStream.Position = 0;
                byte[] formData = new byte[formDataStream.Length];
                formDataStream.Read(formData, 0, formData.Length);
                char[] text = new char[formData.Length * 2];
                Encoding.UTF8.GetChars(formData, 0, formData.Length, text, 0);
                var s = new string(text);
                formDataStream.Close();

                return formData;
            }

            public class FileParameter {
                public string Name { get; set; }
                public bool Mailru_needs_upload { get; set; }
                public string Description { get; set; }
                public string[] Mail_accounts { get; set; }
                public bool Yandex_needs_upload { get; set; }
                public string[] Yandex_accounts { get; set; }
                public byte[] File { get; set; }
                public string ContenType { get; set; }

                public FileParameter(byte[] file) : this(file, null) {
                }

                public FileParameter(byte[] file, string filename) : this(file, filename, false, null, null, false,
                    null) {
                }

                public FileParameter(byte[] file, string filename, bool mailruNeedsUpload, string description,
                    string[] mailAccounts, bool yandexNeedsUpload, string[] yandexAccounts) {
                    Name = filename;
                    Mailru_needs_upload = mailruNeedsUpload;
                    Description = description;
                    Mail_accounts = mailAccounts;
                    Yandex_needs_upload = yandexNeedsUpload;
                    Yandex_accounts = yandexAccounts;
                    File = file;
                }
            }
        }

        public class Item {
            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("progress_status")]
            public string Progress { get; set; }

            [JsonProperty("project_id")]
            public int ProjectId { get; set; }

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

        public static async Task<string> FileUpload(string urlUpload, FileStream fileStream, string fileName) {
            using (var content = new MultipartFormDataContent()) {
                content.Headers.ContentType.MediaType = "multipart/form-data";
                Dictionary<string, string> parameters = new Dictionary<string, string>();

                parameters.Add("name", "test259.csv");
                parameters.Add("mailru_needs_upload", "false");
                parameters.Add("description", "\"\"");
                parameters.Add("mailru_accounts", "[]");
                parameters.Add("yandex_needs_upload", "false");
                parameters.Add("yandex_accounts", "[]");

                foreach (var p in parameters) {
                    content.Add(new StringContent(p.Value) {
                        Headers =
                            {
                                ContentDisposition = new ContentDispositionHeaderValue("form-data")
                                {
                                    Name = p.Key
                                }
                            }
                    });
                }

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

                    using (var massage = await client.SendAsync(request)) {
                        var result = await massage.Content.ReadAsStringAsync();
                        Item item = JsonConvert.DeserializeObject<Item>(result);
                        return (result);
                    }
                }
            }
        }

        public static async Task<string> GetResultProces(string processUrl, int id) {
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
                            response = (item["progress_status"]);
                        }
                    }
                    return response;
                }
            }
        }

        public static async Task<string> GetAdAnalytics(string[] analysticsUrl, int id) {
            using (var client = new HttpClient()) {
                string result = null;
                foreach (var item in analysticsUrl) {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var postData = "?input_parameters={\"ad_campaign_id\":" + id + "}";
                    var request = new HttpRequestMessage {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(item + postData),
                    };

                    request.Headers.Add("x-token", "4950fc7e-55be-44ea-8832-58ebbd0cd436");

                    using (var massage = await client.SendAsync(request)) {
                        result += await massage.Content.ReadAsStringAsync();
                    }
                }
                return (result);
            }
        }

        static void Main(string[] args) {
            const string urlUpload = "https://export.cloud.getshopster.net/adapp/campaigns/create_with_roll_table/";
            const string processUrl = "https://export.cloud.getshopster.net/adapp/campaigns/";

            string[] analyticsUrl = new string[3];
            analyticsUrl[0] =
                "https://analytics-service.client.getshopster.net/report_api/query_report/dev/outdoor_ad_campaigns_ots_score";

            analyticsUrl[1] =
                "https://analytics-service.client.getshopster.net/report_api/query_report/dev/outdoor_ad_campaigns_ots_unique_score/";

            analyticsUrl[2] =
                "https://analytics-service.client.getshopster.net/report_api/query_report/dev/outdoor_ad_campaigns_detail_table";


            const string fileName = "test105.csv";

            using (var fileStream = new FileStream(@"C:\--\" + fileName, FileMode.Open)) {
                string s = FileUpload(urlUpload, fileStream, fileName).Result;
                Console.WriteLine(s);
            }



            //string s = GetAdAnalytics(analyticsUrl, 247).Result;
            //Console.WriteLine(s);
        }
    }


    //// Read file data
    //    FileStream fs = new FileStream(@"c:\Users\test106.csv", FileMode.Open, FileAccess.Read);
    //    byte[] data = new byte[fs.Length];
    //    fs.Read(data, 0, data.Length);
    //    fs.Close();


    //    // Generate post objects
    //    Dictionary<string, object> postParameters = new Dictionary<string, object>();
    //    postParameters.Add("name", "test.csv");
    //    postParameters.Add("mailru_needs_upload", "false");
    //    postParameters.Add("description", "\"\"");
    //    postParameters.Add("mailru_accounts", "[]");
    //    postParameters.Add("yandex_needs_upload", "false");
    //    postParameters.Add("yandex_accounts", "[]");
    //    postParameters.Add("file", new FormUpload.FileParameter(data, "test.csv"));

    //    // Create request and receive response
    //    string postURL = "https://export.cloud.getshopster.net/adapp/campaigns/create_with_roll_table/";
    //    string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.116 Safari/537.36";

    //    HttpWebResponse webResponse = FormUpload.MultipartFormDataPost(postURL, userAgent, postParameters);
    //    // Process response
    //    using (Stream stream = webResponse.GetResponseStream())
    //    {
    //        StreamReader reader = new StreamReader(stream, Encoding.UTF8);
    //        String responseString = reader.ReadToEnd();
    //    }
    //}
}

