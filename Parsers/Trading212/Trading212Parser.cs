﻿using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.Trading212
{
	public class Trading212Parser : RecordBaseImporter<Trading212Record>
	{
		private readonly ICurrencyMapper currencyMapper;

		public Trading212Parser(ICurrencyMapper currencyMapper)
		{
			this.currencyMapper = currencyMapper;
		}

		protected override IEnumerable<PartialActivity> ParseRow(Trading212Record record, int rowNumber)
		{
			if (string.IsNullOrWhiteSpace(record.Id))
			{
				record.Id = $"{record.Action}_{record.ISIN}_{record.Time.ToInvariantDateOnlyString()}_{record.Total!.Value.ToString(CultureInfo.InvariantCulture)}_{record.Currency}";
			}

			var lst = new List<PartialActivity>();
			string? currencySymbol = string.IsNullOrWhiteSpace(record.Currency) ? record.CurrencyTotal : record.Currency;
			var currency = currencyMapper.Map(currencySymbol!);

			switch (record.Action)
			{
				case "Deposit":
					lst.Add(PartialActivity.CreateCashDeposit(currency, record.Time, Math.Abs(record.Total.GetValueOrDefault()), record.Id));
					break;
				case "Withdrawal":
					lst.Add(PartialActivity.CreateCashWithdrawal(currency, record.Time, Math.Abs(record.Total.GetValueOrDefault()), record.Id));
					break;
				case "Interest on cash":
				case "Lending interest":
					lst.Add(PartialActivity.CreateInterest(currency, record.Time, record.Total.GetValueOrDefault(), record.Action, record.Id));
					break;
				case "Currency conversion":
					var parsed = ParserConvertion(record);
					lst.AddRange(PartialActivity.CreateCurrencyConvert(record.Time, parsed.Source, parsed.Target, record.Id));
					break;
				case "Limit buy":
				case "Market buy":
					lst.Add(PartialActivity.CreateBuy(currency, record.Time,
						[PartialSymbolIdentifier.CreateStockAndETF(record.ISIN!)], record.NumberOfShares!.Value, record.Price!.Value, record.Id));
					break;
				case "Limit sell":
				case "Market sell":
					lst.Add(PartialActivity.CreateSell(currency, record.Time,
						[PartialSymbolIdentifier.CreateStockAndETF(record.ISIN!)], record.NumberOfShares!.Value, record.Price!.Value, record.Id));
					break;
				case string d when d.Contains("Dividend"):
					lst.Add(PartialActivity.CreateDividend(currency, record.Time,
						[PartialSymbolIdentifier.CreateStockAndETF(record.ISIN!)], record.Price!.Value * record.NumberOfShares!.Value, record.Id));
					break;
				default:
					throw new NotSupportedException();
			}

			lst.AddRange(GetFees(record));

			return lst;
		}

		private (Money Source, Money Target) ParserConvertion(Trading212Record record)
		{
			// "0.01 GBP -> 0.01 EUR"
			var note = record.Notes;

			if (string.IsNullOrWhiteSpace(note))
			{
				throw new NotSupportedException("Conversion without Notes");
			}

			var splitted = note.Split(' ');

			Money source = new(currencyMapper.Map(splitted[1]), decimal.Parse(splitted[0], CultureInfo.InvariantCulture));
			Money target = new(currencyMapper.Map(splitted[4]), decimal.Parse(splitted[3], CultureInfo.InvariantCulture));

			return (source, target);
		}

		protected override CsvConfiguration GetConfig()
		{
			return new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				CacheFields = true,
				Delimiter = ",",
			};
		}

		private IEnumerable<PartialActivity> GetFees(Trading212Record record)
		{
			if (record.TaxUK != null)
			{
				yield return PartialActivity.CreateTax(currencyMapper.Map(record.TaxUKCurrency!), record.Time, record.TaxUK.Value, record.Id!);
			}

			if (record.TaxFrance != null)
			{
				yield return PartialActivity.CreateTax(currencyMapper.Map(record.TaxFranceCurrency!), record.Time, record.TaxFrance.Value, record.Id!);
			}

			if (record.FeeFinra != null)
			{
				yield return PartialActivity.CreateFee(currencyMapper.Map(record.FeeFinraCurrency!), record.Time, record.FeeFinra.Value, record.Id!);
			}

			if (record.ConversionFee != null)
			{
				yield return PartialActivity.CreateFee(currencyMapper.Map(record.ConversionFeeCurrency!), record.Time, record.ConversionFee.Value, record.Id!);
			}
		}
	}
}
