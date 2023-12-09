// Copyright (c) 2023 Michael Logan <ObstreperousMadcap@soclab.tech>
//
// Permission to use, copy, modify, and distribute this software for any
// purpose with or without fee is hereby granted, provided that the above
// copyright notice and this permission notice appear in all copies.
//
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
// WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
// MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
// ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
// WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
// ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
// OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.


// To-do
// Wrap error handling around file access - try/catch
// Confirm actual text of apiErrorCodes[] - contact WildFire engineering
// Add support for /get/report/
// Save results to logfile
// Add CLI option for different region URL
// Add CLI option for using submit logfile to obtain verdicts
// Add CLI option for using submit logfile to obtain reports


// Install System.CommandLine from command prompt:
//     dotnet add package System.CommandLine --prerelease
// For more information: https://learn.microsoft.com/en-us/dotnet/standard/commandline/get-started-tutorial

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

namespace WildFireUtility;

internal class Program
{
    internal static void Main(string[] args)
    {
        // Initialize Variables
        Dictionary<string, Dictionary<string, string>> apiResult;
        var apiResults =
            new Dictionary<string, Dictionary<string, string>>();

        // Parse the command line parameters
        var cliArguments = ParseCommandLine(args);

        // Validate the arguments and call the WildFire API
        switch (cliArguments["apiOption"])
        {
            case string apiOptionMatch when apiOptionMatch.Equals("submitFile"):
                if (File.Exists(cliArguments["value"]))
                {
                    // Submit the file
                    apiResult = CallWildFireAPI(cliArguments["apiKey"], cliArguments["apiOption"],
                        cliArguments["value"]).Result;
                    apiResults.Add(apiResult.ElementAt(0).Key, apiResult.ElementAt(0).Value);
                }
                else
                    apiResults.Add(cliArguments["value"],
                        new Dictionary<string, string> { { "Parameter Error", "File not found." } });
                break;

            case string apiOptionMatch when apiOptionMatch.Equals("submitFolder"):
                // Submit each of the files in the folder
                if (Directory.Exists(cliArguments["value"]))
                    foreach (var folderFile in Directory.EnumerateFiles(cliArguments["value"]))
                    {
                        apiResult = CallWildFireAPI(cliArguments["apiKey"], cliArguments["apiOption"], folderFile)
                            .Result;
                        apiResults.Add(apiResult.ElementAt(0).Key, apiResult.ElementAt(0).Value);
                    }
                else
                    apiResults.Add(cliArguments["value"],
                        new Dictionary<string, string> { { "Parameter Error", "Folder not found." } });
                break;

            case string apiOptionMatch when apiOptionMatch.Equals("submitLink"):
                if (CheckLinkFormat(cliArguments["value"]))
                {
                    // Submit the link
                    apiResult = CallWildFireAPI(cliArguments["apiKey"], cliArguments["apiOption"],
                        cliArguments["value"]).Result;
                    apiResults.Add(apiResult.ElementAt(0).Key, apiResult.ElementAt(0).Value);
                }
                else
                    apiResults.Add(cliArguments["value"],
                        new Dictionary<string, string> { { "Parameter Error", "Invalid link format." } });
                break;

            case string apiOptionMatch when apiOptionMatch.Equals("submitLinkfile"):
                if (File.Exists(cliArguments["value"]))
                    // Submit each of the links in the file
                    using (var reader = new StreamReader(cliArguments["value"]))
                    {
                        string link;
                        while ((link = reader.ReadLine()) != null)
                            if (CheckLinkFormat(link))
                            {
                                // Submit the link
                                apiResult = CallWildFireAPI(cliArguments["apiKey"], cliArguments["apiOption"], link)
                                    .Result;
                                apiResults.Add(apiResult.ElementAt(0).Key, apiResult.ElementAt(0).Value);
                            }
                            else
                                apiResults.Add(link,
                                    new Dictionary<string, string> { { "Parameter Error", "Invalid link format." } });
                    }
                else
                    apiResults.Add(cliArguments["value"],
                        new Dictionary<string, string> { { "Parameter Error", "File not found." } });
                break;

            case string apiOptionMatch when apiOptionMatch.Equals("verdictHash"):
                if (CheckHashFormat(cliArguments["value"]))
                {
                    // Get the verdict for the hash
                    apiResult = CallWildFireAPI(cliArguments["apiKey"], cliArguments["apiOption"],
                        cliArguments["value"]).Result;
                    apiResults.Add(apiResult.ElementAt(0).Key, apiResult.ElementAt(0).Value);
                }
                else
                    apiResults.Add(cliArguments["value"],
                        new Dictionary<string, string> { { "Parameter Error", "Invalid hash format." } });
                break;

            case string apiOptionMatch when apiOptionMatch.Equals("verdictHashfile"):
                if (File.Exists(cliArguments["value"]))
                    // Get the verdict for each of the hashes in the file
                    using (var reader = new StreamReader(cliArguments["value"]))
                    {
                        string hash;
                        while ((hash = reader.ReadLine()) != null)
                            if (CheckHashFormat(hash))
                            {
                                // Get the verdict for the hash
                                apiResult = CallWildFireAPI(cliArguments["apiKey"], cliArguments["apiOption"], hash)
                                    .Result;
                                apiResults.Add(apiResult.ElementAt(0).Key, apiResult.ElementAt(0).Value);
                            }
                            else
                                apiResults.Add(hash,
                                    new Dictionary<string, string> { { "Parameter Error", "Invalid hash format." } });
                    }
                else
                    apiResults.Add(cliArguments["value"],
                        new Dictionary<string, string> { { "Parameter Error", "File not found." } });
                break;

            case string apiOptionMatch when apiOptionMatch.Equals("verdictLink"):
                if (CheckLinkFormat(cliArguments["value"]))
                {
                    // Get the verdict for the link
                    apiResult = CallWildFireAPI(cliArguments["apiKey"], cliArguments["apiOption"],
                        cliArguments["value"]).Result;
                    apiResults.Add(apiResult.ElementAt(0).Key, apiResult.ElementAt(0).Value);
                }
                else
                    apiResults.Add(cliArguments["value"],
                        new Dictionary<string, string> { { "Parameter Error", "Invalid link format." } });
                break;

            case string apiOptionMatch when apiOptionMatch.Equals("verdictLinkfile"):
                if (File.Exists(cliArguments["value"]))
                    // Get the verdict for each of the links in the file
                    using (var reader = new StreamReader(cliArguments["value"]))
                    {
                        string link;
                        while ((link = reader.ReadLine()) != null)
                            if (CheckLinkFormat(link))
                            {
                                // Get the verdict for the link
                                apiResult = CallWildFireAPI(cliArguments["apiKey"], cliArguments["apiOption"], link)
                                    .Result;
                                apiResults.Add(apiResult.ElementAt(0).Key, apiResult.ElementAt(0).Value);
                            }
                            else
                            {
                                apiResults.Add(link,
                                    new Dictionary<string, string> { { "Parameter Error", "Invalid link format." } });
                            }
                    }
                else
                    apiResults.Add(cliArguments["value"],
                        new Dictionary<string, string> { { "Parameter Error", "File not found." } });
                break;
        }

        for (var outerElement = 0; outerElement < apiResults.Count; outerElement++)
        {
            Console.WriteLine("Parameter: " + apiResults.ElementAt(outerElement).Key);
            for (var innerElement = 0;
                 innerElement < apiResults.ElementAt(outerElement).Value.Count;
                 innerElement++)
                Console.WriteLine("\t" + apiResults.ElementAt(outerElement).Value.ElementAt(innerElement).Key +
                                  ": " + apiResults.ElementAt(outerElement).Value.ElementAt(innerElement).Value);
            // if (i < (apiResults.ElementAt(0).Value).Count - 1)
            //    Console.Write("; ");
            // else
            //    Console.WriteLine();
            Console.WriteLine();
        }
    }

