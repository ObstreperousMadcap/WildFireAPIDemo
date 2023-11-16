using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Xml;

namespace WildFireAPIDemo
{
   class Program
    {
        public static void Main(string[] args)
        {
            // Initialize Demo Variables
            var apiKey = "your_api_key_here";
            var sha256 = "afe6b95ad95bc689c356f34ec8d9094c495e4af57c932ac413b65ef132063acc";
            string filePath = @"c:\test.doc";
            string link = @"https://www.paloaltonetworks.com";

            // Initialize Application Variables
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://wildfire.paloaltonetworks.com/");
            var apiEndpointGetVerdict = "publicapi/get/verdict";
            var apiResultKeyGetVerdict = "get-verdict-info";
            var apiEndpointSubmitFile = "publicapi/submit/file";
            var apiResultKeySubmitFile = "upload-file-info";
            var apiEndpointSubmitLink = "publicapi/submit/link";
            var apiResultKeySubmitLink = "submit-link-info";
            var dictParsedResponse = new Dictionary<string, object>(); // Unnecessary to initialize, but keeping it for future use.
            
            // ********************
            // Get a Verdict
            // ********************
            dictParsedResponse = WildFireAPIGetVerdict(httpClient, apiEndpointGetVerdict, apiResultKeyGetVerdict, apiKey, sha256).Result;
            if (dictParsedResponse.Keys.Count > 0)
                DisplayAPIResponse("WildFire API " + apiEndpointGetVerdict + " Results", dictParsedResponse);
            else
                Console.WriteLine(apiEndpointGetVerdict + " failed.");
            // ********************

            // ********************
            // Submit a File
            // ********************
            dictParsedResponse = WildFireAPISubmitFile(httpClient, apiEndpointSubmitFile, apiResultKeySubmitFile, apiKey, filePath).Result;
            if (dictParsedResponse.Keys.Count > 0)
                DisplayAPIResponse("WildFire API " + apiEndpointSubmitFile + " Results", dictParsedResponse);
            else
                Console.WriteLine(apiEndpointSubmitFile + " failed.");
            // ********************

            // ********************
            // Submit a Link
            // ********************
            dictParsedResponse = WildFireAPISubmitLink(httpClient, apiEndpointSubmitLink, apiResultKeySubmitLink, apiKey, link).Result;
            if (dictParsedResponse.Keys.Count > 0)
                DisplayAPIResponse("WildFire API " + apiEndpointSubmitLink + " Results", dictParsedResponse);
            else
                Console.WriteLine(apiEndpointSubmitLink + " failed.");
            // ********************
        }


        public static Dictionary<string, object> ParseAPIXMLResponse(XmlDocument xmlResponse, string apiResultKey)
        {
            // Remove the XML Declaration so that the remaining content can be converted to JSON
            foreach (XmlNode node in xmlResponse)
                if (node.NodeType == XmlNodeType.XmlDeclaration)
                    xmlResponse.RemoveChild(node);

            // Convert the XML response to JSON
            var jsonResponse = JsonConvert.SerializeXmlNode(xmlResponse);

            // Convert the JSON to a dictionary
            Dictionary<string, dynamic> dictResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonResponse);

            // Return the final dictionary containing the salient data by removing the first-level and second level keys
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(dictResponse["wildfire"][apiResultKey].ToString());
        }


        public static void DisplayAPIResponse(string outputTitle, Dictionary<string, object> dictParsedResponse)
        {
            Console.WriteLine(new string('*', 50));
            Console.WriteLine(outputTitle);
            Console.WriteLine(new string('*', 50));
            foreach (var item in dictParsedResponse)
                Console.WriteLine(item.Key + ": " + item.Value);
            Console.WriteLine(new string('*', 50));
        }


        public static async Task<Dictionary<string, object>> WildFireAPISubmitFile(HttpClient httpClient, string apiEndpoint, string apiResultKey, string apiKey, string filePath)
        {
            // Initialize Variables
            var fileName = Path.GetFileName(filePath);
            var multipartFormDataContent = new MultipartFormDataContent();

            // Add the API key to the form
            multipartFormDataContent.Add(new StringContent(apiKey), name: @"""apikey""");

            // Load and add the file to the form
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

                // Parse the results to make them usable; return the dictionary
                return ParseAPIXMLResponse(xmlResponse, apiResultKey);
            }
            else
                // Return an empty dictionary if the API call failed
                return new Dictionary<string, object>();
        }


        public static async Task<Dictionary<string, object>> WildFireAPIGetVerdict(HttpClient httpClient, string apiEndpoint, string apiResultKey, string apiKey, string sha256)
        {
            // Initialize Variables
            var multipartFormDataContent = new MultipartFormDataContent();

            // Add the API key to the form
            multipartFormDataContent.Add(new StringContent(apiKey), name: @"""apikey""");

            // Add the link to the form
            multipartFormDataContent.Add(new StringContent(sha256), name: @"""hash""") ;

            // Send the API key and hash
            var apiResponse = await httpClient.PostAsync(apiEndpoint, multipartFormDataContent);

            // Did it work?
            if (apiResponse.IsSuccessStatusCode)
            {
                // Get the results
                var xmlResponse = new XmlDocument();
                xmlResponse.LoadXml(await apiResponse.Content.ReadAsStringAsync());

                // Parse the results to make them usable; return the dictionary
                return ParseAPIXMLResponse(xmlResponse, apiResultKey);
            }
            else
                // Return an empty dictionary if the API call failed
                return new Dictionary<string, object>();
        }


        public static async Task<Dictionary<string, object>> WildFireAPISubmitLink(HttpClient httpClient, string apiEndpoint, string apiResultKey, string apiKey, string link)
        {
            // Initialize Variables
            var multipartFormDataContent = new MultipartFormDataContent();

            // Add the API key to the form
            multipartFormDataContent.Add(new StringContent(apiKey), name: @"""apikey""");

            // Add the link to the form
            multipartFormDataContent.Add(new StringContent(link), name: @"""link""");

            // Send the API key and hash
            var apiResponse = await httpClient.PostAsync(apiEndpoint, multipartFormDataContent);

            // Did it work?
            if (apiResponse.IsSuccessStatusCode)
            {
                // Get the results
                var xmlResponse = new XmlDocument();
                xmlResponse.LoadXml(await apiResponse.Content.ReadAsStringAsync());

                // Parse the results to make them usable; return the dictionary
                return ParseAPIXMLResponse(xmlResponse, apiResultKey);
            }
            else
                // Return an empty dictionary if the API call failed
                return new Dictionary<string, object>();

        }
    }
}