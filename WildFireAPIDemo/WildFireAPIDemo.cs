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
            var apiKey = "put your api key here";
            var sha256 = "afe6b95ad95bc689c356f34ec8d9094c495e4af57c932ac413b65ef132063acc"; // Change to the desired hash
            string filePath = @"c:\test.doc"; // Change to the desired file
            string link = @"https://www.paloaltonetworks.com"; // Change to the desired target

            // Initialize Application Variables
            HttpClient httpClient = new HttpClient(); // Using a shared client for simplicity and speed
            httpClient.BaseAddress = new Uri("https://wildfire.paloaltonetworks.com/"); // May need to change for a different region
            var apiEndpointGetVerdict = "publicapi/get/verdict";
            var apiResultKeyGetVerdict = "get-verdict-info";
            var apiEndpointSubmitFile = "publicapi/submit/file";
            var apiResultKeySubmitFile = "upload-file-info";
            var apiEndpointSubmitLink = "publicapi/submit/link";
            var apiResultKeySubmitLink = "submit-link-info";
            
            // Get a Verdict
            DisplayAPIResponse("WildFire API " + apiEndpointGetVerdict + " Results", 
                WildFireAPIGetVerdict(httpClient, apiEndpointGetVerdict, apiResultKeyGetVerdict, apiKey, sha256).Result);
            
            // Submit a File
            DisplayAPIResponse("WildFire API " + apiEndpointSubmitFile + " Results", 
                WildFireAPISubmitFile(httpClient, apiEndpointSubmitFile, apiResultKeySubmitFile, apiKey, filePath).Result);

            // Submit a Link
            DisplayAPIResponse("WildFire API " + apiEndpointSubmitLink + " Results", 
                WildFireAPISubmitLink(httpClient, apiEndpointSubmitLink, apiResultKeySubmitLink, apiKey, link).Result);
        }


        public static async Task<Dictionary<string, object>> CallAPIEndpoint(
            HttpClient httpClient, string apiEndpoint, string apiResultKey, MultipartFormDataContent multipartFormDataContent)
        {
            //Send the API key and the form containing the data
            var apiResponse = await httpClient.PostAsync(apiEndpoint, multipartFormDataContent);

            // Did it work?
            if (apiResponse.IsSuccessStatusCode)
            {
                // Get the results
                var xmlResponse = new XmlDocument();
                xmlResponse.LoadXml(await apiResponse.Content.ReadAsStringAsync());

                // Remove the XML Declaration so that the remaining content can be converted to JSON
                foreach (XmlNode node in xmlResponse)
                    if (node.NodeType == XmlNodeType.XmlDeclaration)
                        xmlResponse.RemoveChild(node);

                // Convert the XML response to JSON
                var jsonResponse = JsonConvert.SerializeXmlNode(xmlResponse);

                // Convert the JSON to a Dictionary<string, dynamic>,
                // remove the first-level and second level keys,
                // and return a Dictionary<string, object> containing the salient data
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(
                    JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(jsonResponse)
                        ["wildfire"][apiResultKey].ToString());
            }
            else
                // Return an empty dictionary if the API call failed
                return new Dictionary<string, object>() { {apiEndpoint, "API call failed."} };
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


        public static async Task<Dictionary<string, object>> WildFireAPISubmitFile(
            HttpClient httpClient, string apiEndpoint, string apiResultKey, string apiKey, string filePath)
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

            // Call the API endpoint
            return CallAPIEndpoint(httpClient, apiEndpoint, apiResultKey, multipartFormDataContent).Result;
        }


        public static async Task<Dictionary<string, object>> WildFireAPIGetVerdict(
            HttpClient httpClient, string apiEndpoint, string apiResultKey, string apiKey, string sha256)
        {
            // Initialize Variables
            var multipartFormDataContent = new MultipartFormDataContent();

            // Add the API key to the form
            multipartFormDataContent.Add(new StringContent(apiKey), name: @"""apikey""");

            // Add the link to the form
            multipartFormDataContent.Add(new StringContent(sha256), name: @"""hash""") ;

            // Call the API endpoint
            return CallAPIEndpoint(httpClient, apiEndpoint, apiResultKey, multipartFormDataContent).Result;
        }


        public static async Task<Dictionary<string, object>> WildFireAPISubmitLink(
            HttpClient httpClient, string apiEndpoint, string apiResultKey, string apiKey, string link)
        {
            // Initialize Variables
            var multipartFormDataContent = new MultipartFormDataContent();

            // Add the API key to the form
            multipartFormDataContent.Add(new StringContent(apiKey), name: @"""apikey""");

            // Add the link to the form
            multipartFormDataContent.Add(new StringContent(link), name: @"""link""");

            // Call the API endpoint
            return CallAPIEndpoint(httpClient, apiEndpoint, apiResultKey, multipartFormDataContent).Result;
        }
    }
}