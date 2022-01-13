# Calculating Symbol Tick/Pip Value

When it comes to using Open API for trading or developing trading applications the most difficult part is calculating the symbols tick value, without a symbol tick value you can't calculate account equity, used margin, or size your position based on your account balance/equity.

OpenAPI.NET makes symbol tick/Pip value calculation as simple as possible by providing a set of helper methods that you can use to calculate not just tick/Pip value also other properties of symbols like tick/Pip size.

We also have a [WPF Sample application](https://github.com/spotware/OpenAPI.Net/tree/master/src/WPF.Sample) that has all the necessary code for calculating symbols tick/Pip value and other account statistics like equity, free margin, used margin, margin level, net profit, and gross profit.

To calculate a symbol tick value:

* Get all account assets by sending a ProtoOAAssetListReq request, store the assets inside a collection.

* You have to get all available symbols of a trading account by sending a ProtoOASymbolsListReq, then get all those symbols full entities by sending a ProtoOASymbolByIdReq, add all symbol IDs to the request SymbolId collection, store all symbol entities inside a collection.

* Subscribe to ProtoOASpotEvent of your API client:

```C#
client.OfType<ProtoOASpotEvent>().Subscribe(OnSpotEvent);
```

OnSpotEvent will be the callback method that will be called whenever a symbol bid/ask changes, this method must have a ProtoOASpotEvent parameter.


* Subscribe to all symbols live quotes by sending a ProtoOASubscribeSpotsReq, add all symbols IDs to the request SymbolId collection.

* Now whenever a symbol bid/ask changes you will receive a ProtoOASpotEvent and your OnSpotEvent method will be called.

* Inside your OnSpotEvent you have to find the upcoming tick data symbol entity, the ProtoOASpotEvent has a SymbolId property that you can use.

* After you found the symbol entity that the ProtoOASpotEvent belongs to, you have to calculate its Bid/Ask:

```C#
using OpenAPI.Net.Helpers;
// spotEvent is ProtoOASpotEvent
if (spotEvent.HasBid) bid = symbol.GetPriceFromRelative((long)spotEvent.Bid);
if (spotEvent.HasAsk) ask = symbol.GetPriceFromRelative((long)spotEvent.Ask);
```

The GetPriceFromRelative method is part of SymbolExtensions class, its an extension method of ProtoOASymbol, to access it you have to add the "OpenAPI.Net.Helpers" using.

* Now we have the symbol latest bid/ask price, to calculate the tick value:


```C#
using OpenAPI.Net.Helpers;

double symbolTickValue = 0;

if (symbolQuoteAsset.AssetId == accountDepositAsset.AssetId)
{
	symbolTickValue = symbol.GetTickValue(symbolQuoteAsset, accountDepositAsset, null, default);
}
else
{
	var conversionSymbol = Symbols.FirstOrDefault(iSymbol => (iSymbol.BaseAssetId == symbolQuoteAsset.AssetId
		|| iSymbol.QuoteAssetId == symbolQuoteAsset.AssetId)
		&& (iSymbol.BaseAssetId == accountDepositAsset.AssetId
		|| iSymbol.QuoteAssetId == accountDepositAsset.AssetId));

	if (conversionSymbol is not null && conversionSymbol.Bid is not 0)
	{
		var conversionSymbolBaseAsset = accountAssets.First(iAsset => iAsset.AssetId == conversionSymbol.BaseAssetId);
		
		symbolTickValue = symbol.GetTickValue(symbolQuoteAsset, accountDepositAsset, conversionSymbolBaseAsset, conversionSymbol.Bid);
	}
}
```
When a symbol quote asset/currency is same as your trading account deposit currency then its tick value is equal to the symbol tick size, that's why in above code snippet we check if its equal then we don't look for conversion symbol and we pass null and default (0) for conversion symbol base asset/currency and its current price.

If its not then we have to convert the symbol price to account deposit currency, to do that we first iterate over all symbol entities, we need a symbol that its quote asset be same as symbol quote asset and its base or quote asset be same as account deposit asset/currency.

Once we found the conversion symbol we then get its base asset from account assets collection and then we pass all data to the symbol GetTickValue extension method, this method is also part of SymbolExtensions class which is in "OpenAPI.Net.Helpers" name space.

For a complete example please check our [WPF Sample application](https://github.com/spotware/OpenAPI.Net/tree/master/src/WPF.Sample).

Also check our [Blazor web assembly sample](https://github.com/spotware/OpenAPI.Net/tree/master/src/Blazor.WebSocket.Sample) which is deployed in [Github pages](https://spotware.github.io/openapi-blazor-wasm-sample/).