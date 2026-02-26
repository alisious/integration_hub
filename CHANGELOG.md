# Changelog

## [1.1.2] - 2026-02-23
### Changed
- Refaktoryzacja SRP na `Result<T, Error>` – serwisy zwracają Result zamiast ProxyResponse
- ProxyResponseMapper – nowa sygnatura `ToProxyResponse(source, requestId)`, przeciążenie bez parametrów dla zgodności wstecznej
- PiespController, ZWController – uproszczone mapowanie Result na ProxyResponse
- ZWSourceService – uporządkowane nazwy serwisu ZW i DTO (wanted person / weapon holder / soldier / address)
- CEPUdostepnianie – rozszerzenie `PytanieOPojazdRozszerzoneResponse` o dane właściciela–osoby (wariantPodmiotu 7)
### Fixed
- Usunięcie martwego kodu z SRPController
