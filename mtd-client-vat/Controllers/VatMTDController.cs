using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Controllers
{
    public class VatMTDController : Controller
    {
        const string api = "vat mtd (v1.0)";
        const string vatObligationsEndpoint = "/VatMTD/VatObligationsCall";
        const string vatViewReturnEndpoint = "/VatMTD/VatReturnCall";
        const string vatLiabilitiesEndpoint = "/VatMTD/VatLiabilitiesCall";
        const string vatReturnsEndpoint = "/VatMTD/VatReturnCall";
        const string vatViewEndpoint = "/VatMTD/VatViewReturnCall";
        const string vatIndexEndpoint = "./";
        const string validateHeadersEndpoint = "/VatMtd/TestFraudRequestHeaders";


        const string JSON_FORMAT = "application/json";

        private readonly ClientSettings _clientSettings;

        public VatMTDController(IOptions<ClientSettings> clientSettingsOptions)
        {
            _clientSettings = clientSettingsOptions.Value;
        }

        public IActionResult Index()
        {
            ViewData["service"] = api;
            ViewData["validateHeadersEndpoint"] = validateHeadersEndpoint;
            ViewData["vatObligationsEndpoint"] = vatObligationsEndpoint;
            ViewData["vatViewReturnEndpoint"] = vatViewReturnEndpoint;
            ViewData["vatLiabilitiesEndpoint"] = vatLiabilitiesEndpoint;
            ViewData["vatReturnsEndpoint"] = vatReturnsEndpoint;
            ViewData["vatViewReturnEndpoint"] = vatViewEndpoint;

            return View();
        }

        class HmrcContent
        {
            public int code { get; set; }
            public string message { get; set; }
        }

        public class VatReturn
        {
            public string periodKey { get; set; }
            public decimal vatDueSales { get; set; }
            public decimal vatDueAcquisitions { get; set; }
            public decimal totalVatDue { get; set; }
            public decimal vatReclaimedCurrPeriod { get; set; }
            public decimal netVatDue { get; set; }
            public decimal totalValueSalesExVAT { get; set; }
            public decimal totalValuePurchasesExVAT { get; set; }
            public decimal totalValueGoodsSuppliedExVAT { get; set; }
            public decimal totalAcquisitionsExVAT { get; set; }
            public bool finalised { get; set; }
        }
        

        public async Task<IActionResult> TestFraudRequestHeaders()
        {
            string accessToken = await HttpContext.GetTokenAsync("access_token");

            if (accessToken != null)
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(_clientSettings.Uri);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.hmrc.1.0+json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                  

                    HttpResponseMessage response = await client.GetAsync($"/test/fraud-prevention-headers/validate");

                    String resp = await response.Content.ReadAsStringAsync();
                    return Content(resp, JSON_FORMAT);
                }
            }
            else
            {
                return Challenge(new AuthenticationProperties() { RedirectUri = vatIndexEndpoint }, "HMRC");
            }
        }

        public IActionResult ChallangeCall()
        {
            return Challenge(new AuthenticationProperties() { RedirectUri = vatIndexEndpoint }, "HMRC");
        }

        public async Task<IActionResult> VatObligationsCall(string vn)
        {
            string accessToken = await HttpContext.GetTokenAsync("access_token");

            if (accessToken != null)
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(_clientSettings.Uri);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.hmrc.1.0+json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);


                    client.DefaultRequestHeaders.Add("Gov-Test-Scenario", "QUARTERLY_ONE_MET");
                    HttpResponseMessage response = await client.GetAsync($"/organisations/vat/{vn}/obligations?status=O"); //?status=O || F
                    
                    String resp = await response.Content.ReadAsStringAsync();
                    return Content(resp, JSON_FORMAT);
                }
            }
            else
            {
                return Challenge(new AuthenticationProperties() { RedirectUri = vatIndexEndpoint }, "HMRC");
            }
        }

        public async Task<IActionResult> VatViewReturnCall(string vn, string pk)
        {
            string accessToken = await HttpContext.GetTokenAsync("access_token");

            if (accessToken != null)
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(_clientSettings.Uri);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.hmrc.1.0+json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                   
                    HttpResponseMessage response = await client.GetAsync($"/organisations/vat/{vn}/returns/{pk}");

                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                        return Content(JsonSerializer.Serialize(new HmrcContent() { code = (int)response.StatusCode, message = response.ReasonPhrase }), JSON_FORMAT);
                    else
                        return Content(await response.Content.ReadAsStringAsync(), JSON_FORMAT);
                }
            }
            else
            {
                return Challenge(new AuthenticationProperties() { RedirectUri = "/VatMTD/VatViewReturnCall" }, "HMRC");
            }
        }

        public async Task<IActionResult> VatLiabilitiesCall(string vn, string pk)
        {
            string accessToken = await HttpContext.GetTokenAsync("access_token");

            if (accessToken != null)
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(_clientSettings.Uri);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.hmrc.1.0+json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);                    
                    

                    client.DefaultRequestHeaders.Add("Gov-Test-Scenario", "MULTIPLE_LIABILITIES");
                    HttpResponseMessage response = await client.GetAsync($"/organisations/vat/{vn}/liabilities?from=2017-04-05&to=2017-12-21");
                    
                    String resp = await response.Content.ReadAsStringAsync();
                    return Content(resp, JSON_FORMAT);
                }
            }
            else
            {
                return Challenge(new AuthenticationProperties() { RedirectUri = vatIndexEndpoint }, "HMRC");
            }
        }

        public async Task<IActionResult> VatReturnCall(string vn, string pk)
        {
            VatReturn vatReturn = new()
            {
                periodKey = pk,
                vatDueSales = 105.5M,
                vatDueAcquisitions = -100.45M,
                totalVatDue = 5.05M,
                vatReclaimedCurrPeriod = 105.15M,
                netVatDue = 100.1M,
                totalValueSalesExVAT = 300M,
                totalValuePurchasesExVAT = 300M,
                totalValueGoodsSuppliedExVAT = 3000M,
                totalAcquisitionsExVAT = 3000M,
                finalised = true
            };

            string jsonString = JsonSerializer.Serialize(vatReturn);

            string accessToken = await HttpContext.GetTokenAsync("access_token");

            if (accessToken != null)
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(_clientSettings.Uri);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.hmrc.1.0+json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    HttpRequestMessage requestMessage = new()
                    {
                        Content = new StringContent(jsonString),
                        RequestUri = new Uri($"{_clientSettings.Uri}/organisations/vat/{vn}/returns"),
                        Method = HttpMethod.Post
                    };

                    requestMessage.Content.Headers.ContentType = new MediaTypeWithQualityHeaderValue(JSON_FORMAT);
                    

                    HttpResponseMessage response = await client.SendAsync(requestMessage);

  
                        return Content(await response.Content.ReadAsStringAsync(), JSON_FORMAT);
                }
            }
            else
            {
                return Challenge(new AuthenticationProperties() { RedirectUri = "/VatMTD/VatReturnCall" }, "HMRC");
            }
        }

    }
}
