using System.Collections.Generic;
using Nexus.Core;
using Nexus.Core.Services;
using RingFlow.Gameplay.Strategies;
using UnityEngine;
using UnityEngine.UI;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// Mechanic Guide popup — lists every ring mechanic with its ASCII symbol,
    /// localized name, and short description. Self-building when no prefab exists.
    /// Opened from the HUD's "?" button during gameplay.
    /// </summary>
    [Mediator(typeof(MechanicGuideMediator))]
    public class MechanicGuideView : View, IAuthoredView
    {
        public Button CloseButton { get; private set; }
        private GameObject _card;
        private bool _uiBuilt;

        /// <summary>
        /// Mechanic entry data: symbol character, display name key, description key.
        /// </summary>
        public struct MechanicEntryData
        {
            public readonly string Symbol;
            public readonly string NameKey;
            public readonly string NameFallback;
            public readonly string DescKey;
            public readonly string DescFallback;
            public readonly Color SymbolColor;

            public MechanicEntryData(string symbol, string nameKey, string nameFallback,
                string descKey, string descFallback, Color symbolColor)
            {
                Symbol = symbol;
                NameKey = nameKey;
                NameFallback = nameFallback;
                DescKey = descKey;
                DescFallback = descFallback;
                SymbolColor = symbolColor;
            }
        }

        /// <summary>
        /// All mechanics in display order. Mediator reads descriptions from
        /// localization service; these fallbacks are English-only.
        /// </summary>
        public static readonly MechanicEntryData[] AllMechanics = new[]
        {
            new MechanicEntryData("?", "mechanic.mystery", "Mystery",
                "mechanic.mystery_desc", "Hidden ring — revealed when picked up. May be any color or type.", Color.yellow),
            new MechanicEntryData("*", "mechanic.frozen", "Frozen",
                "mechanic.frozen_desc", "Locked in ice. Cannot be moved until a matching-color ring lands on it to break the ice.", Color.cyan),
            new MechanicEntryData("L", "mechanic.locked_pole", "Locked Pole",
                "mechanic.locked_pole_desc", "This pole is locked. Place a Key ring on it to unlock and use it.", new Color(1f, 0.84f, 0f)),
            new MechanicEntryData("S", "mechanic.stone", "Stone",
                "mechanic.stone_desc", "Immovable obstacle. Rings can stack on it but it can never be picked up.", Color.gray),
            new MechanicEntryData("G", "mechanic.glass", "Glass",
                "mechanic.glass_desc", "Transparent bridge — accepts any ring color on top and can be placed anywhere.", new Color(1f, 1f, 1f, 0.7f)),
            new MechanicEntryData("~", "mechanic.rainbow", "Rainbow",
                "mechanic.rainbow_desc", "Cycles through all colors. Can land on any pole. Converts to Standard after one move.", Color.magenta),
            new MechanicEntryData("1", "mechanic.bomb", "Bomb",
                "mechanic.bomb_desc", "Counts down each move. When it reaches 0 it explodes and you lose the level! Clear it before time runs out.", Color.red),
            new MechanicEntryData("C", "mechanic.chain", "Chain",
                "mechanic.chain_desc", "Linked pair. When one Chain ring moves, its partner is pulled to the same pole automatically.", Color.white),
            new MechanicEntryData("M", "mechanic.magnet", "Magnet",
                "mechanic.magnet_desc", "Attracts matching colors. When a Magnet lands, all same-color rings on the board are pulled to its pole.", Color.magenta),
            new MechanicEntryData("P", "mechanic.paint", "Paint",
                "mechanic.paint_desc", "Paints the ring below it to match its own color. Consumed after painting — becomes a standard ring.", Color.green),
        };

        private void Awake()
        {
            if (transform.childCount == 0)
            {
                BuildUI();
            }
            else
            {
                BindReferencesFromChildren();
            }
        }

        public void BuildUI()
        {
            _uiBuilt = true;

            // Semi-transparent overlay
            var overlay = GameUIResources.CreateOverlay(transform, GameUIResources.OverlayHeavy);
            overlay.name = "Overlay";

            // Scrollable card container
            _card = GameUIResources.CreatePanel("Card", transform);
            var cardRect = _card.GetComponent<RectTransform>();
            GameUIResources.SetAnchors(cardRect, 0.05f, 0.08f, 0.95f, 0.88f);
            var cardImg = _card.GetComponent<Image>();
            cardImg.color = GameUIResources.PanelColor;
            cardImg.raycastTarget = true;

            // Title
            var titleGo = GameUIResources.CreateText("MECHANICS GUIDE", _card.transform,
                28, TextAnchor.MiddleCenter, GameUIResources.AccentColor);
            titleGo.name = "Title";
            titleGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
            GameUIResources.SetAnchors(titleGo.GetComponent<RectTransform>(), 0.08f, 0.88f, 0.92f, 0.97f);

            // Scroll View for mechanic entries
            var scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(_card.transform, false);
            var scrollRect = scrollGo.GetComponent<RectTransform>();
            GameUIResources.SetAnchors(scrollRect, 0.04f, 0.06f, 0.96f, 0.84f);
            var scrollImg = scrollGo.GetComponent<Image>();
            scrollImg.color = Color.clear;
            scrollImg.raycastTarget = true;
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 30f;

            // Viewport
            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var vpRect = viewportGo.GetComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = Vector2.zero;
            vpRect.offsetMax = Vector2.zero;
            var vpMask = viewportGo.GetComponent<Mask>();
            vpMask.showMaskGraphic = false;
            var vpImg = viewportGo.GetComponent<Image>();
            vpImg.color = Color.white;
            vpImg.raycastTarget = false;

            scroll.viewport = vpRect;

            // Content container
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, AllMechanics.Length * 110f + 20f);

            scroll.content = contentRect;

            // Build each mechanic entry row
            for (int i = 0; i < AllMechanics.Length; i++)
            {
                var entry = AllMechanics[i];
                BuildEntryRow(contentGo.transform, entry, i, 110f);
            }

            // Close button
            var closeBtnGo = GameUIResources.CreateButton("CLOSE", _card.transform, 200, 44);
            CloseButton = closeBtnGo.GetComponent<Button>();
            CloseButton.name = "Btn_Close";
            GameUIResources.SetAnchors(closeBtnGo.GetComponent<RectTransform>(), 0.30f, 0.01f, 0.70f, 0.055f);
            GameUIResources.ApplyPrimaryStyle(closeBtnGo);
        }

        private void BuildEntryRow(Transform parent, MechanicEntryData entry, int index, float rowHeight)
        {
            float yPos = -(index * rowHeight + 10f);

            var row = new GameObject($"Entry_{index}", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, yPos);
            rowRect.sizeDelta = new Vector2(0f, rowHeight - 8f);

            // Symbol circle
            var symbolGo = new GameObject("Symbol", typeof(RectTransform), typeof(Image));
            symbolGo.transform.SetParent(row.transform, false);
            var symRect = symbolGo.GetComponent<RectTransform>();
            symRect.anchorMin = new Vector2(0f, 0.1f);
            symRect.anchorMax = new Vector2(0f, 0.9f);
            symRect.sizeDelta = new Vector2(52f, 0f);
            symRect.anchoredPosition = new Vector2(38f, 0f);
            var symImg = symbolGo.GetComponent<Image>();
            symImg.color = new Color(0.15f, 0.15f, 0.20f, 1f);
            symImg.raycastTarget = false;

            // Symbol text
            var symbolTextGo = GameUIResources.CreateText(entry.Symbol, symbolGo.transform,
                26, TextAnchor.MiddleCenter, entry.SymbolColor);
            symbolTextGo.name = "SymbolText";
            symbolTextGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
            GameUIResources.SetAnchors(symbolTextGo.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f);

            // Name text
            var nameGo = GameUIResources.CreateText(entry.NameFallback, row.transform,
                18, TextAnchor.MiddleLeft, GameUIResources.TextColor);
            nameGo.name = "Name";
            nameGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
            GameUIResources.SetAnchors(nameGo.GetComponent<RectTransform>(), 0.14f, 0.55f, 0.96f, 0.95f);

            // Description text
            var descGo = GameUIResources.CreateText(entry.DescFallback, row.transform,
                13, TextAnchor.UpperLeft, GameUIResources.MutedText);
            descGo.name = "Desc";
            descGo.GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Wrap;
            GameUIResources.SetAnchors(descGo.GetComponent<RectTransform>(), 0.14f, 0.05f, 0.96f, 0.55f);
        }

        public void Localize(ILocalizationService loc)
        {
            if (loc == null || !_uiBuilt) return;

            // Localize title
            var title = _card?.transform.Find("Title")?.GetComponent<Text>();
            if (title != null)
                title.text = loc.GetString("mechanic_guide_title", "MECHANICS GUIDE");

            // Localize each entry
            for (int i = 0; i < AllMechanics.Length; i++)
            {
                var row = _card?.transform.Find($"ScrollView/Viewport/Content/Entry_{i}");
                if (row == null) continue;

                var nameText = row.Find("Name")?.GetComponent<Text>();
                if (nameText != null)
                    nameText.text = loc.GetString(AllMechanics[i].NameKey, AllMechanics[i].NameFallback);

                var descText = row.Find("Desc")?.GetComponent<Text>();
                if (descText != null)
                    descText.text = loc.GetString(AllMechanics[i].DescKey, AllMechanics[i].DescFallback);
            }

            GameUIResources.LocalizeButtonText(CloseButton?.gameObject, "settings_close", loc);
        }

        private void BindReferencesFromChildren()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                GameUIResources.AddButtonEffects(btn);
                var upper = btn.name.ToUpperInvariant();
                if (upper.Contains("CLOSE")) CloseButton = btn;
            }
            _card = transform.Find("Card")?.gameObject;
        }
    }
}
