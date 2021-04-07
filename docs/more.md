# Compiling Proto Files

If you are using this library it comes with compiled proto files of Spotware Open API and we do our best to keep the files update, in case there was a new version of proto files available and we weren't updated the files in library you can clone the library and compile the new proto files, then replace the library proto files with your compiled ones, the message files are located at Protobuf project inside Messages directory.

For compiling the proto files there is a guide available on Spotware Open API documentation but that is out dated and if you compile the files by following their instruction you will endup with Protobuf 2.0 which is old version and not supported anymore by Google, the new Protobuf 3 compiler can compile the old version files, Open API uses 2.0 but you can use the new version compiler and benifit from all the new features of version 3.

If you use the old version compiled files then you can't use .NET Core, because the Google Protobuf 2 .NET library is only available for .NET framework.

We recommend you to use our compiling instruction instead of Spotware documentation instruction, this instruction is for Windows and you can follow the Google standard instruction on Protobuf documentation if you are using Linux.

* Download the proto files from Spotware provided link/repo
* Download the Google Protobuf latest version from [here](https://github.com/protocolbuffers/protobuf/releases)
* Extract the Google Protobuf, there will be a "bin" folder, copy the ".proto" files there
* Open "CMD", go to bin folder location, and type: 
```
protoc --csharp_out=. ./proto_file_name.proto
```
Instead of "proto_file_name.proto" you have to provide each of the proto files names, you have to execute this command for each proto file.

After executing the command there will be a ".cs" file for the proto file, you can use those files instead of library default message files.

Don't forget to update the library Google Protobuf Nuget package to the version that you used for compiling the proto files, otherwise you will see lots of errors and you will not be able to build the project.

# Calculating Symbol Tick/Pip Value

When it comes to using Open API for trading or developing trading applications the most difficult part is calculating the symbols tick value, without a symbol tick value you can't calculate account equity, used margin, or size your position based on your account balance/equity.

OpenAPI.NET makes symbol tick/Pip value calculation as simple as possible by providing a set of helper methods that you can use to calculate not just tick/Pip value also other properties of symbols like tick/Pip size.

We also have a <a href="https://github.com/afhacker/OpenAPI.Net/tree/master/src/Trading.UI.Demo">Demo WPF application</a> that has all the necessary code for calculating symbols tick/Pip value and other account statistics like equity, free margin, used margin, margin level, net profit, and gross profit.

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

For a complete example please check our <a href="https://github.com/afhacker/OpenAPI.Net/tree/master/src/Trading.UI.Demo">WPF demo application</a>.

