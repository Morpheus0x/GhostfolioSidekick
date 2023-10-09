# GhostfolioSidekick

A continuous running Docker container (A sidecar) to automatically import files from several brokers & crypto exchanges. The program checks every hour if any new transactions are found and inserts them in [ghostfolio](https://github.com/ghostfolio/ghostfolio). It can also correct & remove transactions in case they have changed (for example a different exchange rate) or the source file was deleted.

( more to come? Help is always welcome! )

## Setup

### Ghostfolio
* Create an account in ghostfolio using anonymous option save your **KEY**.
* Create the relevant accounts in ghostfolio.

### Folder structure

FileImporterPath should point to a folder with the following structure:
 * Account Name
   * File1.csv
 * Account Name 2
   * File2.csv
   * File3.csv

Assuming you configured an account with the name 'Trading 212' and an account with 'De Giro' in ghostfolio, the following structure should be used.
For example:
* Trading 212
  * Export2023.csv
* De Giro
  * Export2022.csv
  * Export2023.csv

### Mapping File
A single csv file that contains mapping to convert currencies and symbols to a symbol that can be found via ghostfolio

```
TYPE,SOURCE,TARGET
CURRENCY,GBX,GBp
IDENTIFIER,ATOM-USD,Cosmos USD
```

### Supported formats
| Platform | Source of the files | Buy | Sell | Dividend | Interest & Cash balance |
|--|--|--|--|--|
| Generic importer | See below | X | X | X | X |
| Trading 212 | Export of transaction history | X | X | X | X |
| De Giro | Export of transaction history | X | - | X | X |
| Scalable Capital | The CSV files of the Baader bank. Type WUM and RKK | X | X | X | X |
| Bunq (bank) | Export CSV (Semicolom delimited) | - | - | - | X |
| ~~Coinbase (Experimental)~~ | Export of transaction history | X | X | - |
| ~~Nexo (Experimental)~~ | Export of transaction history | X | - | - |

#### Generic import format
Beside the supported exchanges and brokers there is also a generic format. This format is only usefull for stocks at the moment, not for cryptocurrency:

| Field | Value(s) | 
| ----- | ----- |
| OrderType | BUY ,SELL or DIVIDEND | 
| Symbol | The symbol to search
| Date | The date, yyyy-MM-dd |
| Currency | The currency of the unitprice and fee |
| Quantity | The amount of units |
| UnitPrice | The paid price per unit |
| Fee | The total fee paid for the transaction |

##### Example

OrderType,Symbol,Date,Currency,Quantity,UnitPrice,Fee
BUY,US67066G1040,2023-08-07,USD,0.0267001000,453.33,0.02

## Run in Docker
The docker image is named: vibenl/ghostfoliosidekick

### Settings
| Envs |Description  |
|--|--|
|**GHOSTFOLIO_URL**  | The endpoint for your ghostfolio instance.   |
|**GHOSTFOLIO_ACCESTOKEN**  | The token as used to 'login' in the UI |
|**MAPPINGFILE**  | (optional) The path to the mapping file containing mapping for identifiers so it can be mapped automatically [Mapping File] |
|**FileImporterPath**  | The path to the files (see [Import Path]) |

## Contributing

* Feel free to submit any issue or PR's you think necessary
