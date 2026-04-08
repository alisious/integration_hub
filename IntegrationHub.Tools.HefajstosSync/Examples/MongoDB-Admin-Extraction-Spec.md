# Specyfikacja dla administratora MongoDB

## Cel

Przygotować kopię bazy MongoDB `moja_baza` zawierającą **wyłącznie**
kolekcje i pola używane przez importer `IntegrationHub.Tools.HefajstosSync`.

## Zasada główna

- **Nie wykonywać logiki biznesowej** (bez filtrowania, deduplikacji, agregacji, joinów).
- Operacja ma polegać tylko na:
  - pozostawieniu wymaganych kolekcji,
  - usunięciu zbędnych pól z dokumentów.

## Kolekcje do pozostawienia

- `RejestrBroni`
- `Osoby`

## 1) Kolekcja `RejestrBroni` – minimalny schemat

W każdym dokumencie zostawić tylko:

- `_id`
- `rodzaj_broni`
- `Rejestry_Broni` (tablica), a w każdym elemencie:
  - `data_wyrejestrowania_broni`
  - `Osoby` (obiekt), a w nim:
    - `pesel_osoby`

Przykładowa docelowa struktura:

```json
{
  "_id": "EB720D61-BE41-4A45-9043-0C8242E86E30",
  "rodzaj_broni": "Pistolet",
  "Rejestry_Broni": [
    {
      "data_wyrejestrowania_broni": null,
      "Osoby": {
        "pesel_osoby": "77040102494"
      }
    }
  ]
}
```

## 2) Kolekcja `Osoby` – minimalny schemat

W każdym dokumencie zostawić tylko:

- `_id`
- `Osoba` (obiekt), a w nim:
  - `pesel_osoby`
- `Adresy` (tablica), a w każdym elemencie:
  - `miejsce_broni`
  - `miejscowosc`
  - `ulica`
  - `numer_domu`
  - `numer_lokalu`
  - `kod_pocztowy`
  - `poczta`

Przykładowa docelowa struktura:

```json
{
  "_id": "FFFC3A14-0FA1-47B3-B743-D759C55FABFB",
  "Osoba": {
    "pesel_osoby": "77040102494"
  },
  "Adresy": [
    {
      "miejsce_broni": true,
      "miejscowosc": "Racławice",
      "ulica": "Nowowiejska",
      "numer_domu": "1a",
      "numer_lokalu": null,
      "kod_pocztowy": "37-400",
      "poczta": "Nisko"
    }
  ]
}
```

## Czego nie robić

- Nie usuwać dokumentów z `RejestrBroni` na podstawie `data_wyrejestrowania_broni`.
- Nie usuwać adresów z `Osoby.Adresy` na podstawie `miejsce_broni`.
- Nie zmieniać nazw pól.
- Nie zmieniać typów danych.
- Nie wykonywać transformacji danych.

## Uzasadnienie

Importer `HefajstosSync` wykonuje logikę biznesową po swojej stronie:

- filtruje aktywne wpisy po `data_wyrejestrowania_broni == null`,
- wybiera tylko `Adresy` z `miejsce_broni == true`,
- buduje opis rodzajów broni i mapowanie do SQL.

MongoDB ma dostarczyć tylko odchudzone, ale semantycznie surowe dane wejściowe.

