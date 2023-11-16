using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Xml;

namespace WildFireAPIDemo
{
   class Program
    {
        public static void Main(string[] args)
        {
            // ********************
            // Demo Variables
            // ********************
            var apiKey = "put_a_key_here";
            var fileSHA256 = "put_a_hash_here";
            string filePath = @"put_a_file_path_and_name_here";
            // ********************

            // ********************
            // Standard Variables
            // ********************
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://wildfire.paloaltonetworks.com/");
            var apiEndpointGetVerdict = "publicapi/get/verdict";
            var apiResultKeyGetVerdict = "get-verdict-info";
            var apiEndpointSubmitFile = "publicapi/submit/file";
            var apiResultKeySubmitFile = "upload-file-info";
            Dictionary<string, object> dictParsedResponse = new Dictionary<string, object>();
            // ********************

            // ********************
            // Get a Verdict
            // ********************
            dictParsedResponse = WildFireAPIGetVerdict(httpClient, apiEndpointGetVerdict, apiResultKeyGetVerdict, apiKey, fileSHA256).Result;
            if (dictParsedResponse.Keys.Count > 0)
            {
                DisplayAPIResponse("WildFire API " + apiEndpointGetVerdict + " Results", dictParsedResponse);
            }
            else
            {
                Console.WriteLine(apiEndpointGetVerdict + " failed.");
            }
            // ********************

            // ********************
            // Submit a File
            // ********************
            dictParsedResponse = WildFireAPISubmitFile(httpClient, apiEndpointSubmitFile, apiResultKeySubmitFile, apiKey, filePath).Result;
            if (dictParsedResponse.Keys.Count > 0)
            {
                DisplayAPIResponse("WildFire API " + apiEndpointSubmitFile + " Results", dictParsedResponse);
            }
            else
            {
                Console.WriteLine(apiEndpointSubmitFile + " failed.");
            }
            // ********************
        }


        public static Dictionary<string, object> ParseAPIXMLResponse(XmlDocument xmlResponse, string apiResultKey)
        {
            Dictionary<string, object> dictParsedResponse = new Dictionary<string, object>();

            // Remove the XML Declaration so that the remaining content can be converted to JSON
            foreach (XmlNode node in xmlResponse)
            {
                if (node.NodeType == XmlNodeType.XmlDeclaration)
                {
                    xmlResponse.RemoveChild(node);
                }
            }

            // Convert the XML response to JSON
            var jsonResponse = Newtonsoft.Json.JsonConvert.SerializeXmlNode(xmlResponse);

            // Convert the JSON to a dictionary
            Dictionary<string, dynamic> dictResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonResponse);

            // Create the final dictionary containing the salient data by removing the first-level and second level keys
            dictParsedResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(dictResponse["wildfire"][apiResultKey].ToString());

            return dictParsedResponse;
        }


        public static void DisplayAPIResponse(string outputTitle, Dictionary<string, object> dictParsedResponse)
        {
            Console.WriteLine(new string('*', 50));
            Console.WriteLine(outputTitle);
            Console.WriteLine(new string('*', 50));
            foreach (var item in dictParsedResponse)
            {
                Console.WriteLine(item.Key + ": " + item.Value);
            }
            Console.WriteLine(new string('*', 50));
            return;
        }


        public static async Task<Dictionary<string, object>> WildFireAPISubmitFile(HttpClient httpClient, string apiEndpoint, string apiResultKey, string apiKey, string filePath)
        {
            Dictionary<string, object> dictParsedResponse = new Dictionary<string, object>();

            var fileName = Path.GetFileName(filePath);
            var multipartFormDataContent = new MultipartFormDataContent();

            // Add the API key to the form
            multipartFormDataContent.Add(new StringContent(apiKey), name: @"""apikey""");

            // Load and add the file to form
            var fileStreamContent = new StreamContent(File.OpenRead(filePath));
            fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            fileStreamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = @"""file""",
                FileName = @$"""{fileName}"""
            };
            multipartFormDataContent.Add(fileStreamContent);

            //Send the API key and the file
            var apiResponse = await httpClient.PostAsync(apiEndpoint, multipartFormDataContent);

            // Did it work?
            if (apiResponse.IsSuccessStatusCode)
            {
                // Get the results
                var xmlResponse = new XmlDocument();
                xmlResponse.LoadXml(await apiResponse.Content.ReadAsStringAsync());

                // Parse the results to make them usable
                dictParsedResponse = ParseAPIXMLResponse(xmlResponse, apiResultKey);
            }

            return dictParsedResponse;
        }


        public static async Task<Dictionary<string, object>> WildFireAPIGetVerdict(HttpClient httpClient, string apiEndpoint, string apiResultKey, string apiKey, string fileSHA256)
        {
            Dictionary<string, object> dictParsedResponse = new Dictionary<string, object>();

            var queryData = new Dictionary<string, string>
            {
                { "apikey", apiKey},
                { "hash", fileSHA256}
            };
            var encodedQueryData = new FormUrlEncodedContent(queryData);

            // Send the API key and hash
            var apiResponse = await httpClient.PostAsync(apiEndpoint, encodedQueryData);

            // Did it work?
            if (apiResponse.IsSuccessStatusCode)
            {
                // Get the results
                var xmlResponse = new XmlDocument();
                xmlResponse.LoadXml(await apiResponse.Content.ReadAsStringAsync());

                // Parse the results to make them usable
                dictParsedResponse = ParseAPIXMLResponse(xmlResponse, apiResultKey);
            }

            return dictParsedResponse;
        }
    }
}