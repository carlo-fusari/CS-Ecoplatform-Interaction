using System;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using System.Xml;
using System.Xml.Linq;
using System.Reflection;
using System.Globalization;
using System.Diagnostics.Metrics;

namespace EcoPlatformApiExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Retrieve Token
            string relativePath = @"..\..\..\token.txt";
            string fullPath = Path.GetFullPath(relativePath);
            string accessToken = "";
            if (System.IO.File.Exists(fullPath))
            {
                using (StreamReader reader = new StreamReader(fullPath))
                {
                    accessToken = reader.ReadToEnd();
                }
            }
            else
            {
                Console.WriteLine($"Token file does not exist: {fullPath}");
                return;
            }
            //------------------

            // Searching name
            string prompting = "Type a name to search:";
            Console.WriteLine(prompting);
            string searchingName = Console.ReadLine();
            //------------------

            // Search in eco-platform
            var apiUrl = "https://data.eco-platform.org/resource/processes?search=true&" +
                "distributed=true&" +
                "virtual=true&" +
                $"name={searchingName}";
            //"owner=Saint-Gobain PAM";*/

            var searchedLinks = new List<string>();
            var searchedNames = new List<string>();
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                try
                {
                    // GET request
                    HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
                    if (response.StatusCode == System.Net.HttpStatusCode.SeeOther)
                    {
                        var newUri = response.Headers.Location;
                        if (newUri != null)
                        {
                            response = await httpClient.GetAsync(newUri);
                            response.EnsureSuccessStatusCode();
                        }
                    }
                    else
                    {
                        response.EnsureSuccessStatusCode();
                    }
                    string responseBody = await response.Content.ReadAsStringAsync();

                    if (response.Content.Headers.ContentType.ToString() == "application/xml")
                    {
                        XDocument doc = XDocument.Parse(responseBody);
                        XNamespace nsSapi = "http://www.ilcd-network.org/ILCD/ServiceAPI";
                        XNamespace nsP = "http://www.ilcd-network.org/ILCD/ServiceAPI/Process";
                        XNamespace nsXlink = "http://www.w3.org/1999/xlink";
                        var dataSetList = doc.Element(nsSapi + "dataSetList");
                        if (dataSetList.Elements().Count() == 0)
                        {
                            Console.WriteLine("No results found.");
                            return;
                        }

                        var processes = doc.Descendants(nsP + "process");

                        foreach(var process in processes)
                        {
                            searchedLinks.Add(process.Attribute(nsXlink + "href").Value);
                            searchedNames.Add(process.Element(nsSapi + "name").Value);
                        }
                    }
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", e.Message);
                }
            }
            //------------------


            // Make the choice from the found list
            string chosenName;
            string chosenLink = "";
            bool matched = false;
            Console.WriteLine();
            Console.WriteLine("Choose from the following list:");
            for(int i = 0; i < searchedNames.Count; i++)
                Console.WriteLine($"{searchedNames[i]}"); //Console.WriteLine($"{i}: {searchedNames[i]}");
            do
            {
                searchingName = Console.ReadLine();
                for (int i = 0; i < searchedNames.Count(); i++)
                {
                    if (searchedNames[i] == searchingName)
                    {
                        chosenName = searchedNames[i];
                        chosenLink = searchedLinks[i];
                        matched = true;
                        break;
                    }
                }
                if (!matched)
                {
                    Console.WriteLine("The written name is not matching the previous list. Please try again:");
                    Console.WriteLine();
                }
            } while (!matched);
            //------------------


            //3. read from material's site reference
            Console.WriteLine(); Console.WriteLine(); Console.WriteLine();
            //apiUrl = "https://data.environdec.com/resource/processes/e7037d56-10e8-4c02-4566-08db71fe4796?version=07.00.015&format=xml";
            apiUrl = chosenLink  + "&format=xml";

            using (var httpClient = new HttpClient())
            {
                // Set up the request header with your access token
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                try
                {
                    // GET request
                    HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                    if (response.StatusCode == System.Net.HttpStatusCode.SeeOther)
                    {
                        var newUri = response.Headers.Location;
                        if (newUri != null)
                        {
                            response = await httpClient.GetAsync(newUri);
                            response.EnsureSuccessStatusCode();
                        }
                    }
                    else
                    {
                        response.EnsureSuccessStatusCode();
                    }
                    string responseBody = await response.Content.ReadAsStringAsync();

                    //saving file to downloads
                    if(false)
                    {
                        string userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
                        string downloadsPath = Path.Combine(userProfile, "Downloads");
                        if (response.Content.Headers.ContentType.ToString() == "application/zip")
                        {
                            SaveFileFromHttpResponse(response, response.Content.Headers.ContentDisposition, Path.Combine(downloadsPath, "file.zip"));
                        }
                        if (response.Content.Headers.ContentType.ToString() == "application/pdf")
                        {
                            SaveFileFromHttpResponse(response, response.Content.Headers.ContentDisposition, Path.Combine(downloadsPath, "file.pdf"));
                        }
                        if (response.Content.Headers.ContentType.ToString() == "application/xml")
                        {
                            SaveFileFromHttpResponse(response, response.Content.Headers.ContentDisposition, Path.Combine(downloadsPath, "file.xml"));
                        }
                    }

                    if (response.Content.Headers.ContentType.ToString() == "application/xml")
                    {
                        //XDocument xmlDoc = XDocument.Parse(responseBody);
                        //XNamespace sapi = "http://www.ilcd-network.org/ILCD/ServiceAPI";
                        //XNamespace f = "http://www.ilcd-network.org/ILCD/ServiceAPI/Flow";
                        //var flows = xmlDoc.Descendants(f + "flow")
                        //    .Where(flow =>
                        //        flow.Element(sapi + "uuid") != null &&
                        //        flow.Element(sapi + "name") != null);
                        //for(int i = 0; i < flows.ToList().Count(); i++)
                        //{
                        //    var flow = flows.ToList()[i];
                        //    string uuid = flow.Element(sapi + "uuid").Value;
                        //    string name = flow.Element(sapi + "name").Value;
                        //    Console.WriteLine($"{i}: {name}\t\tuuid:{uuid}");
                        //}

                        //XDocument doc = XDocument.Parse(responseBody);
                        //XNamespace ns = "http://lca.jrc.it/ILCD/LCIAMethod";
                        //XNamespace commonNs = "http://lca.jrc.it/ILCD/Common";
                        //var factors = doc.Descendants(ns + "factor")
                        //    .Select(factor => new
                        //    {
                        //        RefObjectId = factor.Element(ns + "referenceToFlowDataSet").Attribute("refObjectId").Value,
                        //        ShortDescription = factor.Element(ns + "referenceToFlowDataSet").Element(commonNs + "shortDescription").Value,
                        //        MeanValue = factor.Element(ns + "meanValue").Value
                        //    });
                        //for (int i = 0; i < factors.ToList().Count(); i++)
                        //{
                        //    var factor = factors.ToList()[i];
                        //    Console.WriteLine($"{i}: RefObjectId: {factor.RefObjectId}, ShortDescription: {factor.ShortDescription}, MeanValue: {factor.MeanValue}");
                        //}

                        XDocument doc = XDocument.Parse(responseBody);
                        XNamespace nsDefault = "http://lca.jrc.it/ILCD/Process";
                        XNamespace nsCommon = "http://lca.jrc.it/ILCD/Common";
                        XNamespace nsEPD = "http://www.iai.kit.edu/EPD/2013";
                        var lciaResults = doc.Descendants(nsDefault + "LCIAResults").Elements();
                        var gwps = new string[]
                        {
                            "Global Warming Potential - fossil fuels (GWP-fossil)",
                            "Global Warming Potential - biogenic (GWP-biogenic)",
                            "Global Warming Potential - land use and land use change (GWP-luluc)",
                            "Global Warming Potential - total (GWP-total)"
                        };
                        foreach (var g in gwps)
                        {
                            XElement? resultGWP = null;
                            foreach (var lcia in lciaResults)
                            {
                                var name = lcia.Element(nsDefault + "referenceToLCIAMethodDataSet")?
                                    .Element(nsCommon + "shortDescription")?.Value;
                                if (name != null && name == g)
                                {
                                    resultGWP = lcia;
                                }
                            }
                            var other = resultGWP?.Element(nsCommon + "other");
                            var amounts = other.Elements().Where(a => a.Name.LocalName == "amount");
                            var unit = other.Element(nsEPD + "referenceToUnitGroupDataSet")?.Element(nsCommon + "shortDescription")?.Value ?? "N/A";
                            if (resultGWP != null)
                            {
                                string s = resultGWP.Element(nsDefault + "referenceToLCIAMethodDataSet")?.Element(nsCommon + "shortDescription")?.Value;
                                Console.WriteLine($"Description: {s}");
                                foreach (var a in amounts)
                                {
                                    var module = a.Attribute(nsEPD + "module")?.Value;
                                    var value = a.Value;// double.Parse(a.Value, CultureInfo.InvariantCulture);
                                    Console.WriteLine($"Module: {module}, Value: {value}");
                                }
                                Console.WriteLine($"Unit: {unit}");
                            }
                            Console.WriteLine();
                        }

                        Console.WriteLine();
                    }

                    
                    //Console.WriteLine(responseBody);
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", e.Message);
                }
            }
        }

        static void SaveFileFromHttpResponse(HttpResponseMessage response, ContentDispositionHeaderValue contentDisposition, string filePath)
        {
            if (response == null || !response.IsSuccessStatusCode || response.Content == null)
            {
                throw new Exception("Invalid or unsuccessful HTTP response");
            }

            if (contentDisposition == null || !contentDisposition.DispositionType.Equals("attachment", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Content disposition is not for an attachment");
            }

            using (Stream contentStream = response.Content.ReadAsStream())
            {
                using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                {
                    contentStream.CopyTo(fileStream);
                }
            }
        }
    }
}
