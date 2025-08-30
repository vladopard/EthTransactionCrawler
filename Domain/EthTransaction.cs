namespace EthCrawlerApi.Domain
{
    public class EthTransaction
    {
        public required string Hash { get; set; }
        //Hash – Јединствени идентификатор трансакције (TX hash).
        //Свака трансакција на Ethereum мрежи има свој hash
        //по коме се може претражити преко explorer-а (нпр. etherscan).
        public required long BlockNumber { get; set; }
        //BlockNumber – Број блока у коме је трансакција потврђена.
        //Ово ти каже у којем тачно блоку је уписана.
        public required DateTime TimeStampUtc { get; set; }
        //Време (у UTC формату) када је блок рударен/валидован, па самим тим и трансакција.
        public required string From { get; set; }
        //Ethereum адреса која је послала трансакцију.
        //То је увек власник приватног кључа који је потписао TX.
        public required string To { get; set; }
        //Одредишна Ethereum адреса.
        //Ако је у питању трансфер ка некој адреси(корисник или смарт уговор),
        //овде иде та адреса.Ако је null или празно,
        //то значи да је трансакција деплојовала нови смарт уговор.
        public required decimal ValueEth { get; set; }
        //Колико је ETH послато у трансакцији.
        //Ако је 0, то значи да је TX служио само за позив функције на смарт уговору
        //(нпр. interaction са DeFi протоколом).
        public required decimal GasUsed { get; set; }
        //Количина gas-а који је стварно потрошен да би се извршила трансакција.
        //Gas = мера колико је computational work-а било потребно.
        public required decimal GasPriceGwei { get; set; }
        //Цена по јединици gas-а коју је пошиљалац био спреман да плати,
        //изражена у Gwei (1 ETH = 1e9 Gwei).
        //Умножиш GasUsed * GasPriceGwei и добијеш укупну цену TX.
        public required bool isError { get; set; }
        //Флаг да ли је трансакција фејловала.false → трансакција је успешно извршена.\
        //true → трансакција је покушана, али је дошло до грешке
        //нпр.недостатак gas-а, revert у смарт уговору
    }
}
