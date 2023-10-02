using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StarCitizenExchangeRateComparer;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private readonly Dictionary<string, double> _exchangeRate = new();

    public MainWindow()
    {
        InitializeComponent();
        RefreshStarCitizenExchangeRate();
        RefreshExchangeRate();
    }

    private async void RefreshSoloStarCitizenExchangeRate(string code, ContentControl target)
    {
        const string queryUrl = "https://robertsspaceindustries.com/graphql";
        const string setCurrencyUrl = "https://robertsspaceindustries.com/api/store/setCurrency";
        const string query = """query platform {store(name: "pledge") {context {pricing {currencyCode exchangeRate}}}}""";
        using var httpClient = new HttpClient();
        var usd = await httpClient.PostAsync(queryUrl, JsonContent.Create(new { query }));
        httpClient.DefaultRequestHeaders.Add("x-rsi-token", usd.Headers.GetValues("Set-Cookie").First().Split(";").First().Split("=")[1]);
        await httpClient.PostAsync(setCurrencyUrl, JsonContent.Create(new { currency = code }));
        var result = await httpClient.PostAsync(queryUrl, JsonContent.Create(new { query }));
        var jsonResult = JsonConvert.DeserializeObject(result.Content.ReadAsStringAsync().Result) as JObject;
        if (!double.TryParse(jsonResult?.SelectToken("data.store.context.pricing.exchangeRate")?.ToString(), out var price)) return;
        target.Content = $"1.00→{price / 10000:F2}";
        _exchangeRate[code] = price / 10000;
    }

    private void RefreshStarCitizenExchangeRate()
    {
        RefreshSoloStarCitizenExchangeRate("EUR", ScUsd2EurLabel);
        RefreshSoloStarCitizenExchangeRate("GBP", ScUsd2GbpLabel);
    }

    private async void RefreshSoloExchangeRate(string code, ContentControl target)
    {
        const string baseUrl = "https://gushitong.baidu.com/opendata?query={0}&resource_id=5343";
        const string priceToken = "Result[0].DisplayData.resultData.tplData.result.minute_data.cur.price";
        var result = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(await new HttpClient().GetByteArrayAsync(string.Format(baseUrl, code)))) as JObject;
        if (!double.TryParse(result?.SelectToken(priceToken)?.ToString(), out var price)) return;
        target.Content = $"1.00→{price:F4}";
        _exchangeRate[code] = price;
    }

    private void RefreshExchangeRate()
    {
        RefreshSoloExchangeRate("USDEUR", Usd2EurLabel);
        RefreshSoloExchangeRate("USDGBP", Usd2GbpLabel);
        RefreshSoloExchangeRate("USDCNY", Usd2CnyLabel);
        RefreshSoloExchangeRate("EURCNY", Eur2CnyLabel);
        RefreshSoloExchangeRate("GBPCNY", Gbp2CnyLabel);
    }

    private void CheckPriceValidPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (!double.TryParse((sender as TextBox)?.Text + e.Text, out _)) e.Handled = true;
    }

    private void ScPriceTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!double.TryParse((sender as TextBox)?.Text, out var price)) return;
        Usd2CnyLabel.Content = $"{price:F2}→{price * _exchangeRate["USDCNY"]:F4}";
        Eur2CnyLabel.Content = $"{price * _exchangeRate["EUR"]:F2}→{price * _exchangeRate["EUR"] * _exchangeRate["EURCNY"]:F4}";
        Gbp2CnyLabel.Content = $"{price * _exchangeRate["GBP"]:F2}→{price * _exchangeRate["GBP"] * _exchangeRate["GBPCNY"]:F4}";
    }
}