    public static Dictionary<string, string> ParseCommandLine(string[] args)
    {
        var cliArguments = new Dictionary<string, string>
        {
            { "apiKey", "" },
            {
                "apiOption", ""
            }, // submitFile, submitFolder, submitLink, submitLinkfile, verdictHash, verdictHashfile, verdictLink, verdictLinkfile
            { "value", "" }
        };

        var apiKey = new Option<string?>(
                "--apikey",
                "WildFire API Key")
            { ArgumentHelpName = "APIKEY" };
        apiKey.IsRequired = true;
        apiKey.AddValidator(result =>
        {
            var apiKeySubmitted = result.GetValueForOption(apiKey).ToString();
            if (apiKeySubmitted.All(char.IsLetterOrDigit) && apiKeySubmitted.Length == 64)
                cliArguments["apiKey"] = apiKeySubmitted;
            else
                result.ErrorMessage = "<APIKEY> has an incorrect length and/or contains invalid characters.";
        });

        var submitFile = new Option<FileInfo?>(
                "--file",
                "Submit <FILE>")
            { ArgumentHelpName = "FILE" };
        submitFile.AddValidator(result =>
        {
            cliArguments["apiOption"] = "submitFile";
            cliArguments["value"] = result.GetValueForOption(submitFile).FullName;
            ;
        });

        var submitFiles = new Option<FileInfo?>(
                "--files",
                "Submit the file(s) in <FOLDER>")
            { ArgumentHelpName = "FOLDER" };
        submitFiles.AddValidator(result =>
        {
            cliArguments["apiOption"] = "submitFolder";
            cliArguments["value"] = result.GetValueForOption(submitFiles).FullName;
        });

        var submitLink = new Option<string?>(
                "--link",
                "Submit <LINK>")
            { ArgumentHelpName = "LINK" };
        submitLink.AddValidator(result =>
        {
            cliArguments["apiOption"] = "submitLink";
            cliArguments["value"] = result.GetValueForOption(submitLink).ToString();
        });

        var submitLinks = new Option<FileInfo?>(
                "--links",
                "Submit the link(s) in <FILE>")
            { ArgumentHelpName = "FILE" };
        submitLinks.AddValidator(result =>
        {
            cliArguments["apiOption"] = "submitLinkfile";
            cliArguments["value"] = result.GetValueForOption(submitLinks).FullName;
        });

        var verdictHash = new Option<string?>(
                "--hash",
                "Obtain the verdict for MD5/SHA-256 <HASH>")
            { ArgumentHelpName = "HASH" };
        verdictHash.AddValidator(result =>
        {
            cliArguments["apiOption"] = "verdictHash";
            cliArguments["value"] = result.GetValueForOption(verdictHash).ToString();
        });

        var verdictHashes = new Option<FileInfo?>(
                "--hashes",
                "Obtain the verdict for MD5/SHA-256 hash(es) in <FILE>")
            { ArgumentHelpName = "FILE" };
        verdictHashes.AddValidator(result =>
        {
            cliArguments["apiOption"] = "verdictHashfile";
            cliArguments["value"] = result.GetValueForOption(verdictHashes).FullName;
        });

        var verdictLink = new Option<string?>(
                "--link",
                "Obtain the verdict for <LINK>")
            { ArgumentHelpName = "LINK" };
        verdictLink.AddValidator(result =>
        {
            cliArguments["apiOption"] = "verdictLink";
            cliArguments["value"] = result.GetValueForOption(verdictLink).ToString();
        });

        var verdictLinks = new Option<FileInfo?>(
                "--links",
                "Obtain the verdict for link(s) in <FILE>")
            { ArgumentHelpName = "FILE" };
        verdictLinks.AddValidator(result =>
        {
            cliArguments["apiOption"] = "verdictLinkfile";
            cliArguments["value"] = result.GetValueForOption(verdictLinks).FullName;
        });

        var submit = new Command("submit", "Submit file(s)/link(s) to WildFire for analysis");
        submit.AddOption(apiKey);
        submit.AddOption(submitFile);
        submit.AddOption(submitFiles);
        submit.AddOption(submitLink);
        submit.AddOption(submitLinks);

        var verdict = new Command("verdict", "Obtain the verdict for file(s)/link(s)");
        verdict.AddOption(apiKey);
        verdict.AddOption(verdictHash);
        verdict.AddOption(verdictHashes);
        verdict.AddOption(verdictLink);
        verdict.AddOption(verdictLinks);

        var rootCommand = new RootCommand("WildFire API Utility");
        rootCommand.AddCommand(submit);
        rootCommand.AddCommand(verdict);
        rootCommand.InvokeAsync(args);

        return cliArguments;
    }

