# HefajstosSync - mapowanie MongoDB -> SQL

Dokument opisuje, z jakich pól MongoDB korzysta narzędzie `IntegrationHub.Tools.HefajstosSync`
i jak mapuje je do tabel SQL `piesp.BronOsoby` oraz `piesp.BronAdresy`.

## 1) Zakres danych używanych z MongoDB

### Kolekcja `moja_baza.RejestrBroni`

Narzędzie korzysta tylko z:

- `Rejestry_Broni` (tablica)
- `Rejestry_Broni[].data_wyrejestrowania_broni` - tylko rekordy z wartością `null`
- `Rejestry_Broni[].Osoby.pesel_osoby`
- `rodzaj_broni` (na poziomie dokumentu)

Na tej podstawie ładowana jest tabela `piesp.BronOsoby`.

### Kolekcja `moja_baza.Osoby`

Narzędzie korzysta tylko z:

- `Osoba.pesel_osoby`
- `Adresy` (tablica)
- `Adresy[].miejsce_broni` - tylko rekordy z wartością `true`
- `Adresy[].miejscowosc`
- `Adresy[].ulica`
- `Adresy[].numer_domu`
- `Adresy[].numer_lokalu`
- `Adresy[].kod_pocztowy`
- `Adresy[].poczta`

Na tej podstawie ładowana jest tabela `piesp.BronAdresy`.

## 2) Mapowanie kolumn MongoDB -> SQL

### 2.1 `moja_baza.RejestrBroni` -> `piesp.BronOsoby`

| MongoDB | SQL | Uwagi |
|---|---|---|
| `Rejestry_Broni[].Osoby.pesel_osoby` | `piesp.BronOsoby.BO_PESEL` | Tylko wpisy aktywne (`data_wyrejestrowania_broni == null`) |
| `rodzaj_broni` | `piesp.BronOsoby.BO_OPIS` | Dla danego PESEL łączone w listę oddzielaną przecinkami |
| `Rejestry_Broni[].data_wyrejestrowania_broni` | (filtr) | Nie jest zapisywane do SQL; służy tylko do filtrowania aktywnych wpisów |

### 2.2 `moja_baza.Osoby` -> `piesp.BronAdresy`

| MongoDB | SQL | Uwagi |
|---|---|---|
| `Osoba.pesel_osoby` | `piesp.BronAdresy.BA_BOPESEL` | Tylko PESEL obecne wcześniej w `BronOsoby` |
| `Adresy[].miejscowosc` | `piesp.BronAdresy.BA_MIEJSCOWOSC` | Tylko adresy z `miejsce_broni == true` |
| `Adresy[].ulica` | `piesp.BronAdresy.BA_ULICA` | jw. |
| `Adresy[].numer_domu` | `piesp.BronAdresy.BA_NUMER_DOMU` | jw. |
| `Adresy[].numer_lokalu` | `piesp.BronAdresy.BA_NUMER_LOKALU` | jw. |
| `Adresy[].kod_pocztowy` | `piesp.BronAdresy.BA_KOD_POCZTOWY` | jw. |
| `Adresy[].poczta` | `piesp.BronAdresy.BA_POCZTA` | jw. |
| `Adresy[].miejsce_broni` | (filtr) | Nie jest zapisywane do SQL; służy tylko do wyboru właściwych adresów |

## 3) Dodatkowe uzupełnienie po stronie SQL

Po insercie danych do `piesp.BronAdresy` narzędzie wykonuje aktualizację:

- `piesp.BronAdresy.BA_OPIS` <- `piesp.BronOsoby.BO_OPIS`
- klucz łączenia: `BA_BOPESEL = BO_PESEL`

Czyli opis rodzajów broni wyliczony w `BronOsoby` jest kopiowany do wszystkich adresów danej osoby w `BronAdresy`.

