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
            new MechanicEntryData("O", "mechanic.portal", "Portal",
                "mechanic.portal_desc", "Portal pole pair — a ring placed on one portal pole teleports instantly to its linked partner pole.", Color.cyan),
        };

        private void Awake()
        {
            BindReferencesFromChildren();
        }

        /// <summary>
        /// Satisfies IAuthoredView. UI hierarchy is now loaded from prefab;
        /// this method is kept for interface compliance (Editor UI Studio tooling).
        /// </summary>
        public void BuildUI() { }

        public void Localize(ILocalizationService loc)
        {
            if (loc == null) return;

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
