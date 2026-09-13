# Bojovník

1. V horním panelu nastav virtuální kredit a sázku na celý zápas.
2. Rozděl přesně 12 bodů mezi sílu, obranu, obratnost, odolnost, taktiku a štěstí. Každý perk má 0–5 bodů.
3. Zahaj zápas. PC nezávisle rozdělí stejných 12 bodů a odkryje své perky. Sázka se rezervuje, vstupy se zamknou.
4. Postupně odehraj pět kol. Z osmi situací se bez opakování vybere pět; příběh, zranění a obrázek vycházejí z výsledku konkrétního kola.
5. Po pátém kole rozhoduje počet vyhraných kol. Výhra vrací dvojnásobek rezervované sázky, remíza ji vrací celou, prohra nic. Čistá bilance je tedy +sázka, 0 nebo −sázka. Vzdání zápasu znamená prohru.

Zdraví začíná na 100 a přenáší se mezi koly. Bojová síla = 3 × hlavní perk scénáře + 2 × vedlejší perk + štěstí + hod 1–20 − únava. Odolnost tlumí únavu a obrana zranění. Vyšší síla vyhraje kolo, shoda je remíza. Zdraví určuje únavu, o vítězi zápasu rozhodují vyhraná kola. Zranění je omezené tak, aby vždy proběhlo všech pět kol.

Kronika uchovává všech pět výsledků a obrázků. **Uložit kroniku** vytvoří ZIP s PNG obrázky a českým textem. Nový zápas vymaže předchozí kroniku z paměti, proto ji nejdřív ulož.

## Ilustrace

- **Místní ilustrace** fungují hned bez připojení. Aplikace skládá připravenou arénu, pózy dvou rytířů, počasí a efekty podle scénáře a vítěze. Nejde o nový požadavek na AI v každém kole.
- **AI obrázky** posílají po každém kole jeho příběh a výsledek do OpenAI Image API. Vyber tento režim a otevři **Nastavení AI obrázků**, případně nastav proměnnou prostředí `OPENAI_API_KEY`. Klíč se neukládá na disk, do kroniky ani do Gitu; dialog jej drží jen po dobu spuštění aplikace.
- Použit je `gpt-image-2`, velikost 1536 × 1024, nízká kvalita, jeden PNG na kolo. Zápas může vytvořit až pět placených API požadavků. API účet se účtuje odděleně od virtuální sázky a předplatného ChatGPT. Viz [oficiální Image API dokumentace](https://developers.openai.com/api/docs/guides/image-generation).
- Při chybě nebo po **Přeskočit AI** zůstane místní ilustrace. Kolo se neopakuje a sázka se znovu neúčtuje. Přeskočení ruší místní čekání; požadavek už přijatý poskytovatelem může být zpoplatněn. Čekání je omezeno na 150 sekund, automatické placené opakování se neprovádí.
- AI prompt zachovává tyrkysového hráče vlevo a rudého soupeře vpravo a obsahuje skutečného vítěze; generativní model přesto nemusí všechny vizuální detaily dodržet. Herní výsledek určuje aplikace před vytvořením obrázku.

Integrace má testy požadavků, odpovědí, chyb a zrušení pomocí simulované HTTP služby. Živý placený požadavek nebyl během vývoje proveden, protože nebyl dostupný uživatelský API klíč. Původ podkladů a zadání jsou v [Assets/Warrior/README.md](../Assets/Warrior/README.md).
