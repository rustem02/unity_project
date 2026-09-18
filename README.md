# VR Training Scenario

VR-тренировка по сценарию с группами шагов: лобби → сцена тренировки → итоги. Визуал на примитивах, URP, перемещение — скольжение (WASD) и телепорт (T).

## Быстрый старт

1. Откройте проект в **Unity 6000.6.2f1**.
2. Дождитесь импорта пакетов (URP + VR Feature Set / XR Interaction Toolkit).
3. Если сцены уже есть (`Assets/_Project/Scenes/Lobby.unity`, `Training.unity`) — откройте **Lobby** и Play.
4. Если нужно пересобрать контент: меню **VR Training → Build Project Content**.

### Управление (desktop / без гарнитуры)

| Действие | Клавиши |
|----------|---------|
| Ходьба | WASD |
| Обзор | ПКМ + мышь |
| Телепорт | удерживать **T**, отпустить на точке |
| Клик по объекту | ЛКМ |
| Grab | **E** (луч на объект) |
| UI | мышь / XR UI ray |

## Архитектура

Системы связаны через тонкий typed **EventBus** (без DI-контейнеров): интеракции публикуют `PlayerActionEvent`, `ScenarioController` оценивает действие через `StepActionEvaluator` и шлёт события шага/группы/итога. Данные сценария — `ScenarioDefinition` (ScriptableObject): группы → шаги → ожидаемые действия (`ReachZone` / `Grab` / `Click` / `PressUIButton`). Подсветка идёт через `IHighlightable` + `OutlineHighlighter` (адаптер `HighlightPlusAdapter` для Highlight Plus), звук и UI подписаны на те же события.

Нарушение порядка (действие более позднего шага) закрывает текущую группу: завершённые шаги сохраняют статус, остальные — `Skipped`. Неверный целевой объект текущего типа действия помечает шаг как `Failed`.

## Структура

```
Assets/_Project/
  Scripts/   Core, Scenario, Interaction, Highlight, Audio, UI, VR, Lobby, Editor
  Scenes/    Lobby, Training
  Materials / ScriptableObjects / Prefabs / Audio / Tests
```

Сценарий по умолчанию — «Таможенный контроль»: 3 группы × 3 шага (документы → досмотр → выход), с отвлекающими зонами/объектами/кнопками для ошибок.

## Гибкая система сценариев (видение)

Наиболее расширяемый вариант — граф шагов с условиями перехода (Success / Fail / SequenceBreak) и composition expected-actions через Strategy (`IActionCondition`), а контент хранить в JSON/SO с id-ссылками на сценические `InteractableBinding`. Тогда автор сценария не трогает код, а рантайм остаётся тем же: шина событий + чистый evaluator. Текущая реализация — линейный поднабор этой модели (3×3), достаточный для ТЗ и легко наращиваемый до графа без ломки интеракций.