    public static async Task<Dictionary<string, Dictionary<string, string>>>
        CallWildFireAPI(string apiKey, string apiOption, string apiOptionValue,
            string apiHost = "https://wildfire.paloaltonetworks.com/")
    {
        // Initialize Variables
        var httpClient = new HttpClient(); // Using a shared client for simplicity and speed.
        httpClient.BaseAddress =
            new Uri(apiHost); // To-Do: make this a commandline option so different regions can be used

        // Possible error codes returned by the API; both numeric and text keys are used because the responses have not been confirmed with engineering
        var apiErrorCodes = new Dictionary<string, string>
        {
            { "OK", "200; Successful call." },
            { "Unauthorized", "401; Invalid API key. Ensure that the API key is correct." },
            { "401", "Unauthorized; Invalid API key. Ensure that the API key is correct." },
            { "Forbidden", "403; Permission denied." },
            { "403", "Forbidden. Permission denied." },
            { "NotFound", "404; The file or report was not found." },
            { "404", "The file or report was not found." },
            {
                "MethodNotAllowed",
                "405; Invalid request method. Ensure you are using POST for all calls except '/test/pe'."
            },
            { "405", "Invalid request method. Ensure you are using POST for all calls except '/test/pe'." },
            { "RequestEntityTooLarge", "413; File size over maximum limit." },
            { "413", "File size over maximum limit." },
            { "UnsupportedFileType", "418; File type is not supported." },
            { "418", "File type is not supported." },
            {
                "MaxRequestReached", "419; The maximum number of uploads per day has been exceeded. " +
                                     "If you continue to make API requests, you will receive this error until the daily limit resets at 23:59:00 UTC."
            },
            {
                "419", "The maximum number of uploads per day has been exceeded. " +
                       "If you continue to make API requests, you will receive this error until the daily limit resets at 23:59:00 UTC."
            },
            { "InsufficientArguments", "420; Ensure the request has the required request parameters." },
            { "420", "Insufficient arguments. Ensure the request has the required request parameters." },
            { "InvalidArgument", "421; Ensure the request is properly constructed." },
            { "MisdirectedRequest", "421; Invalid arguments. Ensure the request is properly constructed." },
            { "421", "InvalidArgument. Ensure the request is properly constructed." },
            {
                "UnprocessableEntity",
                "422; The provided file or URL cannot be processed. Possible reasons include: " +
                "(1) The specified URL cannot be downloaded, or (2) The specified file has formatting errors or invalid content."
            },
            {
                "422",
                "Unprocessable entity. The provided file or URL cannot be processed. Possible reasons include: " +
                "(1) The specified URL cannot be downloaded, or (2) The specified file has formatting errors or invalid content."
            },
            { "InternalError", "500; Internal error." },
            { "500", "Internal error." },
            { "513", "513; File upload failed." }
        };

        var apiResourceURLs = new Dictionary<string, string>
        {
            { "submitFile", "publicapi/submit/file" },
            { "submitFolder", "publicapi/submit/file" },
            { "submitLink", "publicapi/submit/link" },
            { "submitLinkfile", "publicapi/submit/link" },
            { "verdictHash", "publicapi/get/verdict" },
            { "verdictHashfile", "publicapi/get/verdict" },
            { "verdictLink", "publicapi/get/verdict" },
            { "verdictLinkfile", "publicapi/get/verdict" }
        };

        var apiResourceTags = new Dictionary<string, string>
        {
            { "submitFile", "upload-file-info" },
            { "submitFolder", "upload-file-info" },
            { "submitLink", "submit-link-info" },
            { "submitLinkfile", "submit-link-info" },
            { "verdictHash", "get-verdict-info" },
            { "verdictHashfile", "get-verdict-info" },
            { "verdictLink", "get-verdict-info" },
            { "verdictLinkfile", "get-verdict-info" }
        };

        // Used to display text verdicts instead of a number that needs to be looked up on the API webpage
        var apiVerdictCodes = new Dictionary<string, string>
        {
            { "0", "Benign" },
            { "1", "Malware" },
            { "2", "Grayware" },
            { "4", "Phishing" },
            { "5", "C2" },
            { "-100", "Pending; the file exists, but there is currently no verdict." },
            { "-101", "Error" },
            { "-102", "Unknown; Cannot find file record in the database." },
            { "-103", "Invalid hash value." }
        };

        // Contains all of the API parameter names and values
        var multipartFormDataContent = new MultipartFormDataContent();

        // Dictionary containing final content returned to caller
        var apiResultComplete =
            new Dictionary<string, Dictionary<string, string>>();

        // Use apiOptionValue as the key to ensure the dictionary returned to the caller is unique
        apiResultComplete.Add(apiOptionValue, new Dictionary<string, string>());

        // Add the API key to the form
        multipartFormDataContent.Add(new StringContent(apiKey), @"""apikey""");

        switch (apiOption)
        {
            case string apiOptionMatch
                when apiOptionMatch.Equals("submitFile") || apiOptionMatch.Equals("submitFolder"):
                // Load and add the file to the form
                var fileName = Path.GetFileName(apiOptionValue);
                var fileStreamContent = new StreamContent(File.OpenRead(apiOptionValue));
                fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                fileStreamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    Name = @"""file""",
                    FileName = @$"""{fileName}"""
                };
                multipartFormDataContent.Add(fileStreamContent);
                break;

            case string apiOptionMatch
                when apiOptionMatch.Equals("submitLink") || apiOptionMatch.Equals("submitLinkfile"):
                multipartFormDataContent.Add(new StringContent(apiOptionValue), @"""link""");
                break;

            case string apiOptionMatch
                when apiOptionMatch.Equals("verdictHash") || apiOptionMatch.Equals("verdictHashfile"):
                // Add the hash to the form
                multipartFormDataContent.Add(new StringContent(apiOptionValue), @"""hash""");
                break;

            case string apiOptionMatch
                when apiOptionMatch.Equals("verdictLink") || apiOptionMatch.Equals("verdictLinkfile"):
                multipartFormDataContent.Add(new StringContent(apiOptionValue), @"""url""");
                break;
        }

