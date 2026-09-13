# Bojovník

1. V horním panelu nastav virtuální kredit a sázku na celý zápas.
2. Vyber vyváženého rytíře, berserka, strážce nebo šermíře a případně uprav perky. Rozděl přesně 12 bodů mezi sílu, obranu, obratnost, odolnost, taktiku a štěstí. Každý perk má 0–5 bodů; najetí myší vysvětlí jeho význam.
3. Zahaj zápas. PC nezávisle rozdělí stejných 12 bodů a odkryje své perky. Sázka se rezervuje, vstupy se zamknou.
4. Před každým kolem si vlevo přečti scénář a náznak soupeřova záměru. Zvol **nápor, kryt nebo lest** a teprve poté odehraj kolo. První kolo je vstup do arény, prostřední tři se losují ze sedmi různých střetů bez opakování a páté je finále o korunu. Příběh navazuje na předchozí situaci, výsledek a zbývající zdraví.
5. Po pátém kole rozhoduje počet vyhraných kol. Výhra vrací dvojnásobek rezervované sázky, remíza ji vrací celou, prohra nic. Čistá bilance je tedy +sázka, 0 nebo −sázka. Vzdání zápasu znamená prohru.

Zdraví začíná na 100 a přenáší se mezi koly. Bojová síla = 3 × hlavní perk scénáře + 2 × vedlejší perk + štěstí + hod 1–20 − únava + bonus taktiky + případné vzepětí. Odolnost tlumí únavu a obrana zranění. Vyšší síla vyhraje kolo, shoda je remíza. Zdraví určuje únavu, o vítězi zápasu rozhodují vyhraná kola. Zranění je omezené tak, aby vždy proběhlo všech pět kol.

## Taktika a soupeř

| Taktika | Přemůže | Slabina | Další účinek |
| --- | --- | --- | --- |
| Nápor | Lest | Kryt | — |
| Kryt | Nápor | Lest | Zranění menší o 2, minimálně 1. |
| Lest | Kryt | Nápor | — |

Správná protitaktika dává +5 k síle, poražené taktice −5. Stejné volby nemají bonus. PC si tah připraví před volbou hráče; vychází ze svých perků a může reagovat na minulou odhalenou taktiku hráče. Aktuální skrytou volbu hráče nečte. Náznak postoje odpovídá skutečnému záměru v 70 % případů, jinak je klamný. Zaostávající rytíř získá pouze v pátém kole **poslední vzepětí +3 k síle**. Pravidlo platí stejně pro hráče i PC a je oznámeno před kolem.

Zelenomodrý a červený pruh ukazují zdraví, světlejší část poslední ztrátu. Výsledek u obrázku patří zobrazenému kolu. Panel vlevo připravuje následující kolo; při prohlížení historie se výsledek ani příští soupeřův tah nemění. Po zápase vidíš i počet úspěšných přečtení soupeře a můžeš rovnou zahájit odvetu nebo upravit perky.

Kronika uchovává všech pět výsledků a obrázků. Dvojklik zvětší ilustraci, **Celý příběh** otevře text vybraného kola. **Uložit kroniku** vytvoří ZIP s PNG obrázky a českým textem. Nový zápas vymaže předchozí kroniku z paměti, proto ji nejdřív ulož.

## Ilustrace

- **Místní ilustrace** fungují hned bez připojení. Aplikace skládá připravenou arénu, pózy dvou rytířů, počasí a efekty podle scénáře a vítěze. Nejde o nový požadavek na AI v každém kole.
- **AI obrázky** posílají po každém kole jeho příběh a výsledek do OpenAI Image API. Vyber tento režim a otevři **Nastavení AI obrázků**, případně nastav proměnnou prostředí `OPENAI_API_KEY`. Klíč se neukládá na disk, do kroniky ani do Gitu; dialog jej drží jen po dobu spuštění aplikace.
- Použit je `gpt-image-2`, velikost 1536 × 1024, nízká kvalita, jeden PNG na kolo. Zápas může vytvořit až pět placených API požadavků. API účet se účtuje odděleně od virtuální sázky a předplatného ChatGPT. Viz [oficiální Image API dokumentace](https://developers.openai.com/api/docs/guides/image-generation).
- Při chybě nebo po **Přeskočit AI** zůstane místní ilustrace. Kolo se neopakuje a sázka se znovu neúčtuje. Přeskočení ruší místní čekání; požadavek už přijatý poskytovatelem může být zpoplatněn. Čekání je omezeno na 150 sekund, automatické placené opakování se neprovádí.
- AI prompt zachovává tyrkysového hráče vlevo a rudého soupeře vpravo a obsahuje skutečného vítěze; generativní model přesto nemusí všechny vizuální detaily dodržet. Herní výsledek určuje aplikace před vytvořením obrázku.

Integrace má testy požadavků, odpovědí, chyb a zrušení pomocí simulované HTTP služby. Živý placený požadavek nebyl během vývoje proveden, protože nebyl dostupný uživatelský API klíč. Původ podkladů a zadání jsou v [Assets/Warrior/README.md](../Assets/Warrior/README.md).
