using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using AngleSharp.Html.Parser;
using AngleSharp.Dom;

namespace TenderParser
{
    class Program
    {
        static HttpClient _client = new HttpClient();
        static string tenderId, tenderName, status, customer, startMaxPrice, publicationDate, stopDate, destination;
        static List<LotComposition> positions = new List<LotComposition>();
        static List<TradeDocument> documents = new List<TradeDocument>();
        static void Main(string[] args)
        {
            string id = "";

            Console.WriteLine("Insert Trade ID:");
            id = Console.ReadLine();

            while (!int.TryParse(id, out int result))
            {
                Console.WriteLine("Wrong ID inserted!");
                Console.WriteLine("Insert Trade ID:");
                id = Console.ReadLine();
            }
            _client.Timeout = new TimeSpan(0, 0, 90);
            string _ContentType = "application/x-www-form-urlencoded";
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(_ContentType));
            var _UserAgent = "d-fens HttpClient";
            _client.DefaultRequestHeaders.Add("User-Agent", _UserAgent);

            var tradeInfoTask = GetTradesForParticipanrOrAnonymousAsync(id, _client).GetAwaiter();
            tradeInfoTask.OnCompleted(new Action(() => 
            {
                if (tenderId == null) { Console.WriteLine($"Trade Info for ID = {id} not found"); }
                else
                {
                    Console.WriteLine($"Tender ID: {tenderId}");
                    Console.WriteLine($"Tender Name: {tenderName}");
                    Console.WriteLine($"Status: {status}");
                    Console.WriteLine($"Customer: {customer}");
                    Console.WriteLine($"Initial Price: {startMaxPrice}");
                    Console.WriteLine($"Publication Date: {publicationDate}");
                    Console.WriteLine($"Stop Date: {stopDate}");
                    
                }
            }));

            var tradeDocsTask = GetTradeDocuments(id, _client).GetAwaiter();
            tradeDocsTask.OnCompleted(new Action(() =>
            {
                if (documents.Count == 0) { Console.WriteLine($"Trade documents for ID = {id} not found"); }
                else
                {
                    Console.WriteLine("Trade documents:");
                    foreach (var item in documents)
                    {
                        Console.WriteLine(item.ToString());
                    }
                }
            }));

            var tradeViewTask = ViewTrade(id, _client).GetAwaiter();
            tradeViewTask.OnCompleted(new Action(() =>
            {
                if (positions.Count == 0) { Console.WriteLine($"Positions for ID = {id} not found"); }
                else
                {
                    Console.WriteLine($"Destination: {destination}");
                    Console.WriteLine("Trade positions:");
                    foreach (var item in positions)
                    {
                        Console.WriteLine(item.ToString());
                    }
                }

                Console.WriteLine("Press enter to exit");
            }));

            Console.ReadLine();
        }

        static async System.Threading.Tasks.Task GetTradesForParticipanrOrAnonymousAsync(string id, HttpClient client)
        { 
            var json = new List<KeyValuePair<string, string>>();
            json.Add(new KeyValuePair<string, string>("page", "1"));
            json.Add(new KeyValuePair<string, string>("itemsPerPage", "10"));
            json.Add(new KeyValuePair<string, string>("Id", id));
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.market.mosreg.ru/api/Trade/GetTradesForParticipantOrAnonymous") { Content = new FormUrlEncodedContent(json) };
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            dynamic obj = JObject.Parse(responseBody);
            dynamic invData = obj.invdata.First;

            var value = invData.Id;
            tenderId = value;
            value = invData.TradeName;
            tenderName = value;
            value = invData.TradeState;
            status = value;
            value = invData.CustomerFullName;
            customer = value;
            var isInitialPriceDefined = invData.IsInitialPriceDefined == "true";
            value = isInitialPriceDefined ? invData.InitialPrice : "0";
            startMaxPrice = value;
            value = invData.PublicationDate;
            publicationDate = value;
            value = invData.FillingApplicationEndDate;
            stopDate = value;

        }

        static async System.Threading.Tasks.Task ViewTrade(string id, HttpClient client)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://market.mosreg.ru/Trade/ViewTrade/" + id);
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            var parser = new HtmlParser();
            var document = parser.ParseDocument(responseBody);
            foreach (IElement element in document.QuerySelectorAll(".informationAboutCustomer__informationPurchase-infoBlock"))
            {
                if (element.QuerySelector("span").TextContent.Contains("Место поставки"))
                {
                    destination = element.QuerySelector("p").TextContent;
                    break;
                }
            }

            var positionsDiv = document.QuerySelector(".informationAboutCustomer__resultBlock-outputResults");
            if (positionsDiv != null)
            {
                foreach (IElement element in positionsDiv.QuerySelectorAll(".outputResults__oneResult"))
                {
                    LotComposition lot = new LotComposition();
                    foreach (IElement subElement in element.QuerySelectorAll(".grayText"))
                    {
                        if (subElement.TextContent.Contains("Наименование товара, работ, услуг:"))
                        {
                            lot.Name = subElement.Parent.TextContent.Substring(subElement.Parent.TextContent.IndexOf(":")+2).TrimEnd();
                        }

                        if (subElement.TextContent.Contains("Единицы измерения:"))
                        {
                            lot.Unit = subElement.Parent.TextContent.Substring(subElement.Parent.TextContent.IndexOf(":") + 2).TrimEnd();
                        }

                       if (subElement.TextContent.Contains("Количество:"))
                       {
                            lot.Quantity = Convert.ToSingle(subElement.Parent.TextContent.Substring(subElement.Parent.TextContent.IndexOf(":") + 1));
                       }

                       if (subElement.TextContent.Contains("Стоимость единицы продукции ( в т.ч. НДС при наличии):"))
                       {
                            lot.Price = Convert.ToSingle(subElement.Parent.TextContent.Substring(subElement.Parent.TextContent.IndexOf(":") + 1));
                       }
                    }
                    positions.Add(lot);
                }
            }
        }

        static async System.Threading.Tasks.Task GetTradeDocuments(string id, HttpClient client)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.market.mosreg.ru/api/Trade/{id}/GetTradeDocuments");
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            documents = JsonConvert.DeserializeObject<List<TradeDocument>>(responseBody); 
        }
    }
}
