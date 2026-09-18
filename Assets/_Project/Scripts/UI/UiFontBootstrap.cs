using UnityEngine;
using UnityEngine.UI;

namespace VRTraining.UI
{
    /// <summary>
    /// Runtime Cyrillic-safe font: OS Dynamic font + atlas prewarm + rebuild refresh.
    /// Serialized TTF Dynamic atlases often leave glyphs as flat "lines" until requested.
    /// </summary>
    public class UiFontBootstrap : MonoBehaviour
    {
        private static readonly string[] OsFontNames =
        {
            "Segoe UI", "Arial", "Tahoma", "Microsoft Sans Serif", "DejaVu Sans"
        };

        private static readonly int[] Sizes = { 32, 40, 48, 64 };

        private const string Alphabet =
            "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя" +
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
            " |.-–—,:;!?()[]{}«»\"'/\\+*=_%#@";

        private const string Phrases =
            "Документы Подтвердить Отклонить Выход Завершить Итог Лобби " +
            "Попытаться ещё Возврат в Лобби Начать тренировку " +
            "ходьба обзор телепорт клик взять Группа шаг зона объект кнопка " +
            "Проверка документов Поиск нарушений Финализация " +
            "успех ошибка пропущен выполнено нарушение последовательности";

        private Font _font;

        private void Awake()
        {
            _font = Font.CreateDynamicFontFromOSFont(OsFontNames, 64);
            if (_font == null)
                return;

            Prewarm(_font);
            ApplyToScene(_font);
            Font.textureRebuilt += OnTextureRebuilt;
        }

        private void OnDestroy()
        {
            Font.textureRebuilt -= OnTextureRebuilt;
        }

        private void OnTextureRebuilt(Font changed)
        {
            if (changed != _font)
                return;
            RefreshAllTexts();
        }

        private static void Prewarm(Font font)
        {
            var sample = Alphabet + " " + Phrases;
            for (var i = 0; i < Sizes.Length; i++)
                font.RequestCharactersInTexture(sample, Sizes[i], FontStyle.Normal);
        }

        private void ApplyToScene(Font font)
        {
            var texts = FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < texts.Length; i++)
            {
                var ui = texts[i];
                if (ui == null)
                    continue;
                ui.font = font;
                if (!string.IsNullOrEmpty(ui.text))
                    font.RequestCharactersInTexture(ui.text, ui.fontSize, ui.fontStyle);
            }

            // Second pass after atlas settles — first pass can leave stale meshes as "lines".
            for (var i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null)
                    ForceRebuild(texts[i]);
            }
        }

        private void RefreshAllTexts()
        {
            var texts = FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null)
                    ForceRebuild(texts[i]);
            }
        }

        private static void ForceRebuild(Text ui)
        {
            var value = ui.text;
            ui.text = string.Empty;
            ui.text = value;
        }

        /// <summary>Used by panels that set text at runtime.</summary>
        public static void EnsureCharacters(Text ui, string value)
        {
            if (ui == null || string.IsNullOrEmpty(value))
                return;
            if (ui.font != null)
                ui.font.RequestCharactersInTexture(value, ui.fontSize, ui.fontStyle);
            ui.text = value;
        }
    }
}
