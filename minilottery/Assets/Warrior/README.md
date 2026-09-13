# Podklady Bojovníka

Vytvořeno 2026-09-13 vestavěným ImageGen, režim generování nového obrázku ze zadání. Oba původní PNG jsou použity beze změny jako podklady pro místní vykreslování; ořez a kompozici provádí aplikace za běhu. Nepoužitý pokus o přerovnání atlasu není součástí projektu.

`arena.png`: 1536 × 1024, pozadí arény. `fighters.png`: 1536 × 1024, transparentní atlas tří póz hráče a tří póz PC. Atlas není pravidelný; LocalBattleIllustrator izoluje postavy pomocí explicitních oblastí a cest ořezu.

## Zadání arény

Use case: stylized-concept. Asset type: finished landscape background for a Windows fantasy fighting game, aspect ratio 3:2. Paint an exquisitely detailed atmospheric medieval ruined tournament arena at dusk. Central broad weathered flagstone fighting ground in the foreground, a towering stone arch gate on the far left, ruined columns and a short staircase on the right, distant pine ridges, cool teal fog, warm amber braziers with convincing fire, drifting embers, moss, carved stone and chipped stonework. Cohesive premium illustrated strategy game art, semi-realistic painterly materials and cinematic volumetric lighting, sharp detailed foreground with atmospheric background depth. Eye-level camera, ample clearly visible open floor across the lower half for two fighters to be composited later. No people, no creatures, no weapons in foreground, no text, no UI, no borders, no watermarks. Deliver the actual art only.

## Zadání postav

Use case: stylized-concept. Asset type: finished transparent character sprite atlas for a medieval fantasy duel game. Create a precise 3-column by 2-row contact sheet on an actually transparent background, landscape canvas, equal cell sizes with generous empty transparent margins around each figure, no cell outlines or text. Six separate full-body detailed semi-realistic painterly knight sprites all at the same scale and ground baseline within their own cell. Top row: the SAME heroic adult knight in brushed steel armor, a rich teal surcoat, teal cape, closed steel helmet with a narrow glowing teal visor and a straight silver sword, facing right. Top-left idle guard pose; top-middle powerful victorious forward sword thrust to the right; top-right defeated kneeling pose, head lowered, sword tip resting on ground. Bottom row: the SAME rival adult knight in blackened iron armor, crimson surcoat and red cape, closed helmet with red visor, straight steel sword, facing left. Bottom-left idle guard pose; bottom-middle powerful victorious forward sword thrust to the left; bottom-right defeated kneeling pose, head lowered, sword tip resting on ground. Realistic human proportions, ornate layered armor plates, leather straps, chainmail, beautiful cloak folds, detailed metallic reflections. All six figures completely contained within their own equal cell without overlap or clipping. No gore, no blood, no text, no scenery, no shadows outside figure. The game will address each cell independently.

## Obrázky za běhu

Místní režim skládá tyto podklady s efekty. Volitelný API režim vytváří nový obrázek podle BattleImagePrompt.cs, kam se dosadí scénář, český příběh, konkrétní vítěz, perky a zdraví. Každý uložený obrázek má v kronice zaznamenaný zdroj (místní / AI).
