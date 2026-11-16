using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ShiftStorm
{
    // Round 14 item 1: the menus, drawn the same way on every page (Game builds the rows): the page title at the top, one row per line
    // (big text, a highlight bar on the chosen one, "<  value  >" where left / right change it, a slider for a volume), a description line
    // for the chosen row, the controls hint at the bottom. The title screen shows the logo and "Press A / Start"; the course page shows a
    // card per course (picture, name, the forms it uses, best time). Sizes are for the canvas's 1280 x 720 reference: on the Deck's
    // 1280 x 800 everything is x1.11 (rows 32 px, the description 26 px, the hint 22 px). In the Garage and on choose-your-racer the rows sit
    // low in a band so the turntable shows above them.
    public sealed partial class Hud
    {
        const int MaxRows = 9; const float RowStep = 52f, RowW = 780f;
        GameObject menuRoot, dim, titleGroup, cardGroup; Text menuTitle, menuDesc, menuHint, menuNote, pressStart;
        readonly List<(RectTransform root, Image bar, Text label, Text value, Image sliderBg, Image sliderFill)> rowViews = new List<(RectTransform, Image, Text, Text, Image, Image)>();
        readonly List<(RectTransform root, Image frame, RawImage pic, Text name, Text info)> cards = new List<(RectTransform, Image, RawImage, Text, Text)>();
        RectTransform rowsRoot; Text scrollUp, scrollDown;
        static readonly Color Gold = new Color(1f, .83f, .3f), Ink = new Color(.93f, .95f, .92f), Dimmed = new Color(.6f, .65f, .63f);

        void BuildMenu(Transform p)
        {
            menuRoot = new GameObject("Menu"); menuRoot.transform.SetParent(p, false); var mr = menuRoot.AddComponent<RectTransform>(); Stretch(mr);
            dim = new GameObject("dim"); dim.transform.SetParent(menuRoot.transform, false); var di = dim.AddComponent<Image>(); di.color = new Color(.02f, .05f, .07f, .6f); Stretch(di.rectTransform);
            menuTitle = Label(menuRoot.transform, new Vector2(.5f, 1), new Vector2(0, -34), 46, TextAnchor.UpperCenter, new Vector2(1100, 60)); menuTitle.color = Gold; menuTitle.fontStyle = FontStyle.Bold;
            menuNote = Label(menuRoot.transform, new Vector2(.5f, 1), new Vector2(0, -98), 24, TextAnchor.UpperCenter, new Vector2(1100, 34)); menuNote.color = new Color(.62f, .91f, 1f); menuNote.supportRichText = true;
            var rr = new GameObject("rows"); rr.transform.SetParent(menuRoot.transform, false); rowsRoot = rr.AddComponent<RectTransform>(); rowsRoot.anchorMin = rowsRoot.anchorMax = rowsRoot.pivot = new Vector2(.5f, 1); rowsRoot.anchoredPosition = new Vector2(0, -140); rowsRoot.sizeDelta = new Vector2(RowW, MaxRows * RowStep);
            for (int i = 0; i < MaxRows; i++)
            {
                var g = new GameObject("row " + i); g.transform.SetParent(rowsRoot, false); var rt = g.AddComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, 1); rt.sizeDelta = new Vector2(RowW, RowStep - 6); rt.anchoredPosition = new Vector2(0, -i * RowStep);
                var bar = g.AddComponent<Image>(); bar.color = new Color(1, 1, 1, 0);
                var lab = Label(g.transform, new Vector2(0, .5f), new Vector2(26, 0), 30, TextAnchor.MiddleLeft, new Vector2(470, 44));
                var val = Label(g.transform, new Vector2(1, .5f), new Vector2(-24, 0), 30, TextAnchor.MiddleRight, new Vector2(420, 44)); val.supportRichText = true;
                var sb = new GameObject("slider"); sb.transform.SetParent(g.transform, false); var sbi = sb.AddComponent<Image>(); sbi.color = new Color(0, 0, 0, .5f);
                var sr = sbi.rectTransform; sr.anchorMin = sr.anchorMax = sr.pivot = new Vector2(1, .5f); sr.anchoredPosition = new Vector2(-150, 0); sr.sizeDelta = new Vector2(220, 14);
                var sf = new GameObject("fill"); sf.transform.SetParent(sb.transform, false); var sfi = sf.AddComponent<Image>(); sfi.color = Gold;
                var fr = sfi.rectTransform; fr.anchorMin = new Vector2(0, 0); fr.anchorMax = new Vector2(1, 1); fr.pivot = new Vector2(0, .5f); fr.offsetMin = new Vector2(2, 2); fr.offsetMax = new Vector2(-2, -2);
                rowViews.Add((rt, bar, lab, val, sbi, sfi));
            }
            scrollUp = Label(menuRoot.transform, new Vector2(.5f, 1), new Vector2(0, -116), 22, TextAnchor.MiddleCenter, new Vector2(200, 24)); scrollUp.text = "▲ more"; scrollUp.color = Dimmed;
            scrollDown = Label(menuRoot.transform, new Vector2(.5f, 1), new Vector2(0, -140 - MaxRows * RowStep - 2), 22, TextAnchor.MiddleCenter, new Vector2(200, 24)); scrollDown.text = "▼ more"; scrollDown.color = Dimmed;
            menuDesc = Label(menuRoot.transform, new Vector2(.5f, 0), new Vector2(0, 66), 24, TextAnchor.LowerCenter, new Vector2(1060, 64)); menuDesc.color = new Color(.85f, .9f, .86f); menuDesc.horizontalOverflow = HorizontalWrapMode.Wrap;
            menuHint = Label(menuRoot.transform, new Vector2(.5f, 0), new Vector2(0, 18), 20, TextAnchor.LowerCenter, new Vector2(1100, 30)); menuHint.color = Dimmed;
            // the title screen: the logo, the four forms, "Press A / Start"
            titleGroup = new GameObject("title"); titleGroup.transform.SetParent(menuRoot.transform, false); var tr = titleGroup.AddComponent<RectTransform>(); Stretch(tr);
            var logo = new GameObject("logo"); logo.transform.SetParent(titleGroup.transform, false); var li = logo.AddComponent<RawImage>(); li.texture = Resources.Load<Texture2D>("UI/Logo");
            var lr = li.rectTransform; lr.anchorMin = lr.anchorMax = lr.pivot = new Vector2(.5f, .5f); lr.anchoredPosition = new Vector2(0, 120); lr.sizeDelta = new Vector2(1000, 1000 * 560f / 2048f);
            if (!li.texture) { li.enabled = false; var lt = Label(titleGroup.transform, new Vector2(.5f, .5f), new Vector2(0, 130), 110, TextAnchor.MiddleCenter, new Vector2(1100, 140)); lt.text = "SHIFTSTORM"; lt.color = Gold; lt.fontStyle = FontStyle.Bold; }
            var forms = Label(titleGroup.transform, new Vector2(.5f, .5f), new Vector2(0, -36), 30, TextAnchor.MiddleCenter, new Vector2(1100, 40)); forms.text = "CAR  •  BOAT  •  PLANE  •  SUB"; forms.color = new Color(.65f, .93f, 1f);
            var tag = Label(titleGroup.transform, new Vector2(.5f, .5f), new Vector2(0, -76), 22, TextAnchor.MiddleCenter, new Vector2(1100, 30)); tag.text = "One machine. Four forms. One race."; tag.color = Ink;
            pressStart = Label(titleGroup.transform, new Vector2(.5f, 0), new Vector2(0, 120), 40, TextAnchor.MiddleCenter, new Vector2(1000, 54)); pressStart.color = Gold; pressStart.fontStyle = FontStyle.Bold;
            var ver = Label(titleGroup.transform, new Vector2(1, 0), new Vector2(-20, 14), 18, TextAnchor.LowerRight, new Vector2(400, 24)); ver.text = "v" + Application.version; ver.color = Dimmed;
            // the course cards
            cardGroup = new GameObject("cards"); cardGroup.transform.SetParent(menuRoot.transform, false); var cg = cardGroup.AddComponent<RectTransform>(); Stretch(cg);
            menuRoot.SetActive(false);
        }
        static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        void EnsureCards(int n)
        {
            while (cards.Count < n)
            {
                var g = new GameObject("card " + cards.Count); g.transform.SetParent(cardGroup.transform, false); var rt = g.AddComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, 1); rt.sizeDelta = new Vector2(470, 380);
                var fr = g.AddComponent<Image>(); fr.color = new Color(.06f, .09f, .1f, .92f);
                var pg = new GameObject("picture"); pg.transform.SetParent(g.transform, false); var pi = pg.AddComponent<RawImage>(); var pr = pi.rectTransform; pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(.5f, 1); pr.anchoredPosition = new Vector2(0, -10); pr.sizeDelta = new Vector2(450, 253);
                var nm = Label(g.transform, new Vector2(.5f, 1), new Vector2(0, -268), 32, TextAnchor.UpperCenter, new Vector2(450, 40)); nm.fontStyle = FontStyle.Bold;
                var inf = Label(g.transform, new Vector2(.5f, 1), new Vector2(0, -310), 21, TextAnchor.UpperCenter, new Vector2(450, 64)); inf.supportRichText = true; inf.color = new Color(.85f, .9f, .86f);
                cards.Add((rt, fr, pi, nm, inf));
            }
        }

        public bool MenuShown => menuRoot && menuRoot.activeSelf;
        public string MenuScreenText { get { if (!MenuShown) return ""; var sb = new System.Text.StringBuilder(); sb.AppendLine(menuTitle.text); foreach (var r in rowViews) if (r.root.gameObject.activeSelf) sb.AppendLine($"{r.label.text} | {r.value.text}"); sb.Append(menuDesc.text); return sb.ToString(); } }

        void UpdateMenu(bool open)
        {
            if (open != menuRoot.activeSelf) menuRoot.SetActive(open);
            if (!open) return;
            var page = game.MenuPage; bool title = page == Game.Page.Title, gar = Garage.Active, front = game.InFront;
            titleGroup.SetActive(title);
            dim.SetActive(!gar); dim.GetComponent<Image>().color = title ? new Color(.02f, .05f, .08f, .28f) : front ? new Color(.02f, .05f, .07f, .72f) : new Color(.02f, .05f, .07f, .62f);
            menuTitle.text = title ? "" : game.MenuTitle;
            var miss = SplitScreen.Session ? SplitScreen.Session.MissingText : null;
            menuNote.text = miss != null ? $"<color=#ff7060><b>{miss}</b></color>" : game.MenuNote;
            if (title)
            {
                rowsRoot.gameObject.SetActive(false); cardGroup.SetActive(false); scrollUp.enabled = scrollDown.enabled = false; menuDesc.text = "";
                float blink = .55f + .45f * Mathf.Cos(Time.unscaledTime * 3.2f); pressStart.color = new Color(Gold.r, Gold.g, Gold.b, blink);
                pressStart.text = AnyPad ? "Press  A  /  START" : "Press  ENTER";
                menuHint.text = ""; return;
            }
            rowsRoot.gameObject.SetActive(true);
            var rows = game.Rows; int sel = game.MenuIndex;
            bool cardsOn = page == Game.Page.Course; cardGroup.SetActive(cardsOn);
            // where the rows sit: the full page; the Garage / choose your racer: a band at the bottom; the course page: under the cards
            float top = gar ? -(720 - 300) : cardsOn ? -520 : -140;
            rowsRoot.anchoredPosition = new Vector2(0, top); menuTitle.rectTransform.anchoredPosition = new Vector2(0, gar ? -18 : -34); menuTitle.fontSize = gar ? 36 : 46;
            int visible = gar ? 5 : cardsOn ? 2 : MaxRows;
            int first = Mathf.Clamp(sel - visible / 2, 0, Mathf.Max(0, rows.Count - visible));
            for (int i = 0; i < MaxRows; i++)
            {
                var rv = rowViews[i]; int k = first + i; bool on = i < visible && k < rows.Count; rv.root.gameObject.SetActive(on); if (!on) continue;
                var r = rows[k]; bool chosen = k == sel;
                rv.root.anchoredPosition = new Vector2(0, -i * (gar ? 48f : RowStep)); rv.root.sizeDelta = new Vector2(RowW, (gar ? 48f : RowStep) - 6);
                rv.bar.color = chosen ? (gar ? new Color(.32f, .24f, .06f, .92f) : new Color(1f, .78f, .25f, .26f)) : r.Selectable ? new Color(0, 0, 0, gar ? .78f : .42f) : gar ? new Color(0, 0, 0, .6f) : new Color(0, 0, 0, 0);
                int fs = gar ? 27 : r.Selectable ? 30 : 22;
                rv.label.fontSize = fs; rv.value.fontSize = fs;
                rv.label.text = (chosen ? "▶  " : "     ") + r.label; rv.label.color = chosen ? Gold : r.Selectable ? Ink : Dimmed; rv.label.fontStyle = chosen ? FontStyle.Bold : FontStyle.Normal;
                if (!r.Selectable) { rv.label.rectTransform.sizeDelta = new Vector2(RowW - 40, 44); rv.label.text = "     " + r.label; } else rv.label.rectTransform.sizeDelta = new Vector2(470, 44);
                string v = r.value ?? ""; bool slider = r.slider >= 0;
                rv.value.text = v == "" ? "" : r.side != null ? (chosen ? $"<color=#ffd34d>‹</color>  {v}  <color=#ffd34d>›</color>" : $"   {v}   ") : v;
                rv.value.color = chosen ? Color.white : Ink;
                rv.sliderBg.gameObject.SetActive(slider); if (slider) { rv.sliderFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(r.slider), 1); rv.value.rectTransform.sizeDelta = new Vector2(130, 44); } else rv.value.rectTransform.sizeDelta = new Vector2(420, 44);
            }
            scrollUp.enabled = first > 0 && !gar; scrollDown.enabled = first + visible < rows.Count && !gar;
            scrollUp.rectTransform.anchoredPosition = new Vector2(0, top + 24); scrollDown.rectTransform.anchoredPosition = new Vector2(0, top - visible * RowStep - 2);
            menuDesc.text = game.MenuDescription; menuDesc.rectTransform.anchoredPosition = new Vector2(0, gar ? 48 : 66);
            if (gar) menuDesc.text = "";
            bool anySide = sel < rows.Count && rows[sel].side != null;
            menuHint.text = (AnyPad ? "D-pad / stick: move" : "Arrows: move") + (anySide ? (AnyPad ? "    ‹ › : change" : "    Left / Right: change") : "") + (AnyPad ? "    A: choose    B: back" : "    Enter: choose    Backspace: back") + (front ? "" : AnyPad ? "    Start: resume" : "    Esc: resume");
            if (cardsOn) UpdateCards();
        }
        static bool AnyPad => UnityEngine.InputSystem.Gamepad.all.Count > 0;

        void UpdateCards()
        {
            var cs = game.Courses; EnsureCards(cs.Count);
            float w = 470, gap = 30, x0 = -(cs.Count * w + (cs.Count - 1) * gap) * .5f + w * .5f;
            for (int i = 0; i < cards.Count; i++)
            {
                var cd = cards[i]; bool on = i < cs.Count; cd.root.gameObject.SetActive(on); if (!on) continue;
                bool chosen = i == game.MenuCourse; var c = cs[i];
                cd.root.anchoredPosition = new Vector2(x0 + i * (w + gap), -110); cd.root.localScale = Vector3.one * (chosen ? 1.04f : .94f);
                cd.frame.color = chosen ? new Color(.95f, .72f, .2f, .95f) : new Color(.06f, .09f, .1f, .85f);
                cd.pic.texture = CourseCards.Get(i); cd.pic.color = chosen ? Color.white : new Color(.6f, .6f, .6f);
                cd.name.text = c.courseName; cd.name.color = chosen ? new Color(.1f, .06f, .02f) : Ink;
                var best = BestTimes.Get(c.courseName);
                cd.info.text = $"{CourseCards.Forms(c)}\nBest lap: {(best != null ? TimeTrial.Clock(best.lap) : "none yet")}";
                cd.info.color = chosen ? new Color(.15f, .1f, .04f) : new Color(.85f, .9f, .86f);
            }
        }
    }
}
