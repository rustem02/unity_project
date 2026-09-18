# VR Training Scenario

VR-тренировка по сценарию с группами шагов: лобби → сцена тренировки → итоги. Визуал на примитивах, URP, перемещение — скольжение (WASD) и телепорт (T).

## Быстрый старт

1. Откройте проект в **Unity 6000.6.2f1**.
2. Дождитесь импорта пакетов (URP + VR Feature Set).
3. Откройте `Assets/_Project/Scenes/Lobby.unity` → Play.
4. Если розовые материалы: **VR Training → Fix URP Pipeline**, затем **Build Project Content**.

### Управление (desktop / без гарнитуры)

| Действие | Управление |
|----------|------------|
| Ходьба | **WASD** / стрелки |
| Обзор | удерживать **ПКМ** + мышь (или Lock Cursor) |
| Телепорт | удерживать **T**, навести мышь на пол, отпустить |
| Клик по объекту | **ЛКМ** по объекту |
| Grab | навести мышь + **E** |
| UI-кнопки | **ЛКМ** по кнопке |

Важно: телепорт/клик/grab работают от **позиции мыши** (нужен обзор ПКМ, чтобы видеть цель). Зоны срабатывают при входе в подсвеченный триггер.

## Архитектура

Системы связаны через typed **EventBus** (без DI): интеракции публикуют `PlayerActionEvent`, `ScenarioController` оценивает действие через `StepActionEvaluator` и шлёт события шага/группы/итога. Данные — `ScenarioDefinition` (ScriptableObject): группы → шаги → действия (`ReachZone` / `Grab` / `Click` / `PressUIButton`). Подсветка — `IHighlightable` + `OutlineHighlighter` (адаптер под Highlight Plus), звук и UI на тех же событиях.

Нарушение порядка закрывает группу: готовые шаги сохраняют статус, остальные — `Skipped`. Неверная цель текущего типа действия → `Failed`.

## Структура

```
Assets/_Project/
  Scripts/   Core, Scenario, Interaction, Highlight, Audio, UI, VR, Lobby, Editor
  Scenes/    Lobby, Training
  Materials / ScriptableObjects / Prefabs / Settings
```

Сценарий «Таможенный контроль»: 3 группы × 3 шага + отвлекающие зоны/объекты/кнопки.

## Соответствие ТЗ

- Лобби → Training, UI мышью
- 3 группы × 3 шага, success/fail/skip, sequence violation
- Действия: зона, grab, click, UI; нарушения неверных целей
- Звук success/fail, подсветка целей, итоги Restart/Lobby
- EventBus, SOLID, URP, примитивы, git-история, README

## Гибкая система сценариев (видение)

Граф шагов с переходами Success/Fail/SequenceBreak и Strategy для условий (`IActionCondition`), контент в JSON/SO со ссылками на `InteractableBinding`. Текущий линейный 3×3 — рабочий поднабор той же модели.
