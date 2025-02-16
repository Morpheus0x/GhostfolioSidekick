﻿using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.Bitvavo
{
	public class BitvavoParser : RecordBaseImporter<BitvavoRecord>
	{
		private readonly ICurrencyMapper currencyMapper;

		public BitvavoParser(ICurrencyMapper currencyMapper)
		{
			this.currencyMapper = currencyMapper;
		}

		protected override IEnumerable<PartialActivity> ParseRow(BitvavoRecord record, int rowNumber)
		{
			if (record.Status != "Completed" && record.Status != "Distributed")
			{
				yield break;
			}

			DateTime dateTime = DateTime.SpecifyKind(record.Date.ToDateTime(record.Time), DateTimeKind.Utc);

			var currency = Currency.EUR;
			var isFiat = currencyMapper.Map(record.Currency).IsFiat();

			if (record.Fee != null && currencyMapper.Map(record.FeeCurrency).IsFiat() && record.Fee != 0)
			{
				yield return PartialActivity.CreateFee(currencyMapper.Map(record.FeeCurrency), dateTime, record.Fee.GetValueOrDefault(0), record.Transaction);
			}

			yield return GetMainRecord(record, dateTime, currency, isFiat);
		}

		private static PartialActivity GetMainRecord(BitvavoRecord record, DateTime dateTime, Currency currency, bool isFiat)
		{
			switch (record.Type)
			{
				case "buy":
					return PartialActivity.CreateBuy(
						currency,
						dateTime,
						[PartialSymbolIdentifier.CreateCrypto(record.Currency!)],
						Math.Abs(record.Amount),
						record.Price ?? 0,
						record.Transaction);
				case "sell":
					return PartialActivity.CreateSell(
						currency,
						dateTime,
						[PartialSymbolIdentifier.CreateCrypto(record.Currency!)],
						Math.Abs(record.Amount),
						record.Price ?? 0,
						record.Transaction);
				case "staking":
					return PartialActivity.CreateStakingReward(
						dateTime,
						[PartialSymbolIdentifier.CreateCrypto(record.Currency!)],
						Math.Abs(record.Amount),
						record.Transaction);
				case "withdrawal":
					if (isFiat)
					{
						return PartialActivity.CreateCashWithdrawal(
						currency,
						dateTime,
						Math.Abs(record.Amount),
						record.Transaction);
					}
					else
					{
						return PartialActivity.CreateSend(
						dateTime,
						[PartialSymbolIdentifier.CreateCrypto(record.Currency!)],
						Math.Abs(record.Amount),
						record.Transaction);
					}
				case "deposit":
					if (isFiat)
					{
						return PartialActivity.CreateCashDeposit(
						currency,
						dateTime,
						Math.Abs(record.Amount),
						record.Transaction);
					}
					else
					{
						return PartialActivity.CreateReceive(
						dateTime,
						[PartialSymbolIdentifier.CreateCrypto(record.Currency!)],
						Math.Abs(record.Amount),
						record.Transaction);
					}
				case "rebate":
					return PartialActivity.CreateGift(
						currency,
						dateTime,
						Math.Abs(record.Amount),
						record.Transaction);
				case "affiliate":
					return PartialActivity.CreateGift(
						currency,
						dateTime,
						Math.Abs(record.Amount),
						record.Transaction);
				default:
					throw new NotSupportedException();
			}
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
	}
}
