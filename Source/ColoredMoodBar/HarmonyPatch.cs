using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using HarmonyLib;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace MoodBarPatch {
    [StaticConstructorOnStartup]
    public class Main {
        public static MethodInfo drawSelectionOverlayOnGUIMethod;
        public static MethodInfo drawCaravanSelectionOverlayOnGUIMethod;
        public static MethodInfo drawIconsMethod;
        public static FieldInfo pawnTextureCameraOffsetField;
        public static FieldInfo deadColonistTexField;
        public static FieldInfo pawnLabelsCacheField;

        public static Texture2D whiteTex;
        public static Texture2D extremeBreakTex;
        public static Texture2D majorBreakTex;
        public static Texture2D minorBreakTex;
        public static Texture2D neutralTex;
        public static Texture2D contentTex;
        public static Texture2D happyTex;


        private static Dictionary<string, bool> loggedMessages;

        static Main() {
            loggedMessages = new Dictionary<string, bool>();
            var harmony = new Harmony("com.github.bc.rimworld.mod.moodbar");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            drawSelectionOverlayOnGUIMethod = typeof(ColonistBarColonistDrawer).GetMethod("DrawSelectionOverlayOnGUI",
                    BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(Pawn), typeof(Rect) }, null);
            drawCaravanSelectionOverlayOnGUIMethod = typeof(ColonistBarColonistDrawer).GetMethod("DrawCaravanSelectionOverlayOnGUI",
                    BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(Caravan), typeof(Rect) }, null);
            drawIconsMethod = typeof(ColonistBarColonistDrawer).GetMethod("DrawIcons", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new Type[] { typeof(Rect), typeof(Pawn) }, null);
            pawnTextureCameraOffsetField = typeof(ColonistBarColonistDrawer).GetField("PawnTextureCameraOffset",
                BindingFlags.Static | BindingFlags.NonPublic);
            deadColonistTexField = typeof(ColonistBarColonistDrawer).GetField("DeadColonistTex",
                    BindingFlags.Static | BindingFlags.NonPublic);
            pawnLabelsCacheField = typeof(ColonistBarColonistDrawer).GetField("pawnLabelsCache",
                BindingFlags.Instance | BindingFlags.NonPublic);

            float colorAlpha = 0.44f;
            Color white = Color.white;
            Color red = Color.red;
            Color orange = new Color(1f, 0.5f, 0.31f, colorAlpha);
            Color yellow = Color.yellow;
            Color neutralColor = new Color(0.87f, 0.96f, 0.79f, colorAlpha);
            Color cyan = Color.cyan;
            Color happyColor = new Color(0.1f, 0.75f, 0.2f, colorAlpha);
            white.a = 1f;
            red.a = orange.a = yellow.a = cyan.a = colorAlpha;

            whiteTex = SolidColorMaterials.NewSolidColorTexture(white);
            extremeBreakTex = SolidColorMaterials.NewSolidColorTexture(red);
            majorBreakTex = SolidColorMaterials.NewSolidColorTexture(orange);
            minorBreakTex = SolidColorMaterials.NewSolidColorTexture(yellow);
            neutralTex = SolidColorMaterials.NewSolidColorTexture(neutralColor);
            contentTex = SolidColorMaterials.NewSolidColorTexture(cyan);
            happyTex = SolidColorMaterials.NewSolidColorTexture(happyColor);
            LogMessage("ColorCodedMoodBar initialized for RimWorld v1.1");
        }

        public static void LogMessage(string text) {
            Log.Message("[ColorCodedMoodBar] " + text);
        }
    }

    [HarmonyPatch(typeof(ColonistBarColonistDrawer), "DrawColonist")]
    public class MoodPatch {
        private static float ApplyEntryInAnotherMapAlphaFactor(Map map, float alpha) {
            if (map == null) {
                if (!WorldRendererUtility.WorldRenderedNow) {
                    alpha = Mathf.Min(alpha, 0.4f);
                }
            }
            else if (map != Find.CurrentMap || WorldRendererUtility.WorldRenderedNow) {
                alpha = Mathf.Min(alpha, 0.4f);
            }

            return alpha;
        }

        public static bool Prefix(ColonistBarColonistDrawer __instance,
            ref Rect rect, ref Pawn colonist, ref Map pawnMap,
            ref bool highlight, ref bool reordering) {
            ColonistBar colonistBar = Find.ColonistBar;
            float entryRectAlpha = colonistBar.GetEntryRectAlpha(rect);
            entryRectAlpha = ApplyEntryInAnotherMapAlphaFactor(pawnMap, entryRectAlpha);

            if (reordering) {
                entryRectAlpha *= 0.5f;
            }

            Color color = new Color(1f, 1f, 1f, entryRectAlpha);
            GUI.color = color;

            GUI.DrawTexture(rect, ColonistBar.BGTex);

            if (colonist.needs != null && colonist.needs.mood != null) {
                Rect position = rect.ContractedBy(2f);
                float num = position.height * colonist.needs.mood.CurLevelPercentage;
                Rect instantLevel = new Rect(
                    rect.x, 
                    position.yMax - ((position.height + 1f) * colonist.needs.mood.CurInstantLevelPercentage),
                    rect.width, 
                    1f
                );

                position.yMin = position.yMax - num;
                position.height = num;

                Texture2D moodTexture = getMoodTexture(ref colonist);
                GUI.DrawTexture(position, moodTexture);

                // Always show where the mood is going in white.
                GUI.DrawTexture(instantLevel, Main.whiteTex);
            }

            if (highlight) {
                int thickness = (rect.width > 22f) ? 3 : 2;
                GUI.color = Color.white;
                Widgets.DrawBox(rect, thickness);
                GUI.color = color;
            }

            Rect rect2 = rect.ContractedBy(-2f * colonistBar.Scale);

            bool isColonistSelected = colonist.Dead ?
                Find.Selector.SelectedObjects.Contains(colonist.Corpse) :
                Find.Selector.SelectedObjects.Contains(colonist);
            if (isColonistSelected && !WorldRendererUtility.WorldRenderedNow) {
                Main.drawSelectionOverlayOnGUIMethod.Invoke(__instance, new object[] { colonist, rect2 });
            }
            else if (WorldRendererUtility.WorldRenderedNow && colonist.IsCaravanMember() && Find.WorldSelector.IsSelected(colonist.GetCaravan())) {
                Main.drawCaravanSelectionOverlayOnGUIMethod.Invoke(__instance, new object[] { colonist.GetCaravan(), rect2 });
            }

            Rect pawnTexturePosition = __instance.GetPawnTextureRect(new Vector2(rect.x, rect.y));

            GUI.DrawTexture(pawnTexturePosition, PortraitsCache.Get(colonist, ColonistBarColonistDrawer.PawnTextureSize, Rot4.South,
                ColonistBarColonistDrawer.PawnTextureCameraOffset, 1.28205f));
            GUI.color = new Color(1f, 1f, 1f, entryRectAlpha * 0.8f);
            Main.drawIconsMethod.Invoke(__instance, new object[] { rect, colonist });
            GUI.color = color;

            if (colonist.Dead) {
                GUI.DrawTexture(rect, (Texture) Main.deadColonistTexField.GetValue(null));
            }

            float num2 = 4f * colonistBar.Scale;
            Vector2 pos = new Vector2(rect.center.x, rect.yMax - num2);
            GenMapUI.DrawPawnLabel(colonist, pos, entryRectAlpha,
                rect.width + colonistBar.SpaceBetweenColonistsHorizontal - 2f,
                (Dictionary<string, string>)Main.pawnLabelsCacheField.GetValue(__instance),
                GameFont.Tiny, true, true);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            return false;
        }

        private static Texture2D getMoodTexture(ref Pawn colonist) {
            float statValue = colonist.GetStatValue(StatDefOf.MentalBreakThreshold, true);
            float currentMoodLevel = colonist.needs.mood.CurLevel;
            Texture2D moodTexture = null;

            // Extreme break threshold
            if (currentMoodLevel <= statValue) {
                moodTexture = Main.extremeBreakTex;
            }
            // Major break threshold
            else if (currentMoodLevel <= statValue + 0.15f) {
                moodTexture = Main.majorBreakTex;
            }
            // Minor break threshold
            else if (currentMoodLevel <= statValue + 0.3f) {
                moodTexture = Main.minorBreakTex;
            }
            // Neutral
            else if (currentMoodLevel <= 0.65f) {
                moodTexture = Main.neutralTex;
            }
            // Content
            else if (currentMoodLevel <= 0.9f) {
                moodTexture = Main.contentTex;
            }
            // Happy
            else {
                moodTexture = Main.happyTex;
            }

            return moodTexture;
        }
    }
}