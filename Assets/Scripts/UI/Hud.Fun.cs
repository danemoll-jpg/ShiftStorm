using UnityEngine;
using UnityEngine.UI;

namespace ShiftStorm
{
    // Round 17: the HUD for the items and the stunts. Item slot top centre-left (the icon, its name, "B: use"; a roulette while a box rolls),
    // a red warning for anything coming at you, the stunt score under the place, the combo going (tricks x multiplier) and the points
    // popping up as they land, HYPE x2 while the Hype Horn runs, the Stunt Show's standing on points.
    public sealed partial class Hud
    {
        RawImage itemIcon; Image itemBack; Text itemName, warnText, stuntText, comboText, popText;
        void BuildFun(Transform p)
        {
            var slot = Panel(p, new Vector2(92, 92), new Color(0, 0, 0, .5f)); var sr = slot.GetComponent<RectTransform>();
            sr.anchorMin = sr.anchorMax = sr.pivot = new Vector2(0, 1); sr.anchoredPosition = new Vector2(250, -70); itemBack = slot.GetComponent<Image>();
            var ig = new GameObject("item icon"); ig.transform.SetParent(slot.transform, false); itemIcon = ig.AddComponent<RawImage>();
            var ir = itemIcon.rectTransform; ir.anchorMin = ir.anchorMax = ir.pivot = new Vector2(.5f, .5f); ir.sizeDelta = new Vector2(80, 80); ir.anchoredPosition = Vector2.zero;
            itemName = Label(p, new Vector2(0, 1), new Vector2(350, -76), 22, TextAnchor.UpperLeft, new Vector2(420, 60)); itemName.supportRichText = true;
            warnText = Label(p, new Vector2(.5f, 1), new Vector2(0, -96), 40, TextAnchor.UpperCenter, new Vector2(1000, 50)); warnText.color = new Color(1f, .32f, .25f);
            stuntText = Label(p, new Vector2(0, 1), new Vector2(24, -236), 26, TextAnchor.UpperLeft, new Vector2(500, 70)); stuntText.supportRichText = true; stuntText.color = new Color(1f, .85f, .35f);
            comboText = Label(p, new Vector2(.5f, .5f), new Vector2(0, -70), 34, TextAnchor.MiddleCenter, new Vector2(1000, 46)); comboText.supportRichText = true;
            popText = Label(p, new Vector2(.5f, .5f), new Vector2(0, -20), 44, TextAnchor.MiddleCenter, new Vector2(1100, 56));
        }
        void UpdateFun()
        {
            if (!itemIcon || !v) return;
            var bag = v.Bag; bool itemsOn = game && Items.OnNow(game) && !v.Finished;
            var k = bag ? bag.RollShown : ItemKind.None; bool show = itemsOn && k != ItemKind.None;
            itemBack.enabled = itemsOn; itemIcon.enabled = show;
            if (show) { var tex = Items.Icon(k); itemIcon.texture = tex; itemIcon.color = tex ? Color.white : Items.Tint(k); }
            string key = v.Input.Device is UnityEngine.InputSystem.Keyboard ? "Q" : "B";
            if (!itemsOn) itemName.text = "";
            else if (bag.Rolling) itemName.text = "<color=#cccccc>...</color>";
            else if (k == ItemKind.None) itemName.text = "<size=17><color=#aaaaaa>no item</color></size>";
            else itemName.text = $"<b>{Items.Name(k)}</b>  <color=#ffd34d>{key}</color>\n<size=16>{(Time.time < bag.StatusUntil ? $"<color=#ff9a70>{bag.Status}</color>" : Items.Hint(k))}</size>";
            // a warning: something coming at you
            warnText.text = Time.time < v.WarningUntil && !v.Finished ? v.Warning : Time.time < v.ShieldUntil ? "<size=26>BUBBLE SHIELD</size>" : "";
            warnText.supportRichText = true; warnText.color = Time.time < v.WarningUntil ? new Color(1f, .32f, .25f) : new Color(.55f, 1f, 1f);
            // the stunt score (and the Stunt Show's standing)
            bool race = game && game.Race && game.Race.Active && v.Course && v.Course.timeTrial;
            if (race && !v.Holding)
            {
                string standing = "";
                if (game.StuntShow) { var o = game.ShowOrder(); standing = $"   <size=20>SHOW {Race.Ordinal(o.IndexOf(v) + 1)}</size>"; }
                stuntText.text = $"STUNTS <b>{v.StuntScore:#,0}</b>{standing}{(Time.time < v.HypeUntil ? $"   <color=#ff9a30>HYPE x2 {v.HypeUntil - Time.time:0}</color>" : "")}";
            }
            else stuntText.text = "";
            // the combo going, the points landing
            if (v.ComboCount > 0 || v.TrickBusy)
                comboText.text = $"<color=#ffe680>{(v.TrickBusy ? v.TrickName.ToUpper() + "  " : "")}{v.ComboPoints:0}</color> <size=26>x{v.ComboMultiplier:0.#}</size>{(v.ComboShift ? "  <color=#9cff9c>SHIFT!</color>" : "")}";
            else comboText.text = "";
            if (Time.time < v.PopUntil) { popText.text = v.Pop; popText.color = v.PopColor; float k2 = Mathf.Clamp01((v.PopUntil - Time.time) * 3f); popText.rectTransform.localScale = Vector3.one * (1f + .25f * (1 - Mathf.Clamp01((Time.time - (v.PopUntil - 1.2f)) * 6f))); var c = popText.color; c.a = k2; popText.color = c; }
            else popText.text = "";
        }
        public string ItemText => itemName ? itemName.text : ""; public string WarnText => warnText ? warnText.text : ""; public string StuntText => stuntText ? stuntText.text : "";
        public string PopShown => popText ? popText.text : "";

        // the results: every racer's stunt points; in Stunt Show the standing on points
        string StuntResults()
        {
            var race = game.Race; if (!race || !race.Active) return "";
            var sb = new System.Text.StringBuilder();
            if (game.StuntShow)
            {
                var o = game.ShowOrder(); int mine = o.IndexOf(v) + 1;
                sb.AppendLine($"<size=26><color=#ffd34d><b>STUNT SHOW: {Race.Ordinal(mine)}</b></color></size>   <size=18>(stunt points + place bonus {string.Join(" / ", Game.ShowBonus)})</size>");
                for (int i = 0; i < o.Count; i++) { var r = o[i]; sb.AppendLine($"{(r == v ? "<b>" : "")}{i + 1}. <color=#{ColorUtility.ToHtmlStringRGB(r.Tint)}>{r.RacerName}</color>   {game.ShowPoints(r):#,0} pts  <size=17>({r.StuntScore:#,0} stunts, {Race.Ordinal(race.Place(r))} place, best combo {r.BestCombo:#,0})</size>{(r == v ? "</b>" : "")}"); }
            }
            else sb.AppendLine($"<size=20>STUNTS: <b>{v.StuntScore:#,0}</b> ({v.TricksLanded} tricks, {v.TricksFailed} wipeouts, best combo {v.BestCombo:#,0}{(v.ShiftTricks > 0 ? $", {v.ShiftTricks} shift tricks" : "")})</size>");
            return sb.ToString();
        }
    }
}
