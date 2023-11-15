using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Xml;

// Set up shared variables
var apikey = "put your WildFire API key here";
var uri = new Uri("https://wildfire.paloaltonetworks.com/");
var client = new HttpClient();
client.BaseAddress = uri;
var apiSubmitFile = "publicapi/submit/file";
var apiGetVerdict = "publicapi/get/verdict";
var apiResponse = new HttpResponseMessage();
var xmlResponse = new XmlDocument();
var jsonResponse = string.Empty;
var dictResponse = new Dictionary<string, dynamic>();
var fileSHA256 = string.Empty;

// ******************** 
// Submit a File
// ******************** 

// Set up the file submission variables
var filePath = @"put the path to your file here"; // e.g., @"C:\malwarefile.exe"
var fileName = Path.GetFileName(filePath);
var multipartFormDataContent = new MultipartFormDataContent();

// Add the API key to the form
multipartFormDataContent.Add(new StringContent(apikey), name: @"""apikey""");

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
apiResponse = await client.PostAsync(apiSubmitFile, multipartFormDataContent);

// Did it work?
if (apiResponse.IsSuccessStatusCode)
{
    // Get the results
    xmlResponse.LoadXml(await apiResponse.Content.ReadAsStringAsync());

    // Get rid of the XML Declaration so that the rest can be converted to JSON
    foreach (XmlNode node in xmlResponse)
        if (node.NodeType == XmlNodeType.XmlDeclaration)
        {
            xmlResponse.RemoveChild(node);
        }

    // Convert the XML response to JSON and then to a dictionary
    jsonResponse = Newtonsoft.Json.JsonConvert.SerializeXmlNode(xmlResponse);
    dictResponse = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(jsonResponse);

    // Display the results
    Console.WriteLine("********************");
    Console.WriteLine("A file was successfully uploaded to WildFire.");
    Console.WriteLine("Filename: " + dictResponse["wildfire"]["upload-file-info"]["filename"]);
    fileSHA256 = dictResponse["wildfire"]["upload-file-info"]["sha256"];
    Console.WriteLine("SHA256: " + fileSHA256);
    Console.WriteLine("MD5: " + dictResponse["wildfire"]["upload-file-info"]["md5"]);
    Console.WriteLine("Size: " + dictResponse["wildfire"]["upload-file-info"]["size"]);
    Console.WriteLine("File Type: " + dictResponse["wildfire"]["upload-file-info"]["filetype"]);
    Console.WriteLine("********************");
}

// ******************** 
// Get the Verdict
// ******************** 

// Set up the verdict query variables
var queryData = new Dictionary<string, string>
    {
        { "apikey", apikey},
        { "hash", fileSHA256}
    };
var encodedQueryData = new FormUrlEncodedContent(queryData);

// Send the API key and hash
apiResponse = await client.PostAsync(apiGetVerdict, encodedQueryData);

// Did it work?
if (apiResponse.IsSuccessStatusCode)
{
    // Get the results
    xmlResponse.LoadXml(await apiResponse.Content.ReadAsStringAsync());

    // Get rid of the XML Declaration so that the rest can be converted to JSON
    foreach (XmlNode node in xmlResponse)
        if (node.NodeType == XmlNodeType.XmlDeclaration)
        {
            xmlResponse.RemoveChild(node);
        }

    // Convert the XML response to JSON and then to a dictionary
    jsonResponse = Newtonsoft.Json.JsonConvert.SerializeXmlNode(xmlResponse);
    dictResponse = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(jsonResponse);

    // Display the results
    Console.WriteLine("********************");
    Console.WriteLine("A verdict was successfully retrieved from WildFire.");
    Console.WriteLine("Verdict: " + dictResponse["wildfire"]["get-verdict-info"]["verdict"]);
    Console.WriteLine("SHA256: " + dictResponse["wildfire"]["get-verdict-info"]["sha256"]);
    Console.WriteLine("MD5: " + dictResponse["wildfire"]["get-verdict-info"]["md5"]);
    Console.WriteLine("********************");
}