        // Make the API call with the key and form-encoded parameters
        var apiResultHTTP = await httpClient.PostAsync(apiResourceURLs[apiOption], multipartFormDataContent);

        // Did it fail?
        if (apiResultHTTP.IsSuccessStatusCode)
        {
            // Get the results
            var apiResultXML = new XmlDocument();
            apiResultXML.LoadXml(await apiResultHTTP.Content.ReadAsStringAsync());

            // Remove the XML declaration node because JsonConvert.DeserializeObject barfs on "?xml"
            foreach (XmlNode node in apiResultXML)
                if (node.NodeType == XmlNodeType.XmlDeclaration)
                    apiResultXML.RemoveChild(node);

            // Extract just the salient data and convert to JSON; "omitRootObject: true" removes the "wildfire" outer key
            var apiResultJSON = JsonConvert.SerializeXmlNode(apiResultXML, Formatting.None, true);

            // Convert the JSON to a dictionary to extract the core content
            var apiResultCoreContent =
                JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(apiResultJSON);

            // Add just the core results
            foreach (var dictElement in apiResultCoreContent[apiResourceTags[apiOption]])
                if (dictElement.Key == "verdict")
                    apiResultComplete[apiOptionValue].Add(dictElement.Key, apiVerdictCodes[dictElement.Value]);
                else
                    apiResultComplete[apiOptionValue].Add(dictElement.Key, dictElement.Value);
        }

        // Add the HTTP response status code
        apiResultComplete[apiOptionValue].Add("HTTP Response '" + apiResultHTTP.StatusCode + "'",
            apiErrorCodes[apiResultHTTP.StatusCode.ToString()]);

        return apiResultComplete;
    }

    public static bool CheckHashFormat(string hash)
    {
        hash = "12a6a16f9f0f7d22d000d1bbd75a96d882d4e3e481bc0eb4b62b1aeb65855bb3";
        var hashPattern = "[A-F0-9]";
        var regexProcessor = new Regex(hashPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        if ((hash.Length == 32 || hash.Length == 64) && regexProcessor.IsMatch(hash))
            return true;
        return false;
    }

    public static bool CheckLinkFormat(string URL)
    {
        var urlPattern = @"^(?:http(s)?:\/\/)?[\w.-]+(?:\.[\w\.-]+)+[\w\-\._~:/?#[\]@!\$&'\(\)\*\+,;=.]+$";
        var regexProcessor = new Regex(urlPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        return regexProcessor.IsMatch(URL);
    }
}