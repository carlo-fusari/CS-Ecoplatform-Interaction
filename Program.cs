using System;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using System.Xml;
using System.Xml.Linq;

namespace EcoPlatformApiExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //1. search materials:
            /*var apiUrl = "https://data.eco-platform.org/resource/processes?search=true&" +
                "distributed=true&" +
                "virtual=true&" +
                "name=Gobain";
                //"owner=Saint-Gobain PAM";*/

            //2. choose materials between results

            //3. read from material's site reference
            var apiUrl = "https://data.environdec.com/resource/processes/e7037d56-10e8-4c02-4566-08db71fe4796?version=07.00.015&format=xml";

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
            }

            using (var httpClient = new HttpClient())
            {
                // Set up the request header with your access token
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                try
                {
                    // Make the GET request
                    HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                    if (response.StatusCode == System.Net.HttpStatusCode.SeeOther)
                    {
                        var newUri = response.Headers.Location;
                        if (newUri != null)
                        {
                            // Optionally, you might want to ensure the new URI is absolute before following it
                            response = await httpClient.GetAsync(newUri);
                            response.EnsureSuccessStatusCode();
                        }
                    }
                    else
                    {
                        response.EnsureSuccessStatusCode();
                    }
                    string responseBody = await response.Content.ReadAsStringAsync();

                    if (response.Content.Headers.ContentType.ToString() == "application/zip")
                    {
                        string zipFilePath = @"C:\Users\Carlo\Downloads\file.zip";
                        SaveZipFileFromHttpResponse(response, response.Content.Headers.ContentDisposition, zipFilePath);
                    }
                    if (response.Content.Headers.ContentType.ToString() == "application/pdf")
                    {
                        string zipFilePath = @"C:\Users\Carlo\Downloads\file.pdf";
                        SavePdfFileFromHttpResponse(response, response.Content.Headers.ContentDisposition, zipFilePath);
                    }
                    if (response.Content.Headers.ContentType.ToString() == "application/xml")
                    {
                        string xmlFilePath = @"C:\Users\Carlo\Downloads\file.xml";
                        SaveXmlFileFromHttpResponse(response, response.Content.Headers.ContentDisposition, xmlFilePath);

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

                        //Console.WriteLine();
                    }

                    Console.WriteLine(responseBody);
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", e.Message);
                }
            }
        }

        static void SaveZipFileFromHttpResponse(HttpResponseMessage response, ContentDispositionHeaderValue contentDisposition, string filePath)
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
        static void SavePdfFileFromHttpResponse(HttpResponseMessage response, ContentDispositionHeaderValue contentDisposition, string filePath)
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
        static void SaveXmlFileFromHttpResponse(HttpResponseMessage response, ContentDispositionHeaderValue contentDisposition, string filePath)
        {
            if (response == null || !response.IsSuccessStatusCode || response.Content == null)
            {
                throw new Exception("Invalid or unsuccessful HTTP response");
            }

            // Optionally check for attachment disposition
            if (contentDisposition != null && !contentDisposition.DispositionType.Equals("attachment", StringComparison.OrdinalIgnoreCase))
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
