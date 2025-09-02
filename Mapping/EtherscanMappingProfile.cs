using System.Globalization;
using System.Numerics;
using AutoMapper;
using EthCrawlerApi.Domain;
using EthCrawlerApi.Providers.Etherscan.Dto;

namespace EthCrawlerApi.Mapping
{
    public class EtherscanMappingProfile : Profile
    {
        public EtherscanMappingProfile()
        {
            // === TxDto -> EthTransaction =========================================
            CreateMap<TxDto, EthTransaction>()
            .ForMember(d => d.BlockNumber, c => c.MapFrom(s => ParseLong(s.blockNumber)))
            .ForMember(d => d.TimeStampUtc, c => c.ConvertUsing(new UnixToUtcConverter(), s => s.timeStamp))
            .ForMember(d => d.Hash, c => c.MapFrom(s => s.hash))
            .ForMember(d => d.From, c => c.MapFrom(s => s.from))
            .ForMember(d => d.To, c => c.MapFrom(s => s.to ?? string.Empty))
            .ForMember(d => d.ValueEth, c => c.ConvertUsing(new WeiToEthConverter(), s => s.value))
            .ForMember(d => d.GasPriceGwei, c => c.ConvertUsing(new WeiToGweiConverter(), s => s.gasPrice))
            .ForMember(d => d.GasUsed, c => c.MapFrom(s => ParseDecimal(s.gasUsed)))
            .ForMember(d => d.isError, c => c.MapFrom(s => ParseIsError(s.isError)));

            // === InternalDto -> InternalTransaction ==============================
            CreateMap<InternalDto, InternalTransaction>()
            .ForMember(d => d.UniqueId, c => c.MapFrom(s => $"{s.hash}:{s.traceId}"))
            .ForMember(d => d.BlockNumber, c => c.MapFrom(s => ParseLong(s.blockNumber)))
            .ForMember(d => d.TimeStampUtc, c => c.ConvertUsing(new UnixToUtcConverter(), s => s.timeStamp))
            .ForMember(d => d.Hash, c => c.MapFrom(s => s.hash))
            .ForMember(d => d.From, c => c.MapFrom(s => s.from))
            .ForMember(d => d.To, c => c.MapFrom(s => s.to ?? string.Empty))
            .ForMember(d => d.ValueEth, c => c.ConvertUsing(new WeiToEthConverter(), s => s.value));
            // === TokenDto -> TokenTransfer =======================================
            CreateMap<TokenDto, TokenTransfer>()
             .ForMember(d => d.UniqueId,
             c => c.MapFrom(s => $"{s.hash}:{(string.IsNullOrEmpty(s.logIndex) ? "0" : s.logIndex)}"))
            .ForMember(d => d.TxHash, c => c.MapFrom(s => s.hash))
            .ForMember(d => d.BlockNumber, c => c.MapFrom(s => ParseLong(s.blockNumber)))
            .ForMember(d => d.TimeStampUtc, c => c.ConvertUsing(new UnixToUtcConverter(), s => s.timeStamp))
            .ForMember(d => d.ContractAddress, c => c.MapFrom(s => s.contractAddress))
            .ForMember(d => d.TokenSymbol, c => c.MapFrom(s => s.tokenSymbol))
            .ForMember(d => d.TokenDecimals, c => c.MapFrom(s => ParseInt(s.tokenDecimal)))
            .ForMember(d => d.From, c => c.MapFrom(s => s.from))
            .ForMember(d => d.To, c => c.MapFrom(s => s.to ?? string.Empty))
            .ForMember(d => d.Amount, c => c.MapFrom<TokenAmountResolver>()); 

        }
        // ---------- Helpers (lokalni statični) ----------

        private static long ParseLong(String s)
            => long.Parse(s.Trim(), System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture);
        private static int ParseInt(string s)
            => int.Parse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture);
        private static decimal ParseDecimal(string s)
            => decimal.Parse(s.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture);
        private static bool ParseIsError(string? flag)
        {
            return (flag?.Trim() ?? "0") == "1";
        }

        // ---------- Value Converters ----------

        public sealed class UnixToUtcConverter : IValueConverter<string, DateTime>
        {
            public DateTime Convert(string sourceMember, ResolutionContext _)
            => DateTimeOffset
            .FromUnixTimeSeconds(long.Parse(sourceMember.Trim(), CultureInfo.InvariantCulture))
            .UtcDateTime;
        }

        public sealed class WeiToEthConverter : IValueConverter<string, decimal>
        {
            private static readonly decimal WeiPerEth = 1_000_000_000_000_000_000m; // 1e18

            public decimal Convert(string weiStr, ResolutionContext context)
            {
                var wei = ParseBigInt(weiStr);   // 1) string → BigInteger
                return (decimal)wei / WeiPerEth; // 2) wei → ETH
            }
        }

        public sealed class WeiToGweiConverter : IValueConverter<string, decimal>
        {
            private static readonly decimal WeiPerGwei = 1_000_000_000m; // 1e9
            public decimal Convert(string weiStr, ResolutionContext context)
            {
                var wei = ParseBigInt(weiStr);
                return (decimal)wei / WeiPerGwei;
            }
        }

        // ---------- Member Resolver (zavisi od više polja) ----------
        //за ово поље у дестинацији (TokenTransfer.Amount) немој га узимати директно
        //из једног поља изворног DTO-а, већ ја сам дефинишем како да га израчунаш
        //и можеш да користиш више поља из source-а“.
        public sealed class TokenAmountResolver : IValueResolver<TokenDto, TokenTransfer, decimal>
        {
            public decimal Resolve(TokenDto src, TokenTransfer dest, decimal destMember, ResolutionContext context)
            {
                //количина токена у raw формату (string у wei-like јединици) - src value
                // број децимала тог токена (ERC-20 спецификација) src.TokenDecimal 
                var raw = ParseBigInt(src.value);
                var decimals = int.Parse(src.tokenDecimal.Trim(), CultureInfo.InvariantCulture);
                
                return ScaleByDecimals(raw, decimals);
            }

            private static decimal ScaleByDecimals(BigInteger raw, int decimals)
            {
                //ERC-20 токени имају својство decimals →
                //колико цифара после децималне тачке има један прави број.
                //raw је количина токена у integer облику 
                //Ова метода скалира тај број тако да добијеш „човеку разумљиву“ вредност.
                if (decimals <= 0) return (decimal)raw;
                decimal divisor = 1m;
                for (int i = 0; i < decimals; i++) divisor *= 10m;
                return (decimal)raw / divisor;
                //npr Raw вредност је 1,000,000, broj deci 6, али стварни износ је 1.0 USDT.
            }
        }


        // ---------- Shared BigInt parser (hex ili decimal) ----------

        static BigInteger ParseBigInt(string s)
        {
            var t = s.Trim();
            if(t.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                t = t[2..];
                // prefiks "0" da izbegnemo znak pri parsiranju
                return BigInteger.Parse("0" + t, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }
            return BigInteger.Parse(t, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

    }
}
