﻿
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;

namespace TestAPI
{
    class Program
    {
        public static class FormUpload
        {
            private static readonly Encoding encoding = Encoding.UTF8;

            public static HttpWebResponse MultipartFormDataPost(string postUrl, string userAgent,
                Dictionary<string, object> postParameters)
            {
                string formDataBoundary = String.Format("----------{0:N}", Guid.NewGuid());
                string contentType = "multipart/form-data; boundary=" + formDataBoundary;

                byte[] formData = GetMultipartFormData(postParameters, formDataBoundary);

                return PostForm(postUrl, userAgent, contentType, formData);
            }
            ///


            private static HttpWebResponse PostForm(string postUrl, string userAgent, string contentType,
                byte[] formData)
            {



                HttpWebRequest request = WebRequest.Create(postUrl) as HttpWebRequest;

                if (request == null)
                {
                    throw new NullReferenceException();
                }

                // Set up the request properties.
                request.Method = "POST";
                request.ContentType = contentType;
                // request.UserAgent = userAgent;
                // request.CookieContainer = new CookieContainer();
                request.ContentLength = formData.Length;
                request.Headers.Add("x-token", "4950fc7e-55be-44ea-8832-58ebbd0cd436");

                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(formData, 0, formData.Length);
                    requestStream.Flush();
                }

                return request.GetResponse() as HttpWebResponse;
            }

            private static byte[] GetMultipartFormData(Dictionary<string, object> postParameters, string boundary)
            {
                Stream formDataStream = new System.IO.MemoryStream();
                bool needsCLRF = false;

                foreach (var param in postParameters)
                {
                    // Thanks to feedback from commenters, add a CRLF to allow multiple parameters to be added.
                    // Skip it on the first parameter, add it to subsequent parameters.
                    if (needsCLRF)
                        formDataStream.Write(encoding.GetBytes("\r\n"), 0, encoding.GetByteCount("\r\n"));

                    needsCLRF = true;

                    if (param.Value is FileParameter fileToUpload)
                    {
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
                    }
                    else
                    {
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

            public class FileParameter
            {
                public string Name { get; set; }
                public bool Mailru_needs_upload { get; set; }
                public string Description { get; set; }
                public string[] Mail_accounts { get; set; }
                public bool Yandex_needs_upload { get; set; }
                public string[] Yandex_accounts { get; set; }
                public byte[] File { get; set; }
                public string ContenType { get; set; }

                public FileParameter(byte[] file) : this(file, null)
                {
                }

                public FileParameter(byte[] file, string filename) : this(file, filename, false, null, null, false,
                    null)
                {
                }

                public FileParameter(byte[] file, string filename, bool mailruNeedsUpload, string description,
                    string[] mailAccounts, bool yandexNeedsUpload, string[] yandexAccounts)
                {

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



        static async Task Main(string[] args)
        {

            const string url = "https://export.cloud.getshopster.net/adapp/campaigns/create_with_roll_table/";
            const string fileName = "test106.csv";

            
            using (var content = new MultipartFormDataContent())
            {
                content.Headers.ContentType.MediaType = "multipart/form-data";
                Stream fileStream = System.IO.File.OpenRead(@"C:\Users\" + fileName);

                var values = new[]
                {
                    new KeyValuePair<string, string>("name", "test.csv"),
                    new KeyValuePair<string, string>("mailru_needs_upload", "false"),
                    new KeyValuePair<string, string>("description", "\"\""),
                    new KeyValuePair<string, string>("mailru_accounts", "[]"),
                    new KeyValuePair<string, string>("yandex_needs_upload","false"),
                    new KeyValuePair<string, string>("yandex_accounts", "[]"),
                };

                foreach (var keyValuePair in values)
                {
                    content.Add(new StringContent(keyValuePair.Value),
                        String.Format("\"{0}\"", keyValuePair.Key));
                }

                using (var client = new HttpClient())
                {
                    content.Add(new StreamContent(fileStream), fileName, fileName);
                    client.BaseAddress = new Uri(url);
                    client.DefaultRequestHeaders.Add("x-token", "4950fc7e-55be-44ea-8832-58ebbd0cd436");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("multipart/form-data"));

                    using (var massage = await client.PostAsync(url, content))
                    {
                        var result = await massage.Content.ReadAsStringAsync();
                        Console.WriteLine(result);
                    }
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
    }
}