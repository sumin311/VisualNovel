// =====================================================================
//  임시 그림 생성기 — 캐릭터 스탠딩(800x1400) + CG(1920x1080)
//  실행:  powershell -ExecutionPolicy Bypass -File Tools\PlaceholderArt\run.ps1 [이름 일부]
//  예)    run.ps1 승주      → 이름에 "승주"가 들어간 그림만 다시 그림
//
//  * Windows PowerShell 5.1의 Add-Type(C# 5)로 컴파일되므로 C# 6 이상 문법은 쓰지 말 것
//  * 이 파일은 UTF-8 (BOM 포함)으로 저장해야 한글 파일 이름이 깨지지 않음
//  * 스탠딩은 전부 '왼쪽을 보는 비스듬한 각도' (게임에서 주변 인물은 좌우반전됨)
// =====================================================================
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

public class PB
{
    public GraphicsPath P = new GraphicsPath(FillMode.Winding);
    PointF last;
    public PB M(float x, float y) { P.StartFigure(); last = new PointF(x, y); return this; }
    public PB L(float x, float y) { var n = new PointF(x, y); P.AddLine(last, n); last = n; return this; }
    public PB Q(float cx, float cy, float x, float y)
    {
        var n = new PointF(x, y);
        var c1 = new PointF(last.X + 2f / 3f * (cx - last.X), last.Y + 2f / 3f * (cy - last.Y));
        var c2 = new PointF(x + 2f / 3f * (cx - x), y + 2f / 3f * (cy - y));
        P.AddBezier(last, c1, c2, n); last = n; return this;
    }
    public PB Crv(float tension, params float[] xy)
    {
        var pts = new PointF[xy.Length / 2 + 1];
        pts[0] = last;
        for (int i = 0; i < xy.Length / 2; i++) pts[i + 1] = new PointF(xy[2 * i], xy[2 * i + 1]);
        P.AddCurve(pts, tension); last = pts[pts.Length - 1]; return this;
    }
    public GraphicsPath Z() { P.CloseFigure(); return P; }
    public GraphicsPath O() { return P; }
}

public class Spec
{
    public string Name, Hair, Outfit;
    public Color HairC, HairSh, HairLt, HairLine, BrowC;
    public bool Headphones;
    public bool Male;
}

public class Look
{
    public string Eyes = "open", Brows = "normal", Mouth = "smile", Arms = "sides";
    public bool Blush, Sweat, Anger, Sparkle, Tear;
    public float LookX, LookY;
}

public static class PlaceholderArt
{
    // ───────────────────────── 색 ─────────────────────────
    public static Color H(string h)
    {
        h = h.TrimStart('#'); int a = 255;
        if (h.Length == 8) { a = Convert.ToInt32(h.Substring(0, 2), 16); h = h.Substring(2); }
        return Color.FromArgb(a, Convert.ToInt32(h.Substring(0, 2), 16), Convert.ToInt32(h.Substring(2, 2), 16), Convert.ToInt32(h.Substring(4, 2), 16));
    }
    static Color A(Color c, int a) { return Color.FromArgb(a, c.R, c.G, c.B); }
    static Color Mix(Color a, Color b, float t)
    {
        return Color.FromArgb((int)(a.A + (b.A - a.A) * t), (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
    }

    static readonly Color LINE = H("#3A2C36");
    static readonly Color SKIN = H("#FDE8DC"), SKIN_SH = H("#F4CBBB"), SKIN_LINE = H("#B97C6E");
    static readonly Color SHIRT = H("#FBFBFD"), SHIRT_SH = H("#DADFEC");
    static readonly Color TIE = H("#2D3866"), TIE_SH = H("#1F284F"), TIE_LT = H("#52609A");
    static readonly Color SKIRT = H("#8F939E"), SKIRT_DK = H("#6C7080"), SKIRT_LT = H("#BBBFC9");
    static readonly Color BLAZER = H("#303C62"), BLAZER_SH = H("#212A48"), BLAZER_LT = H("#48568C");
    static readonly Color HOODIE = H("#A3A7AF"), HOODIE_SH = H("#868A94"), HOODIE_LT = H("#C3C6CD");
    static readonly Color BLANKET = H("#27304F"), BLANKET_SH = H("#1A2139"), BLANKET_LT = H("#3F4B7C"), BLANKET_LINE = H("#10152A");
    static readonly Color EYE_DARK = H("#241C2B"), IRIS = H("#3A3046"), IRIS_LT = H("#6E6084"), PUPIL = H("#140F1A");
    static readonly Color MOUTH = H("#A3424F"), MOUTH_LINE = H("#8E4B52"), TONGUE = H("#EC8C96");
    static readonly Color BLUSH = H("#F5939F"), BLUSH_LINE = H("#E56F83");
    static readonly Color GOLD = H("#D4AC57"), GOLD_DK = H("#8E6A2A");
    static readonly Color MINT = H("#6FD6B0"), MINT_DK = H("#3E9C7C");
    static readonly Color PURPLE = H("#B887FF"), PHONES = H("#2C2A35"), PHONES_LT = H("#4A4757");

    // ───────────────────────── 그리기 도구 ─────────────────────────
    static Pen NewPen(Color c, float w)
    {
        var p = new Pen(c, w); p.LineJoin = LineJoin.Round; p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; return p;
    }
    static void F(Graphics g, GraphicsPath p, Color c) { using (var b = new SolidBrush(c)) g.FillPath(b, p); }
    static void S(Graphics g, GraphicsPath p, Color c, float w) { using (var pen = NewPen(c, w)) g.DrawPath(pen, p); }
    static void FS(Graphics g, GraphicsPath p, Color fill, Color line, float w) { F(g, p, fill); S(g, p, line, w); }
    static void Clip(Graphics g, GraphicsPath clip, Action draw)
    {
        var st = g.Save(); g.SetClip(clip, CombineMode.Intersect); draw(); g.Restore(st);
    }
    /// <summary>그림자색으로 칠한 뒤 왼쪽 위로 밀린 본래 색을 덮어, 오른쪽 아래 가장자리에만 그림자가 남게 함</summary>
    static void Cel(Graphics g, GraphicsPath p, Color baseC, Color shade, float dx, float dy)
    {
        F(g, p, shade);
        Clip(g, p, () => { var st = g.Save(); g.TranslateTransform(dx, dy); F(g, p, baseC); g.Restore(st); });
    }
    static void Line(Graphics g, Color c, float w, params float[] xy)
    {
        var pts = Pts(xy);
        using (var pen = NewPen(c, w)) { if (pts.Length == 2) g.DrawLine(pen, pts[0], pts[1]); else g.DrawCurve(pen, pts, 0.5f); }
    }
    static PointF[] Pts(float[] xy)
    {
        var pts = new PointF[xy.Length / 2];
        for (int i = 0; i < pts.Length; i++) pts[i] = new PointF(xy[2 * i], xy[2 * i + 1]);
        return pts;
    }
    static GraphicsPath Ell(float cx, float cy, float w, float h) { var p = new GraphicsPath(); p.AddEllipse(cx - w / 2, cy - h / 2, w, h); return p; }
    static GraphicsPath Poly(params float[] xy) { var p = new GraphicsPath(); p.AddPolygon(Pts(xy)); return p; }
    static GraphicsPath Rect(float x, float y, float w, float h) { var p = new GraphicsPath(); p.AddRectangle(new RectangleF(x, y, w, h)); return p; }
    static GraphicsPath RRect(float x, float y, float w, float h, float r)
    {
        var p = new GraphicsPath(); float d = Math.Min(r * 2, Math.Min(w, h));
        if (d < 1) { p.AddRectangle(new RectangleF(x, y, w, h)); return p; }
        p.AddArc(x, y, d, d, 180, 90); p.AddArc(x + w - d, y, d, d, 270, 90);
        p.AddArc(x + w - d, y + h - d, d, d, 0, 90); p.AddArc(x, y + h - d, d, d, 90, 90);
        p.CloseFigure(); return p;
    }
    static GraphicsPath Capsule(float x0, float y0, float x1, float y1, float w)
    {
        float dx = x1 - x0, dy = y1 - y0; float len = (float)Math.Sqrt(dx * dx + dy * dy);
        var p = RRect(0, -w / 2, len + w / 2, w, w / 2);
        var m = new Matrix(); m.Translate(x0, y0); m.Rotate((float)(Math.Atan2(dy, dx) * 180 / Math.PI)); m.Translate(-w / 4, 0);
        p.Transform(m); return p;
    }
    static GraphicsPath Smooth(bool closed, float tension, params float[] xy)
    {
        var p = new GraphicsPath(FillMode.Winding);
        if (closed) p.AddClosedCurve(Pts(xy), tension); else p.AddCurve(Pts(xy), tension);
        return p;
    }

    static PointF CR(PointF p0, PointF p1, PointF p2, PointF p3, float t)
    {
        float t2 = t * t, t3 = t2 * t;
        return new PointF(
            0.5f * (2 * p1.X + (-p0.X + p2.X) * t + (2 * p0.X - 5 * p1.X + 4 * p2.X - p3.X) * t2 + (-p0.X + 3 * p1.X - 3 * p2.X + p3.X) * t3),
            0.5f * (2 * p1.Y + (-p0.Y + p2.Y) * t + (2 * p0.Y - 5 * p1.Y + 4 * p2.Y - p3.Y) * t2 + (-p0.Y + 3 * p1.Y - 3 * p2.Y + p3.Y) * t3));
    }
    static List<PointF> Spline(PointF[] pts, int seg)
    {
        var r = new List<PointF>();
        for (int i = 0; i < pts.Length - 1; i++)
        {
            PointF p0 = pts[Math.Max(i - 1, 0)], p1 = pts[i], p2 = pts[i + 1], p3 = pts[Math.Min(i + 2, pts.Length - 1)];
            for (int s = 0; s < seg; s++) r.Add(CR(p0, p1, p2, p3, s / (float)seg));
        }
        r.Add(pts[pts.Length - 1]);
        return r;
    }
    /// <summary>중심선을 따라 굵기가 변하는 관(팔, 소매, 머리카락 다발 등). 양 끝은 둥글게</summary>
    static GraphicsPath Tube(float w0, float w1, params float[] xy)
    {
        var c = Spline(Pts(xy), 10); int n = c.Count;
        var Lp = new PointF[n]; var Rp = new PointF[n]; var dirs = new PointF[n];
        for (int i = 0; i < n; i++)
        {
            PointF a = c[Math.Max(i - 1, 0)], b = c[Math.Min(i + 1, n - 1)];
            float dx = b.X - a.X, dy = b.Y - a.Y; float len = (float)Math.Sqrt(dx * dx + dy * dy); if (len < 0.001f) len = 1;
            dx /= len; dy /= len; dirs[i] = new PointF(dx, dy);
            float w = (w0 + (w1 - w0) * i / (float)(n - 1)) / 2f;
            Lp[i] = new PointF(c[i].X - dy * w, c[i].Y + dx * w);
            Rp[i] = new PointF(c[i].X + dy * w, c[i].Y - dx * w);
        }
        var all = new List<PointF>();
        all.AddRange(Lp);
        AddCap(all, c[n - 1], dirs[n - 1], w1 / 2f, true);
        for (int i = n - 1; i >= 0; i--) all.Add(Rp[i]);
        AddCap(all, c[0], dirs[0], w0 / 2f, false);
        var p = new GraphicsPath(FillMode.Winding); p.AddPolygon(all.ToArray()); return p;
    }
    static void AddCap(List<PointF> list, PointF c, PointF f, float w, bool end)
    {
        // 기준: 앞쪽 f, 왼쪽 법선 nl = (-f.y, f.x)  → L 점 = c + nl*w  (Tube의 Lp와 같은 쪽)
        float nlx = -f.Y, nly = f.X;
        for (int j = 1; j < 8; j++)
        {
            double th = end ? Math.PI / 2 - Math.PI * j / 8 : 3 * Math.PI / 2 - Math.PI * j / 8;
            list.Add(new PointF(c.X + w * (float)(Math.Cos(th) * f.X + Math.Sin(th) * nlx), c.Y + w * (float)(Math.Cos(th) * f.Y + Math.Sin(th) * nly)));
        }
    }

    static void Setup(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
    }

    // ───────────────────────── 캐릭터 정의 ─────────────────────────
    static void BlackHair(Spec s)
    {
        s.HairC = H("#2B2733"); s.HairSh = H("#1A1721"); s.HairLt = H("#4F4A63"); s.HairLine = H("#110F16"); s.BrowC = H("#1E1A24");
    }
    static Spec Make(string name, string hair, string outfit)
    {
        var s = new Spec(); s.Name = name; s.Hair = hair; s.Outfit = outfit; BlackHair(s); return s;
    }
    static Spec[] AllSpecs()
    {
        var seungju = Make("승주", "seungju", "blazer");
        var seunghyun = Make("승현", "seunghyun", "shirt");
        var seoyeon = Make("서연", "seoyeon", "blanket");
        seoyeon.HairC = H("#EECF8A"); seoyeon.HairSh = H("#D0A862"); seoyeon.HairLt = H("#FBEFC8"); seoyeon.HairLine = H("#8C6A3B"); seoyeon.BrowC = H("#A07A43");
        var giju = Make("기주", "giju", "shirt"); giju.Headphones = true;
        var sumin = Make("수민", "sumin", "hoodie");
        return new Spec[] { seungju, seunghyun, seoyeon, giju, sumin };
    }
    /// <summary>CG를 그릴 때 남자로 그릴 캐릭터 이름들</summary>
    static HashSet<string> maleSet = new HashSet<string>();

    static Spec AsMale(Spec s) { if (!s.Male) { s.Male = true; s.Hair = "m_" + s.Hair; } return s; }

    static Spec Find(string name)
    {
        foreach (var s in AllSpecs()) if (s.Name == name) return maleSet.Contains(name) ? AsMale(s) : s;
        return null;
    }

    static Look L(string eyes, string brows, string mouth, string arms)
    {
        var k = new Look(); k.Eyes = eyes; k.Brows = brows; k.Mouth = mouth; k.Arms = arms; return k;
    }

    /// <summary>스탠딩 표정 4종 (캐릭터_프롬프트.md 의 캐릭터별 추천 표정을 따름)</summary>
    static Look Standing(Spec s, string e)
    {
        Look k;
        switch (s.Name)
        {
            case "승주":
                if (e == "기본") return L("half", "flat", "line", "sides");
                if (e == "웃음") return L("half", "flatUp", "smug", "sides");
                if (e == "화남") { k = L("glare", "angry", "pout", "crossed"); return k; }
                k = L("open", "worried", "pout", "sides"); k.Blush = true; k.LookX = -1f; k.LookY = 0.5f; return k;
            case "승현":
                if (e == "기본") return L("open", "normal", "smile", "sides");
                if (e == "웃음") return L("happy", "raised", "grin", "peace");
                if (e == "화남") { k = L("open", "angry", "frown", "sides"); k.Anger = true; return k; }
                k = L("open", "worried", "wavy", "sides"); k.Blush = true; k.Sweat = true; k.LookX = -0.9f; k.LookY = 0.6f; return k;
            case "서연": // 겉보기엔 도도, 웃으면 무너지는 반전 매력. 늘 당근 인형을 안고 있음
                if (e == "기본") return L("open", "flat", "line", "carrot");
                if (e == "웃음") { k = L("happy", "raised", "grin", "carrot"); k.Sweat = true; return k; }
                if (e == "화남") { k = L("open", "angry", "pout", "carrot"); k.Anger = true; return k; }
                k = L("open", "worried", "none", "carrotMouth"); k.Blush = true; k.LookX = -0.9f; k.LookY = 0.4f; return k;
            case "기주":
                if (e == "기본") return L("open", "normal", "smug", "sides");
                if (e == "웃음") return L("wink", "raised", "grin", "sides");
                if (e == "화남") { k = L("open", "angry", "pout", "sides"); k.Anger = true; return k; }
                k = L("open", "worried", "wavy", "sides"); k.Blush = true; k.LookX = -0.9f; k.LookY = 0.5f; return k;
            default: // 수민
                if (e == "기본") { k = L("open", "raised", "o", "phone"); return k; }
                if (e == "웃음") { k = L("down", "raised", "open", "phone"); k.Sparkle = true; k.LookX = 0.6f; k.LookY = 1f; return k; }
                if (e == "화남") { k = L("open", "angry", "pout", "phone"); k.Anger = true; return k; }
                k = L("open", "worried", "wavy", "phone"); k.Blush = true; k.Sweat = true; k.LookX = -0.9f; k.LookY = 0.4f; return k;
        }
    }

    // ───────────────────────── 캐릭터 그리기 (800x1400, 왼쪽 보기) ─────────────────────────
    // 머리 부분은 목 아래(400,612)를 기준으로 조금 작게 그려 몸과의 비율을 맞춤
    const float HS = 0.86f, HPX = 400f, HPY = 612f;
    static float HX(float x) { return HPX + (x - HPX) * HS; }
    static float HY(float y) { return HPY + (y - HPY) * HS; }
    static GraphicsState HeadOn(Graphics g)
    {
        var st = g.Save(); g.TranslateTransform(HPX, HPY); g.ScaleTransform(HS, HS); g.TranslateTransform(-HPX, -HPY); return st;
    }

    static bool curMale;
    static string BaseHair(Spec s) { return s.Hair.StartsWith("m_") ? s.Hair.Substring(2) : s.Hair; }
    /// <summary>남자는 어깨·몸통을 가로로 조금 넓게</summary>
    static GraphicsState BodyOn(Graphics g, Spec s)
    {
        var st = g.Save();
        if (s.Male) { g.TranslateTransform(400, 0); g.ScaleTransform(1.08f, 1f); g.TranslateTransform(-400, 0); }
        return st;
    }

    public static void DrawChar(Graphics g, Spec s, Look k)
    {
        curMale = s.Male;
        // 몸 전체를 살짝 키움 (발 쪽 기준)
        var whole = g.Save();
        g.TranslateTransform(400, 1400); g.ScaleTransform(1.06f, 1.06f); g.TranslateTransform(-400, -1400);
        DrawCharInner(g, s, k);
        g.Restore(whole);
    }

    static void DrawCharInner(Graphics g, Spec s, Look k)
    {
        var hs = HeadOn(g); BackHair(g, s); g.Restore(hs);
        var bs = BodyOn(g, s);
        if (s.Outfit == "hoodie") Hood(g);
        if (k.Arms == "blanketOpen") Cape(g);
        bool neckPhones = s.Headphones && k.Arms != "headphonesHold" && k.Arms != "headphoneShare";
        if (neckPhones) PhonesBand(g);
        Neck(g, s.Male);
        Torso(g);
        if (s.Male) Pants(g); else Skirt(g);
        Tie(g);
        Collar(g);
        if (s.Outfit == "blazer") Blazer(g);
        if (s.Outfit == "hoodie") Hoodie(g);
        if (neckPhones) { Cup(g, 318, 652, 24, 1f); Cup(g, 498, 654, -24, 1f); }
        Arms(g, s, k, 0);
        if (s.Outfit == "blanket" && k.Arms != "blanketOpen") Blanket(g);
        Arms(g, s, k, 1);
        g.Restore(bs);
        hs = HeadOn(g);
        Head(g, s, k);
        FrontHair(g, s);
        if (BaseHair(s) == "giju" && !s.Male) PearlEarring(g, 566, 424);
        if (k.Arms == "carrotMouth")
        {
            Carrot(g, 392, 520, 96, 0.72f);
            Hand(g, 300, 520, -10, 0.95f, "fist", 1); Hand(g, 492, 510, 190, 0.95f, "fist", -1);
        }
        Brows(g, s, k);
        Effects(g, k);
        g.Restore(hs);
        bs = BodyOn(g, s);
        Arms(g, s, k, 2);
        g.Restore(bs);
    }

    // ── 뒷머리 ──
    static GraphicsPath BackHairPath(Spec s)
    {
        switch (s.Hair)
        {
            case "seungju":
                return new PB().M(408, 116).Q(212, 120, 212, 380).Q(204, 650, 222, 902)
                    .L(250, 920).L(282, 906).L(318, 924).L(352, 908).L(470, 912).L(508, 926).L(546, 906).L(574, 922).L(594, 900)
                    .Q(614, 650, 606, 380).Q(606, 120, 408, 116).Z();
            case "seunghyun":
                return new PB().M(408, 116).Q(214, 122, 216, 380).Q(210, 520, 196, 630)
                    .Q(190, 678, 230, 686).Q(258, 704, 288, 678).L(530, 678).Q(560, 704, 590, 686).Q(630, 678, 620, 630)
                    .Q(602, 520, 602, 380).Q(604, 122, 408, 116).Z();
            case "seoyeon": // 포니테일이라 뒷머리는 목덜미까지만 (꼬리는 BackHair 에서)
                return new PB().M(408, 116).Q(214, 122, 218, 360).Q(220, 470, 250, 540).L(566, 540).Q(596, 470, 598, 360).Q(602, 122, 408, 116).Z();
            case "giju":
                return new PB().M(408, 116).Q(218, 122, 224, 330).Q(220, 470, 246, 568).L(570, 568).Q(594, 470, 592, 330).Q(598, 122, 408, 116).Z();
            case "m_seungju":
                return new PB().M(408, 116).Q(214, 122, 218, 360).Q(220, 470, 250, 540).L(566, 540).Q(596, 470, 598, 360).Q(602, 122, 408, 116).Z();
            case "m_seunghyun":
                return new PB().M(408, 116).Q(216, 122, 220, 350).Q(222, 440, 246, 500).L(572, 500).Q(594, 440, 596, 350).Q(600, 122, 408, 116).Z();
            case "m_seoyeon":
                return new PB().M(408, 116).Q(212, 122, 216, 360).Q(204, 460, 226, 540).Q(238, 580, 266, 566).L(552, 566).Q(580, 580, 592, 540).Q(612, 460, 600, 360).Q(604, 122, 408, 116).Z();
            case "m_giju":
                return new PB().M(408, 116).Q(218, 122, 224, 330).Q(224, 430, 250, 500).L(566, 500).Q(592, 430, 592, 330).Q(598, 122, 408, 116).Z();
            default:
                return new PB().M(408, 120).Q(224, 126, 232, 330).L(236, 392).L(588, 392).Q(594, 126, 408, 120).Z();
        }
    }

    static void HairFill(Graphics g, Spec s, GraphicsPath p, float dx, float dy)
    {
        Cel(g, p, s.HairC, s.HairSh, dx, dy);
        S(g, p, s.HairLine, 3f);
    }

    static void BackHair(Graphics g, Spec s)
    {
        var p = BackHairPath(s);
        F(g, p, s.HairC);
        Clip(g, p, () =>
        {
            F(g, Ell(410, 640, 360, 520), s.HairSh); // 목 뒤 안쪽 그늘
            F(g, Rect(560, 100, 120, 1100), A(s.HairSh, 200));
        });
        S(g, p, s.HairLine, 3f);
        // 머리결
        if (s.Hair == "seungju")
        {
            Line(g, A(s.HairLine, 150), 2f, 232, 520, 226, 700, 240, 880);
            Line(g, A(s.HairLine, 150), 2f, 590, 520, 596, 720, 584, 890);
        }
        if (s.Hair == "seoyeon")
        {
            // 뒤통수 아래쪽에서 묶은, 살짝 웨이브 진 긴 포니테일
            var tail = new PB().M(584, 236).Q(690, 262, 700, 400).Q(712, 520, 676, 620).Q(650, 700, 688, 790).Q(712, 860, 650, 940)
                .L(640, 906).Q(664, 850, 640, 790).Q(612, 700, 634, 610).Q(660, 500, 640, 400).Q(626, 320, 572, 300).Z();
            HairFill(g, s, tail, -12, -4);
            Clip(g, tail, () => F(g, new PB().M(660, 300).Q(692, 400, 676, 540).L(662, 540).Q(674, 400, 646, 304).Z(), s.HairLt));
            Line(g, A(s.HairLine, 150), 2f, 610, 300, 668, 420, 652, 600, 664, 760, 656, 880);
            Line(g, A(s.HairLine, 150), 2f, 640, 290, 690, 400, 680, 560, 690, 720);
            FS(g, RRect(570, 262, 34, 44, 10), H("#3A3846"), LINE, 2.5f); // 머리끈
        }
        if (s.Hair == "sumin")
        {
            // 포니테일
            var tail = new PB().M(566, 166).Q(684, 168, 696, 320).Q(716, 520, 670, 760).Q(648, 862, 606, 904)
                .L(598, 874).Q(630, 760, 628, 600).Q(628, 380, 558, 232).Z();
            HairFill(g, s, tail, -12, -4);
            Clip(g, tail, () => F(g, new PB().M(640, 230).Q(682, 340, 668, 520).L(652, 520).Q(664, 340, 626, 236).Z(), s.HairLt));
            Line(g, A(s.HairLine, 160), 2f, 600, 260, 652, 420, 650, 640, 626, 840);
            Line(g, A(s.HairLine, 160), 2f, 620, 230, 676, 380, 676, 560, 652, 760);
        }
        if (s.Hair == "m_sumin" || s.Hair == "m_seoyeon")
        {
            // 짧은 꽁지머리
            var tail = new PB().M(566, 166).Q(650, 170, 662, 270).Q(672, 380, 640, 480).L(620, 470).Q(636, 370, 622, 280).Q(608, 226, 558, 232).Z();
            HairFill(g, s, tail, -10, -4);
            Line(g, A(s.HairLine, 160), 2f, 600, 250, 640, 340, 632, 450);
        }
    }

    // ── 몸 ──
    static void Neck(Graphics g, bool male)
    {
        var neck = male ? new PB().M(334, 468).L(440, 468).L(446, 642).Q(390, 664, 330, 650).Z()
                        : new PB().M(342, 468).L(432, 468).L(438, 642).Q(390, 662, 338, 650).Z();
        F(g, neck, SKIN);
        Clip(g, neck, () => F(g, Poly(330, 460, 450, 460, 450, 590, 330, 556), SKIN_SH));
        S(g, neck, SKIN_LINE, 2.5f);
    }

    static GraphicsPath TorsoPath()
    {
        return new PB().M(346, 612).Q(270, 622, 232, 668).Q(234, 850, 280, 1012).L(532, 1014).Q(568, 850, 572, 676).Q(530, 622, 436, 612).Z();
    }

    static void Torso(Graphics g)
    {
        var t = TorsoPath();
        Cel(g, t, SHIRT, SHIRT_SH, -30, -8);
        Clip(g, t, () =>
        {
            F(g, new PB().M(330, 612).Q(390, 706, 452, 612).L(452, 646).Q(390, 742, 330, 646).Z(), SHIRT_SH); // 깃 아래 그늘
            Line(g, A(SHIRT_SH, 255), 3f, 300, 860, 322, 930, 318, 1000);
            Line(g, A(SHIRT_SH, 255), 3f, 500, 840, 486, 920, 494, 1000);
            Line(g, A(SHIRT_SH, 255), 2.5f, 270, 720, 296, 780);
            for (int i = 0; i < 4; i++) FS(g, Ell(402, 950 + i * 0, 9, 9), H("#E9EBF2"), H("#B9BDCA"), 1.5f);
        });
        S(g, t, LINE, 3f);
    }

    static void Pants(Graphics g)
    {
        var legL = new PB().M(280, 996).L(406, 998).L(404, 1404).L(246, 1404).Q(260, 1200, 280, 996).Z();
        var legR = new PB().M(398, 998).L(532, 998).Q(548, 1200, 562, 1404).L(400, 1404).Z();
        foreach (var leg in new[] { legL, legR })
        {
            var lp = leg;
            Cel(g, lp, SKIRT, SKIRT_DK, -22, 0);
            Clip(g, lp, () =>
            {
                for (int y = 1070; y < 1404; y += 90) { F(g, Rect(200, y, 400, 8), A(SKIRT_DK, 90)); F(g, Rect(200, y + 18, 400, 2), A(SKIRT_LT, 150)); }
                for (int x = 250; x < 580; x += 64) { F(g, Rect(x, 990, 7, 420), A(SKIRT_DK, 70)); F(g, Rect(x + 16, 990, 2, 420), A(SKIRT_LT, 140)); }
            });
            S(g, lp, LINE, 3f);
        }
        Line(g, A(LINE, 140), 2.5f, 342, 1060, 330, 1404);
        Line(g, A(LINE, 140), 2.5f, 466, 1060, 480, 1404);
        Line(g, A(LINE, 150), 2.5f, 402, 1024, 404, 1130);
        FS(g, Rect(270, 988, 272, 28), H("#2E2B35"), LINE, 2.5f);
        FS(g, RRect(384, 992, 32, 20, 4), H("#C9CBD3"), LINE, 2f);
    }

    static void Skirt(Graphics g)
    {
        var sk = new PB().M(282, 996).L(530, 998).Q(560, 1200, 608, 1404).L(208, 1404).Q(250, 1200, 282, 996).Z();
        F(g, sk, SKIRT);
        Clip(g, sk, () =>
        {
            int n = 8;
            for (int i = 0; i < n; i++)
            {
                float w0 = 282 + i * 248f / n, w1 = 282 + (i + 1) * 248f / n, h0 = 208 + i * 400f / n, h1 = 208 + (i + 1) * 400f / n;
                if (i % 2 == 1) F(g, Poly(w0, 1000, w1, 1000, h1, 1404, h0, 1404), A(SKIRT_DK, 70));
                float mw = (w0 + w1) / 2, mh = (h0 + h1) / 2;
                Line(g, A(SKIRT_DK, 110), 10f, mw, 1000, mh, 1404);
                Line(g, A(SKIRT_LT, 170), 2f, mw + 9, 1000, mh + 14, 1404);
                Line(g, A(SKIRT_DK, 200), 2.5f, w0, 1010, h0, 1404);
            }
            for (int y = 1060; y < 1404; y += 96)
            {
                F(g, Rect(150, y, 520, 14), A(SKIRT_DK, 120));
                F(g, Rect(150, y + 26, 520, 3), A(SKIRT_LT, 180));
            }
            F(g, Rect(250, 990, 320, 30), SKIRT_DK);
            F(g, Rect(250, 1012, 320, 4), A(LINE, 60));
        });
        S(g, sk, LINE, 3f);
    }

    static void Tie(Graphics g)
    {
        var blade = new PB().M(389, 682).L(411, 682).L(428, 915).L(400, 948).L(372, 915).Z();
        F(g, blade, TIE);
        Clip(g, blade, () =>
        {
            F(g, Poly(400, 682, 440, 682, 440, 960, 400, 960), TIE_SH);
            for (int i = 0; i < 6; i++) Line(g, A(TIE_LT, 200), 4f, 360, 700 + i * 46, 440, 660 + i * 46);
        });
        S(g, blade, LINE, 2.5f);
        var knot = new PB().M(383, 640).L(417, 640).L(411, 684).L(389, 684).Z();
        FS(g, knot, TIE, LINE, 2.5f);
        Line(g, A(TIE_LT, 220), 3f, 390, 650, 396, 676);
    }

    static void Collar(Graphics g)
    {
        var l = new PB().M(346, 608).L(398, 690).L(320, 666).Q(328, 630, 346, 608).Z();
        var r = new PB().M(436, 608).L(402, 690).L(472, 668).Q(458, 630, 436, 608).Z();
        Cel(g, l, SHIRT, SHIRT_SH, -6, -6); S(g, l, LINE, 2.5f);
        Cel(g, r, SHIRT, SHIRT_SH, -6, -6); S(g, r, LINE, 2.5f);
    }

    static void Blazer(Graphics g)
    {
        var left = new PB().M(346, 612).Q(270, 622, 228, 668).Q(230, 870, 262, 1058).L(404, 1066).L(400, 872).L(368, 704).Q(352, 650, 346, 612).Z();
        var right = new PB().M(436, 612).Q(530, 622, 576, 676).Q(570, 870, 542, 1060).L(396, 1066).L(400, 872).L(426, 704).Q(432, 650, 436, 612).Z();
        Cel(g, left, BLAZER, BLAZER_SH, -26, -6); S(g, left, LINE, 3f);
        Cel(g, right, BLAZER, BLAZER_SH, -22, -6); S(g, right, LINE, 3f);
        var lapL = new PB().M(346, 612).L(316, 664).L(350, 694).L(336, 716).L(400, 872).L(370, 704).Z();
        var lapR = new PB().M(436, 612).L(468, 666).L(434, 694).L(450, 716).L(400, 872).L(424, 704).Z();
        FS(g, lapL, BLAZER_LT, LINE, 2.5f);
        FS(g, lapR, BLAZER_LT, LINE, 2.5f);
        Line(g, A(LINE, 160), 2.5f, 272, 966, 344, 972);
        Line(g, A(LINE, 160), 2.5f, 456, 972, 526, 966);
        FS(g, Ell(414, 912, 16, 16), GOLD, GOLD_DK, 2f);
        FS(g, Ell(414, 990, 16, 16), GOLD, GOLD_DK, 2f);
        // 가슴 엠블럼
        var em = new PB().M(470, 752).L(500, 752).L(500, 776).Q(500, 794, 485, 800).Q(470, 794, 470, 776).Z();
        FS(g, em, H("#B53A51"), GOLD, 2.5f);
        Line(g, GOLD, 2f, 485, 760, 485, 790);
    }

    static void Hood(Graphics g)
    {
        var hood = new PB().M(272, 652).Q(278, 556, 408, 546).Q(538, 556, 544, 654).Q(408, 616, 272, 652).Z();
        Cel(g, hood, HOODIE, HOODIE_SH, -10, -10);
        F(g, new PB().M(312, 628).Q(408, 574, 504, 628).Q(408, 600, 312, 628).Z(), HOODIE_SH);
        S(g, hood, LINE, 3f);
    }

    static void Hoodie(Graphics g)
    {
        var left = new PB().M(346, 612).Q(270, 622, 228, 668).Q(232, 880, 256, 1070).L(372, 1070).L(372, 700).Q(360, 640, 346, 612).Z();
        var right = new PB().M(436, 612).Q(530, 622, 576, 676).Q(570, 880, 546, 1070).L(428, 1070).L(428, 700).Q(440, 640, 436, 612).Z();
        foreach (var p in new[] { left, right })
        {
            var pp = p;
            Cel(g, pp, HOODIE, HOODIE_SH, -24, -6);
            Clip(g, pp, () =>
            {
                F(g, Rect(200, 1036, 400, 40), HOODIE_SH);
                for (int x = 206; x < 600; x += 9) Line(g, A(LINE, 50), 1.5f, x, 1040, x, 1072);
            });
            S(g, pp, LINE, 3f);
        }
        // 지퍼
        using (var pen = new Pen(H("#6A6E78"), 5f)) { pen.DashPattern = new float[] { 0.8f, 0.8f }; g.DrawLine(pen, 376, 640, 376, 1064); g.DrawLine(pen, 424, 640, 424, 1064); }
        FS(g, RRect(418, 760, 14, 30, 4), H("#7B7F89"), LINE, 2f);
        // 끈
        Line(g, H("#EEEEF2"), 6f, 384, 636, 380, 740, 376, 820);
        Line(g, H("#EEEEF2"), 6f, 416, 636, 424, 730, 428, 800);
        FS(g, RRect(370, 816, 12, 22, 4), H("#7B7F89"), LINE, 1.5f);
        FS(g, RRect(422, 796, 12, 22, 4), H("#7B7F89"), LINE, 1.5f);
        // 주머니
        Line(g, A(LINE, 170), 2.5f, 262, 900, 300, 880, 336, 930);
        Line(g, A(LINE, 170), 2.5f, 538, 900, 500, 880, 464, 930);
    }

    static void Blanket(Graphics g)
    {
        var b = new PB().M(300, 592).Q(408, 562, 516, 592).Q(612, 616, 640, 700).Q(666, 900, 652, 1182).Q(608, 1198, 566, 1180)
            .Q(528, 1000, 478, 830).Q(446, 720, 440, 622).L(368, 622).Q(360, 720, 328, 830).Q(282, 1000, 244, 1182)
            .Q(200, 1198, 156, 1178).Q(144, 900, 170, 700).Q(200, 612, 300, 592).Z();
        BlanketFill(g, b);
        // 앞자락 가장자리 스티치
        using (var pen = new Pen(A(H("#8E99C6"), 170), 2.5f))
        {
            pen.DashPattern = new float[] { 3f, 2.5f };
            g.DrawCurve(pen, Pts(new float[] { 380, 630, 372, 720, 340, 830, 300, 980, 262, 1160 }), 0.5f);
            g.DrawCurve(pen, Pts(new float[] { 430, 630, 434, 720, 466, 830, 508, 980, 548, 1160 }), 0.5f);
        }
        Fringe(g, 160, 244, 1180);
        Fringe(g, 566, 650, 1180);
    }

    static void BlanketFill(Graphics g, GraphicsPath b)
    {
        Cel(g, b, BLANKET, BLANKET_SH, -26, -10);
        Clip(g, b, () =>
        {
            for (int y = 560; y < 1320; y += 22)
                for (int x = 60; x < 760; x += 26)
                    Line(g, A(BLANKET_LT, 75), 2f, x, y, x + 6, y + 8, x + 12, y);
            Line(g, A(BLANKET_SH, 255), 10f, 210, 760, 226, 980, 214, 1160);
            Line(g, A(BLANKET_SH, 255), 10f, 600, 760, 588, 980, 604, 1160);
        });
        S(g, b, BLANKET_LINE, 3f);
    }

    static void Fringe(Graphics g, float x0, float x1, float y)
    {
        for (float x = x0 + 4; x < x1; x += 9) Line(g, BLANKET_LT, 3f, x, y - 4, x - 1, y + 16);
    }

    static void Cape(Graphics g)
    {
        var b = new PB().M(96, 698).Q(250, 606, 408, 588).Q(566, 606, 720, 698).Q(752, 960, 706, 1310).L(110, 1310).Q(64, 960, 96, 698).Z();
        BlanketFill(g, b);
        Fringe(g, 116, 700, 1310);
    }

    /// <summary>서연의 당근 인형 (주황 몸통 + 초록 잎 + 웃는 얼굴). 로컬 좌표: 잎이 위(-Y), 끝이 아래(+Y)</summary>
    static void Carrot(Graphics g, float cx, float cy, float ang, float sc)
    {
        var st = g.Save(); g.TranslateTransform(cx, cy); g.RotateTransform(ang); g.ScaleTransform(sc, sc);
        var leafC = H("#8FD14F"); var leafSh = H("#6DB33A"); var leafLine = H("#3F7A24");
        foreach (var lf in new[] { new float[] { -34, -150, -62, -232 }, new float[] { 0, -156, 2, -248 }, new float[] { 34, -150, 64, -228 } })
        {
            var leaf = Capsule(lf[0], lf[1], lf[2], lf[3], 40);
            Cel(g, leaf, leafC, leafSh, -6, -4); S(g, leaf, leafLine, 3f);
            Line(g, A(leafLine, 140), 2f, lf[0], lf[1] - 10, (lf[0] + lf[2]) / 2, (lf[1] + lf[3]) / 2);
        }
        var body = new PB().M(-80, -120).Q(0, -168, 80, -120).Q(104, -40, 66, 70).Q(36, 150, 6, 176).Q(-20, 160, -44, 90).Q(-100, -20, -80, -120).Z();
        Cel(g, body, H("#F59A3C"), H("#DA7524"), -14, -6);
        Clip(g, body, () =>
        {
            F(g, Ell(-36, -96, 50, 26), A(Color.White, 70));
            foreach (var r in new[] { new float[] { -84, -30, -58, -36 }, new float[] { 86, 10, 62, 4 }, new float[] { -60, 66, -40, 60 }, new float[] { 70, 80, 48, 74 } })
                Line(g, A(H("#B85A18"), 200), 3f, r[0], r[1], r[2], r[3]);
        });
        S(g, body, H("#8A4612"), 3f);
        // 얼굴
        F(g, Ell(-26, -56, 14, 16), H("#2A1E1A")); F(g, Ell(26, -56, 14, 16), H("#2A1E1A"));
        Line(g, H("#2A1E1A"), 4f, -24, -26, 0, -10, 24, -26);
        F(g, Ell(-46, -34, 22, 12), A(BLUSH, 150)); F(g, Ell(46, -34, 22, 12), A(BLUSH, 150));
        g.Restore(st);
    }

    /// <summary>기주의 작은 진주 귀걸이 (부잣집 아가씨 느낌)</summary>
    static void PearlEarring(Graphics g, float x, float y)
    {
        Line(g, GOLD_DK, 2.5f, x - 2, y - 22, x, y - 8);
        FS(g, Ell(x, y, 20, 20), H("#F7F2EA"), H("#B6ABA0"), 2f);
        F(g, Ell(x - 4, y - 4, 7, 7), Color.White);
    }

    static void PhonesBand(Graphics g)
    {
        Line(g, PHONES, 22f, 304, 646, 298, 590, 408, 570, 518, 590, 512, 646);
        Line(g, PURPLE, 4f, 306, 618, 316, 590, 408, 578, 500, 590, 510, 618);
    }

    static void Cup(Graphics g, float cx, float cy, float ang, float sc)
    {
        var st = g.Save(); g.TranslateTransform(cx, cy); g.RotateTransform(ang); g.ScaleTransform(sc, sc);
        FS(g, Ell(0, 0, 70, 94), PHONES, LINE, 3f);
        F(g, Ell(4, 0, 54, 78), PURPLE);
        F(g, Ell(6, 0, 36, 58), H("#3E3B4A"));
        F(g, Ell(-12, -16, 14, 24), A(Color.White, 90));
        g.Restore(st);
    }

    // ── 팔 ──
    static void SleeveColors(Spec s, out Color sl, out Color sh, out Color cuff)
    {
        if (s.Outfit == "blazer") { sl = BLAZER; sh = BLAZER_SH; cuff = BLAZER_LT; }
        else if (s.Outfit == "hoodie") { sl = HOODIE; sh = HOODIE_SH; cuff = HOODIE_SH; }
        else { sl = SHIRT; sh = SHIRT_SH; cuff = SHIRT_SH; }
    }

    static void Arm(Graphics g, Spec s, float w0, float w1, string hand, int thumb, float hs, params float[] c)
    {
        Color sl, sh, cuff; SleeveColors(s, out sl, out sh, out cuff);
        var tube = Tube(w0, w1, c);
        Cel(g, tube, sl, sh, -w0 * 0.3f, -6);
        S(g, tube, LINE, 3f);
        int n = c.Length / 2;
        float ex = c[2 * n - 2], ey = c[2 * n - 1], px = c[2 * n - 4], py = c[2 * n - 3];
        float ang = (float)(Math.Atan2(ey - py, ex - px) * 180 / Math.PI);
        var st = g.Save(); g.TranslateTransform(ex, ey); g.RotateTransform(ang);
        FS(g, RRect(-16, -w1 / 2 - 3, 22, w1 + 6, 7), cuff, LINE, 2.5f);
        if (s.Outfit == "blazer") FS(g, RRect(4, -w1 / 2 + 3, 9, w1 - 6, 4), SHIRT, LINE, 2f);
        if (s.Outfit == "hoodie") for (int i = -2; i <= 2; i++) Line(g, A(LINE, 70), 1.5f, -12 + i * 4, -w1 / 2, -12 + i * 4, w1 / 2);
        g.Restore(st);
        if (hand != null)
        {
            double rad = ang * Math.PI / 180;
            Hand(g, ex + (float)Math.Cos(rad) * 8, ey + (float)Math.Sin(rad) * 8, ang, hs, hand, thumb);
        }
    }

    static void ArmFarSide(Graphics g, Spec s) { Arm(g, s, 72, 56, "relax", 1, 1f, 238, 668, 216, 770, 206, 890, 212, 1030); }
    static void ArmNearSide(Graphics g, Spec s) { Arm(g, s, 74, 58, "relax", -1, 1f, 566, 672, 590, 770, 600, 890, 594, 1034); }

    static void Arms(Graphics g, Spec s, Look k, int layer)
    {
        switch (k.Arms)
        {
            case "sides":
                if (layer == 0) { ArmFarSide(g, s); ArmNearSide(g, s); }
                break;
            case "crossed":
                if (layer == 0)
                {
                    Arm(g, s, 72, 66, null, 1, 1f, 238, 668, 222, 770, 236, 872);
                    Arm(g, s, 64, 58, null, 1, 1f, 240, 880, 330, 912, 450, 902, 546, 862);
                    Arm(g, s, 74, 66, null, 1, 1f, 566, 672, 588, 770, 574, 872);
                    Arm(g, s, 62, 56, "fist", 1, 1f, 574, 860, 480, 838, 380, 830, 304, 842);
                }
                break;
            case "peace":
                if (layer == 0) ArmFarSide(g, s);
                if (layer == 2) Arm(g, s, 74, 58, "peace", 1, 1f, 566, 672, 614, 772, 628, 852, 620, 760, 600, 624);
                break;
            case "phone":
                if (layer == 0)
                {
                    ArmFarSide(g, s);
                    PhoneBack(g, 436, 786, -14, 1f);
                    Arm(g, s, 74, 58, "fist", -1, 1f, 566, 672, 604, 782, 610, 874, 548, 868, 488, 842);
                }
                break;
            case "phoneEar":
                if (layer == 0) ArmFarSide(g, s);
                if (layer == 2)
                {
                    PhoneBack(g, HX(562), HY(348), 8, 0.85f);
                    Arm(g, s, 74, 58, "fist", -1, 1f, 566, 672, 620, 772, 632, 690, HX(606), HY(540), HX(578), HY(452));
                }
                break;
            case "carrot": // 당근 인형을 가슴 앞에 안고 있음 (담요 위)
                if (layer == 1)
                {
                    Carrot(g, 404, 850, -14, 1f);
                    Hand(g, 300, 860, -6, 0.95f, "fist", 1); Hand(g, 512, 836, 186, 0.95f, "fist", -1);
                }
                break;
            case "blanketOpen":
                if (layer == 0)
                {
                    Arm(g, s, 72, 56, "fist", -1, 1f, 238, 668, 180, 700, 112, 710);
                    Arm(g, s, 74, 58, "fist", 1, 1f, 566, 672, 632, 700, 700, 710);
                }
                break;
            case "reach":
                if (layer == 0) ArmFarSide(g, s);
                if (layer == 2) Arm(g, s, 74, 60, "open", 1, 1.25f, 566, 672, 574, 790, 502, 880, 392, 906);
                break;
            case "umbrella":
                if (layer == 0) ArmFarSide(g, s);
                if (layer == 2)
                {
                    Umbrella(g);
                    Arm(g, s, 74, 58, "fist", 1, 1f, 566, 672, 552, 782, 450, 822, 330, 814);
                }
                break;
            case "sleeve":
                if (layer == 0) ArmNearSide(g, s);
                if (layer == 2) Arm(g, s, 72, 56, "pinch", 1, 1f, 238, 668, 212, 780, 186, 880, 150, 952);
                break;
            case "headphonesHold":
                if (layer == 2)
                {
                    Arm(g, s, 72, 56, null, 1, 1f, 238, 668, 244, 780, 302, 770, 330, 704);
                    Arm(g, s, 74, 58, null, 1, 1f, 566, 672, 574, 780, 512, 770, 482, 704);
                    Line(g, PHONES, 22f, 316, 650, 322, 598, 406, 570, 492, 598, 498, 650);
                    Line(g, PURPLE, 4f, 330, 628, 336, 604, 406, 582, 478, 604, 484, 628);
                    Cup(g, 318, 660, 10, 1.05f); Cup(g, 496, 660, -10, 1.05f);
                    Hand(g, 332, 694, -70, 1f, "fist", 1); Hand(g, 480, 694, -110, 1f, "fist", -1);
                }
                break;
            case "headphoneShare":
                if (layer == 2)
                {
                    var hs2 = HeadOn(g);
                    Line(g, PHONES, 22f, 556, 330, 520, 150, 400, 108, 270, 160, 214, 300, 200, 450);
                    Line(g, PURPLE, 4f, 540, 300, 510, 160, 400, 122, 284, 172, 232, 300, 216, 430);
                    Cup(g, 562, 356, 4, 1f);
                    Cup(g, 196, 480, -8, 1f);
                    g.Restore(hs2);
                    Arm(g, s, 72, 56, null, 1, 1f, 238, 668, 196, 760, 172, 650, HX(184), HY(530));
                    Hand(g, HX(188), HY(524), -80, 1f, "fist", 1);
                }
                break;
            case "ball":
                if (layer == 0)
                {
                    Ball(g, 416, 846, 196);
                    Arm(g, s, 72, 56, "fist", 1, 1f, 238, 668, 226, 772, 270, 860, 316, 870);
                    Arm(g, s, 74, 58, "fist", -1, 1f, 566, 672, 590, 772, 560, 862, 516, 870);
                }
                break;
            case "snack":
                if (layer == 0)
                {
                    PhoneBack(g, 436, 786, -14, 1f);
                    Arm(g, s, 74, 58, "fist", -1, 1f, 566, 672, 604, 782, 610, 874, 548, 868, 488, 842);
                }
                if (layer == 2)
                {
                    Arm(g, s, 72, 56, null, 1, 1f, 238, 668, 208, 782, 220, 880, 250, 790, 262, 708);
                    MilkCarton(g, 262, 630);
                    Hand(g, 264, 690, -82, 1f, "fist", 1);
                }
                break;
        }
    }

    // ── 손 (로컬 좌표: 손가락이 +X 방향) ──
    static void Hand(Graphics g, float x, float y, float ang, float sc, string type, int thumb)
    {
        var st = g.Save(); g.TranslateTransform(x, y); g.RotateTransform(ang); g.ScaleTransform(sc, sc * thumb);
        switch (type)
        {
            case "relax":
                {
                    var thumbP = new PB().M(6, -22).Q(22, -46, 44, -42).Q(56, -36, 46, -26).Q(30, -22, 20, -14).Z();
                    FS(g, thumbP, SKIN, SKIN_LINE, 2.5f);
                    var palm = new PB().M(-8, -24).Q(28, -30, 58, -22).Q(88, -14, 90, 4).Q(88, 24, 60, 28).Q(20, 32, -8, 22).Z();
                    Cel(g, palm, SKIN, SKIN_SH, -6, -6); S(g, palm, SKIN_LINE, 2.5f);
                    Line(g, SKIN_LINE, 2f, 58, -8, 74, -6, 86, -2);
                    Line(g, SKIN_LINE, 2f, 56, 8, 72, 10, 84, 12);
                    break;
                }
            case "fist":
                {
                    var palm = new PB().M(-8, -26).Q(34, -34, 56, -22).Q(72, -6, 68, 12).Q(62, 30, 36, 30).Q(8, 32, -8, 24).Z();
                    Cel(g, palm, SKIN, SKIN_SH, -6, -6); S(g, palm, SKIN_LINE, 2.5f);
                    Line(g, SKIN_LINE, 2f, 56, -16, 66, -10, 64, -2);
                    Line(g, SKIN_LINE, 2f, 64, 0, 70, 6, 66, 12);
                    Line(g, SKIN_LINE, 2f, 62, 14, 64, 22, 56, 26);
                    var th = new PB().M(14, -22).Q(42, -28, 52, -8).Q(46, 0, 34, -6).Q(22, -12, 12, -12).Z();
                    FS(g, th, SKIN, SKIN_LINE, 2.5f);
                    break;
                }
            case "open":
                {
                    float[][] fing = { new float[] { 36, -20, 92, -34 }, new float[] { 40, -6, 102, -12 }, new float[] { 40, 8, 100, 10 }, new float[] { 36, 20, 86, 32 } };
                    foreach (var f in fing) FS(g, Capsule(f[0], f[1], f[2], f[3], 15), SKIN, SKIN_LINE, 2.5f);
                    FS(g, Capsule(6, -24, 30, -58, 17), SKIN, SKIN_LINE, 2.5f);
                    var palm = new PB().M(-8, -26).Q(20, -32, 42, -28).Q(50, 0, 44, 26).Q(16, 32, -8, 24).Z();
                    Cel(g, palm, SKIN, SKIN_SH, -6, -6); S(g, palm, SKIN_LINE, 2.5f);
                    break;
                }
            case "peace":
                {
                    FS(g, Capsule(46, -16, 106, -40, 15), SKIN, SKIN_LINE, 2.5f);
                    FS(g, Capsule(50, -2, 112, -12, 15), SKIN, SKIN_LINE, 2.5f);
                    var palm = new PB().M(-8, -26).Q(34, -34, 56, -22).Q(72, -6, 68, 12).Q(62, 30, 36, 30).Q(8, 32, -8, 24).Z();
                    Cel(g, palm, SKIN, SKIN_SH, -6, -6); S(g, palm, SKIN_LINE, 2.5f);
                    Line(g, SKIN_LINE, 2f, 62, 10, 68, 16, 62, 24);
                    var th = new PB().M(14, -22).Q(42, -28, 52, -8).Q(46, 0, 34, -6).Q(22, -12, 12, -12).Z();
                    FS(g, th, SKIN, SKIN_LINE, 2.5f);
                    break;
                }
            case "pinch":
                {
                    var palm = new PB().M(-8, -24).Q(30, -30, 56, -20).Q(70, -4, 60, 16).Q(30, 30, -8, 22).Z();
                    Cel(g, palm, SKIN, SKIN_SH, -6, -6); S(g, palm, SKIN_LINE, 2.5f);
                    FS(g, Tube(15, 12, 44, -14, 76, -22, 94, -12), SKIN, SKIN_LINE, 2.5f);
                    FS(g, Tube(16, 12, 14, -22, 60, -40, 92, -20), SKIN, SKIN_LINE, 2.5f);
                    break;
                }
        }
        g.Restore(st);
    }

    // ── 소품 ──
    static void PhoneBack(Graphics g, float cx, float cy, float ang, float sc)
    {
        var st = g.Save(); g.TranslateTransform(cx, cy); g.RotateTransform(ang); g.ScaleTransform(sc, sc);
        FS(g, RRect(-44, -80, 88, 160, 18), MINT, LINE, 3f);
        F(g, RRect(-38, -74, 76, 148, 14), H("#86E0BF"));
        FS(g, RRect(-34, -70, 38, 44, 10), H("#2F3A3A"), LINE, 2f);
        FS(g, Ell(-23, -59, 15, 15), H("#111316"), H("#555B60"), 2f);
        FS(g, Ell(-23, -38, 15, 15), H("#111316"), H("#555B60"), 2f);
        F(g, Ell(-6, -50, 6, 6), H("#F7E7A0"));
        // 스티커 별
        Star(g, 14, 30, 14, H("#FFF4B8"));
        g.Restore(st);
    }

    static void Star(Graphics g, float cx, float cy, float r, Color c)
    {
        var p = new PB().M(cx, cy - r).Q(cx, cy, cx + r, cy).Q(cx, cy, cx, cy + r).Q(cx, cy, cx - r, cy).Q(cx, cy, cx, cy - r).Z();
        F(g, p, c);
    }

    static void Ball(Graphics g, float cx, float cy, float d)
    {
        var b = Ell(cx, cy, d, d);
        Cel(g, b, H("#EA7D32"), H("#C25A1C"), -18, -18);
        Clip(g, b, () =>
        {
            var seam = H("#4A2412");
            Line(g, seam, 4f, cx, cy - d / 2, cx + 6, cy, cx, cy + d / 2);
            Line(g, seam, 4f, cx - d / 2, cy, cx, cy - 6, cx + d / 2, cy);
            Line(g, seam, 4f, cx - d * 0.34f, cy - d * 0.38f, cx - d * 0.18f, cy, cx - d * 0.34f, cy + d * 0.38f);
            Line(g, seam, 4f, cx + d * 0.34f, cy - d * 0.38f, cx + d * 0.18f, cy, cx + d * 0.34f, cy + d * 0.38f);
            F(g, Ell(cx - d * 0.2f, cy - d * 0.24f, d * 0.3f, d * 0.18f), A(Color.White, 90));
        });
        S(g, b, H("#4A2412"), 3f);
    }

    static void MilkCarton(Graphics g, float cx, float cy)
    {
        var body = RRect(cx - 32, cy - 30, 64, 84, 6);
        Cel(g, body, H("#F9BCD0"), H("#E795B0"), -8, 0); S(g, body, LINE, 2.5f);
        FS(g, Poly(cx - 32, cy - 30, cx + 32, cy - 30, cx + 22, cy - 58, cx - 22, cy - 58), H("#FFFFFF"), LINE, 2.5f);
        FS(g, Rect(cx - 22, cy - 66, 44, 9), H("#FFFFFF"), LINE, 2f);
        var berry = new PB().M(cx - 13, cy + 2).Q(cx, cy - 8, cx + 13, cy + 2).Q(cx + 12, cy + 26, cx, cy + 32).Q(cx - 12, cy + 26, cx - 13, cy + 2).Z();
        FS(g, berry, H("#E5485F"), H("#9E2338"), 2f);
        F(g, Poly(cx - 10, cy - 2, cx, cy - 12, cx + 10, cy - 2, cx, cy + 4), H("#5BAE5A"));
        for (int i = 0; i < 5; i++) F(g, Ell(cx - 6 + (i % 3) * 6, cy + 10 + (i / 3) * 9, 3, 4), H("#FFE6A0"));
        Line(g, H("#FFFFFF"), 7f, cx + 12, cy - 60, cx + 26, cy - 104);
        Line(g, H("#F2788F"), 2f, cx + 14, cy - 66, cx + 24, cy - 98);
    }

    static void Umbrella(Graphics g)
    {
        // 접힌 우산: 손잡이는 손 위치(330,814), 끝은 왼쪽 아래
        Line(g, H("#7D8696"), 7f, 290, 838, 40, 1012);
        var canopy = Tube(46, 8, 270, 852, 190, 908, 96, 976);
        Cel(g, canopy, H("#3A4A78"), H("#26325A"), -6, -8); S(g, canopy, LINE, 2.5f);
        Line(g, A(H("#5D6EA6"), 255), 2.5f, 262, 846, 180, 906, 110, 960);
        Line(g, A(LINE, 140), 2f, 250, 870, 170, 920, 104, 972);
        FS(g, RRect(206, 870, 26, 34, 6), H("#C9A24D"), LINE, 2f); // 똑딱이 끈
        var handle = Tube(15, 15, 300, 812, 334, 806, 350, 826, 338, 846);
        FS(g, handle, H("#8A5A3A"), LINE, 2.5f);
    }

    // ── 얼굴 ──
    static GraphicsPath FacePath()
    {
        return new PB().M(266, 286).Q(254, 380, 272, 444).Q(298, 514, 348, 546).Q(368, 558, 392, 548)
            .Q(484, 514, 530, 448).Q(560, 380, 554, 296).Q(540, 150, 405, 148).Q(282, 150, 266, 286).Z();
    }

    static void Head(Graphics g, Spec s, Look k)
    {
        var ear = Ell(552, 364, 40, 76);
        FS(g, ear, SKIN, SKIN_LINE, 2.5f);
        Line(g, SKIN_SH, 4f, 552, 340, 564, 362, 554, 390);
        var face = FacePath();
        F(g, face, SKIN);
        var front = FrontMain(s);
        Clip(g, face, () =>
        {
            var st = g.Save(); g.TranslateTransform(4, 16); F(g, front, SKIN_SH); g.Restore(st);
            F(g, new PB().M(470, 520).Q(530, 450, 556, 320).L(580, 320).L(580, 560).Z(), A(SKIN_SH, 140));
        });
        S(g, face, SKIN_LINE, 2.5f);
        if (k.Blush)
        {
            F(g, Ell(298, 464, 60, 24), A(BLUSH, 130)); F(g, Ell(472, 464, 80, 28), A(BLUSH, 130));
            for (int i = 0; i < 3; i++)
            {
                Line(g, BLUSH_LINE, 2.5f, 284 + i * 11, 472, 292 + i * 11, 458);
                Line(g, BLUSH_LINE, 2.5f, 454 + i * 13, 474, 464 + i * 13, 458);
            }
        }
        Line(g, A(SKIN_LINE, 220), 2.5f, 338, 444, 334, 454, 326, 458);
        Eye(g, 314, 390, 60, 80, true, k);
        Eye(g, 450, 390, 86, 88, false, k);
        var ms = g.Save(); g.TranslateTransform(-16, 14); Mouth(g, k.Mouth); g.Restore(ms);
    }

    static void Eye(Graphics g, float cx, float cy, float w, float h, bool far, Look k)
    {
        float dir = far ? -1 : 1;
        var inner = new PointF(cx - dir * w / 2, cy + 4); var outer = new PointF(cx + dir * w / 2, cy - 4);
        string st = k.Eyes;
        if (curMale) h *= 0.88f;
        if (st == "wink") st = far ? "open" : "closedWink";
        if (st == "happy")
        {
            S(g, new PB().M(inner.X, cy + 8).Q(cx, cy - h * 0.55f, outer.X, cy + 6).O(), EYE_DARK, far ? 6f : 7f);
            Line(g, EYE_DARK, 4f, outer.X, cy + 6, outer.X + dir * 10, cy + 10);
            return;
        }
        if (st == "closedWink")
        {
            S(g, new PB().M(inner.X, cy + 2).Q(cx, cy + h * 0.32f, outer.X, cy - 2).O(), EYE_DARK, 7f);
            Line(g, EYE_DARK, 4f, outer.X, cy - 2, outer.X + dir * 12, cy - 10);
            Line(g, EYE_DARK, 3f, cx + dir * w * 0.2f, cy + h * 0.14f, cx + dir * w * 0.26f, cy + h * 0.3f);
            return;
        }
        float topY = cy - h * 0.78f;
        if (st == "half") topY = cy - h * 0.1f;
        if (st == "down") topY = cy - h * 0.3f;
        if (st == "glare") topY = cy - h * 0.42f;
        var topC = new PointF(cx + dir * w * (st == "glare" ? 0.25f : 0.1f), topY);
        var white = new PB().M(inner.X, inner.Y).Q(topC.X, topC.Y, outer.X, outer.Y).Q(cx, cy + h * 0.62f, inner.X, inner.Y).Z();
        F(g, white, Color.White);
        float ix = cx + k.LookX * w * 0.2f + dir * w * 0.02f, iy = cy + 6 + k.LookY * h * 0.12f;
        float iw = w * 0.66f, ih = h * 0.92f;
        var tc = topC; var inn = inner; var outp = outer;
        Clip(g, white, () =>
        {
            var iris = Ell(ix, iy, iw, ih);
            F(g, iris, IRIS);
            Clip(g, iris, () => F(g, Ell(ix, iy + ih * 0.36f, iw * 1.1f, ih * 0.6f), IRIS_LT));
            F(g, Ell(ix, iy + 2, iw * 0.46f, ih * 0.54f), PUPIL);
            S(g, iris, PUPIL, 2f);
            F(g, new PB().M(inn.X, inn.Y).Q(tc.X, tc.Y, outp.X, outp.Y).L(outp.X, outp.Y + 16).Q(tc.X, tc.Y + 24, inn.X, inn.Y + 14).Z(), A(EYE_DARK, 120));
            F(g, Ell(ix - w * 0.12f, iy - ih * 0.18f, iw * 0.36f, ih * 0.26f), Color.White);
            F(g, Ell(ix + w * 0.13f, iy + ih * 0.22f, 8, 8), A(Color.White, 230));
            if (k.Sparkle) { Star(g, ix + w * 0.1f, iy - ih * 0.02f, 11, Color.White); Star(g, ix - w * 0.16f, iy + ih * 0.2f, 6, Color.White); }
            if (k.Tear) F(g, new PB().M(inn.X, cy + h * 0.2f).Q(cx, cy + h * 0.46f, outp.X, cy + h * 0.2f).Q(cx, cy + h * 0.6f, inn.X, cy + h * 0.2f).Z(), A(H("#CFEFFF"), 200));
        });
        S(g, new PB().M(inner.X, inner.Y).Q(topC.X, topC.Y, outer.X, outer.Y).O(), EYE_DARK, far ? 6f : 7.5f);
        if (!curMale) Line(g, EYE_DARK, 5f, outer.X, outer.Y, outer.X + dir * 12, outer.Y - 9);
        S(g, new PB().M(cx + dir * w * 0.02f, cy + h * 0.5f).Q(cx + dir * w * 0.34f, cy + h * 0.44f, outer.X - dir * 2, outer.Y + 12).O(), A(EYE_DARK, 200), 2.5f);
        if (st == "open" || st == "glare")
            S(g, new PB().M(cx - dir * w * 0.06f, cy - h * 0.5f).Q(cx + dir * w * 0.3f, cy - h * 0.56f, outer.X + dir * 4, outer.Y - 14).O(), A(EYE_DARK, 150), 2f);
        if (st == "half" || st == "down")
            S(g, new PB().M(cx - dir * w * 0.3f, topY - 6).Q(cx, topY - 14, outer.X + dir * 2, outer.Y - 12).O(), A(EYE_DARK, 140), 2f);
    }

    static void Mouth(Graphics g, string m)
    {
        switch (m)
        {
            case "line": Line(g, MOUTH_LINE, 3f, 358, 480, 372, 483, 388, 478); break;
            case "smile": Line(g, MOUTH_LINE, 3f, 352, 474, 372, 488, 394, 472); break;
            case "smug": Line(g, MOUTH_LINE, 3f, 356, 482, 380, 484, 398, 468); Line(g, MOUTH_LINE, 2.5f, 398, 468, 402, 474); break;
            case "frown": Line(g, MOUTH_LINE, 3f, 356, 488, 372, 478, 390, 486); break;
            case "pout": Line(g, MOUTH_LINE, 3f, 360, 486, 372, 478, 386, 486); Line(g, A(MOUTH_LINE, 120), 2f, 364, 492, 382, 492); break;
            case "wavy": Line(g, MOUTH_LINE, 3f, 352, 482, 358, 476, 364, 482, 370, 488, 376, 482, 382, 476, 390, 482); break;
            case "o":
                FS(g, Ell(370, 480, 16, 18), MOUTH, MOUTH_LINE, 2.5f); break;
            case "open":
                {
                    var p = new PB().M(350, 470).Q(372, 476, 396, 468).Q(392, 502, 372, 505).Q(352, 502, 350, 470).Z();
                    F(g, p, MOUTH);
                    Clip(g, p, () => F(g, Ell(366, 498, 34, 20), TONGUE));
                    S(g, p, MOUTH_LINE, 2.5f);
                    break;
                }
            case "grin":
                {
                    var p = new PB().M(342, 466).Q(372, 474, 404, 462).Q(400, 508, 372, 510).Q(346, 506, 342, 466).Z();
                    F(g, p, MOUTH);
                    Clip(g, p, () => { F(g, Rect(330, 460, 90, 16), Color.White); F(g, Ell(370, 504, 40, 22), TONGUE); });
                    S(g, p, MOUTH_LINE, 2.5f);
                    break;
                }
        }
    }

    static void Brows(Graphics g, Spec s, Look k)
    {
        float fiy = 322, foy = 334, niy = 316, noy = 326, arch = 10;
        switch (k.Brows)
        {
            case "angry": fiy += 14; niy += 14; foy -= 6; noy -= 6; arch = 2; break;
            case "worried": fiy -= 10; niy -= 10; foy += 6; noy += 6; arch = 4; break;
            case "raised": fiy -= 10; niy -= 10; foy -= 10; noy -= 10; arch = 12; break;
            case "flat": arch = 2; foy -= 4; noy -= 4; break;
            case "flatUp": arch = 3; fiy -= 4; niy -= 4; foy -= 10; noy -= 10; break;
        }
        Color c = BaseHair(s) == "seungju" ? s.BrowC : A(s.BrowC, 215);
        Brow(g, 344, fiy, 282, foy, arch, c);
        Brow(g, 414, niy, 502, noy, arch, c);
    }
    static void Brow(Graphics g, float ix, float iy, float ox, float oy, float arch, Color c)
    {
        if (curMale) F(g, Tube(11f, 5f, ix, iy, (ix + ox) / 2, (iy + oy) / 2 - arch * 0.6f, ox, oy), c);
        else F(g, Tube(7.5f, 2.5f, ix, iy, (ix + ox) / 2, (iy + oy) / 2 - arch, ox, oy), c);
    }

    // ── 앞머리 ──
    static GraphicsPath FrontMain(Spec s)
    {
        switch (BaseHair(s))
        {
            case "seungju":
                return new PB().M(236, 330).Q(210, 150, 408, 118).Q(602, 136, 586, 330).Q(566, 276, 496, 214)
                    .Q(440, 180, 384, 170).Q(318, 186, 284, 236).Q(258, 282, 262, 330).Z();
            case "seunghyun":
                return new PB().M(232, 400).Q(212, 146, 408, 118).Q(606, 138, 588, 400).L(574, 332)
                    .Crv(0.4f, 548, 316, 530, 266, 500, 328, 478, 264, 446, 320, 424, 260, 392, 326, 366, 264, 334, 316, 312, 264, 286, 322, 264, 346)
                    .L(240, 400).Z();
            case "seoyeon":
                return new PB().M(232, 400).Q(212, 146, 408, 118).Q(606, 138, 588, 400).L(574, 332)
                    .Crv(0.45f, 550, 312, 528, 270, 498, 322, 472, 268, 440, 318, 414, 266, 386, 322, 358, 270, 330, 314, 306, 270, 284, 318, 264, 344)
                    .L(240, 400).Z();
            case "giju":
                return new PB().M(234, 380).Q(214, 150, 408, 118).Q(602, 138, 586, 380).L(560, 330).Q(540, 236, 470, 214)
                    .Q(400, 202, 330, 218).Q(268, 240, 258, 330).L(258, 380).Z();
            default:
                return new PB().M(244, 380).Q(226, 146, 408, 120).Q(562, 126, 592, 190).Q(572, 240, 558, 296)
                    .L(542, 306).Q(406, 324, 266, 306).L(256, 330).L(250, 380).Z();
        }
    }

    static void FrontHair(Graphics g, Spec s)
    {
        var main = FrontMain(s);
        var locks = new List<GraphicsPath>();
        switch (s.Hair)
        {
            case "seungju":
                locks.Add(new PB().M(262, 214).Q(230, 300, 228, 440).Q(222, 620, 236, 780).L(250, 808).L(262, 778).Q(274, 620, 272, 450).Q(270, 330, 296, 226).Z());
                locks.Add(new PB().M(536, 206).Q(590, 320, 592, 470).Q(602, 650, 588, 832).L(568, 860).L(556, 828).Q(566, 650, 556, 480).Q(550, 340, 512, 222).Z());
                break;
            case "seunghyun":
                locks.Add(new PB().M(262, 330).Q(224, 410, 236, 500).Q(250, 580, 222, 640).Q(212, 670, 236, 678).Q(264, 650, 262, 590).Q(268, 520, 266, 450).Q(266, 380, 282, 336).Z());
                locks.Add(new PB().M(552, 320).Q(594, 410, 584, 500).Q(572, 580, 600, 640).Q(608, 672, 584, 678).Q(558, 650, 558, 590).Q(552, 520, 552, 450).Q(550, 380, 534, 330).Z());
                break;
            case "seoyeon": // 포니테일로 묶고 남은 옆머리 (턱 아래까지, 살짝 웨이브)
                locks.Add(new PB().M(262, 330).Q(226, 420, 240, 500).Q(252, 560, 232, 610).Q(224, 640, 246, 646).Q(266, 616, 264, 560).Q(270, 470, 282, 336).Z());
                locks.Add(new PB().M(552, 320).Q(590, 420, 578, 500).Q(568, 560, 590, 610).Q(598, 640, 576, 646).Q(556, 616, 558, 560).Q(552, 470, 534, 330).Z());
                break;
            case "giju":
                locks.Add(new PB().M(258, 296).Q(228, 420, 240, 550).L(298, 558).Q(282, 440, 288, 316).Z());
                locks.Add(new PB().M(548, 292).Q(590, 420, 574, 554).L(520, 562).Q(532, 440, 520, 312).Z());
                break;
            case "m_seungju":
                locks.Add(new PB().M(262, 214).Q(232, 300, 234, 400).Q(238, 456, 256, 486).L(274, 474).Q(272, 380, 296, 226).Z());
                locks.Add(new PB().M(536, 206).Q(586, 300, 584, 400).Q(578, 458, 556, 488).L(540, 474).Q(550, 360, 512, 222).Z());
                break;
            case "m_seunghyun":
                locks.Add(new PB().M(262, 330).Q(234, 390, 246, 450).Q(252, 480, 240, 500).Q(262, 496, 270, 470).Q(268, 400, 282, 336).Z());
                locks.Add(new PB().M(552, 320).Q(586, 390, 574, 450).Q(568, 480, 582, 500).Q(560, 496, 550, 470).Q(550, 400, 534, 330).Z());
                break;
            case "m_seoyeon":
                locks.Add(new PB().M(262, 330).Q(222, 420, 238, 500).Q(246, 540, 230, 566).Q(256, 570, 264, 530).Q(270, 440, 282, 336).Z());
                locks.Add(new PB().M(552, 320).Q(594, 420, 580, 500).Q(572, 540, 590, 566).Q(562, 570, 556, 530).Q(552, 440, 534, 330).Z());
                break;
            case "m_giju":
                locks.Add(new PB().M(258, 296).Q(236, 380, 250, 476).L(286, 482).Q(282, 400, 288, 316).Z());
                locks.Add(new PB().M(548, 292).Q(582, 380, 568, 476).L(530, 482).Q(536, 400, 520, 312).Z());
                break;
            default:
                locks.Add(new PB().M(264, 304).Q(248, 390, 256, 478).L(274, 472).Q(272, 390, 282, 306).Z());
                locks.Add(new PB().M(540, 304).Q(556, 388, 548, 464).L(534, 466).Q(538, 390, 526, 306).Z());
                break;
        }
        foreach (var lk in locks) HairFill(g, s, lk, -10, -4);
        HairFill(g, s, main, -16, -10);
        // 천사링(윤기)
        Clip(g, main, () =>
        {
            float y = BaseHair(s) == "seungju" ? -58 : 0; // 승주는 이마가 보여서 윤기를 정수리 쪽에
            float bw = BaseHair(s) == "seungju" ? 12 : 22;
            F(g, new PB().M(270, 228 + y).Q(404, 168 + y, 546, 232 + y).L(542, 232 + y + bw).Q(404, 170 + y + bw, 274, 228 + y + bw).Z(), A(s.HairLt, 210));
            F(g, new PB().M(300, 214 + y).L(318, 204 + y).L(330, 246 + y).L(312, 250 + y).Z(), s.HairC);
            F(g, new PB().M(452, 204 + y).L(470, 208 + y).L(472, 244 + y).L(454, 242 + y).Z(), s.HairC);
            if (BaseHair(s) == "giju") // 윤기 나는 생머리 (귀티)
            {
                var gloss = A(H("#9A94B8"), 200);
                foreach (float gx in new float[] { 296, 340, 384, 428, 472, 516 }) Line(g, gloss, 3f, gx, 150 + Math.Abs(gx - 406) * 0.3f, gx + 4, 196 + Math.Abs(gx - 406) * 0.3f);
            }
        });
        // 머리결
        Color sl = A(s.HairLine, 150);
        switch (BaseHair(s))
        {
            case "seungju":
                Line(g, s.HairLine, 2.5f, 384, 170, 396, 124);
                Line(g, sl, 2f, 376, 176, 318, 210, 282, 290);
                Line(g, sl, 2f, 394, 176, 470, 206, 540, 280);
                if (!s.Male)
                {
                    Line(g, sl, 2f, 246, 420, 240, 600, 248, 760);
                    Line(g, sl, 2f, 574, 420, 580, 640, 574, 820);
                    Line(g, sl, 2f, 560, 520, 566, 700);
                }
                else Line(g, sl, 2f, 566, 320, 570, 440);
                break;
            case "seunghyun":
            case "seoyeon":
                Line(g, sl, 2f, 400, 150, 380, 230, 392, 320);
                Line(g, sl, 2f, 440, 160, 456, 240, 446, 312);
                Line(g, sl, 2f, 350, 160, 330, 240, 334, 308);
                Line(g, sl, 2f, 250, 420, 252, s.Male ? 480 : 560);
                Line(g, sl, 2f, 570, 420, 568, s.Male ? 480 : 560);
                break;
            case "giju":
                for (int i = 0; i < 8; i++)
                {
                    float x = 288 + i * 33, tipY = 310 + (i % 3) * 7;
                    var strand = new PB().M(x - 11, 212).Q(x - 4, 268, x - 9 + (i % 2) * 5, tipY).Q(x + 5, 266, x + 11, 212).Z();
                    HairFill(g, s, strand, -4, -4);
                }
                Line(g, sl, 2f, 266, 400, 270, s.Male ? 470 : 540);
                Line(g, sl, 2f, 556, 400, 548, s.Male ? 470 : 544);
                break;
            default:
                for (int x = 300; x <= 512; x += 36) Line(g, sl, 2f, x, 262, x + 2, 312);
                Line(g, sl, 2f, 300, 186, 440, 150, 580, 186);
                Line(g, sl, 2f, 290, 240, 450, 200, 570, 232);
                var tie = new PB().M(574, 166).L(598, 158).L(606, 194).L(582, 202).Z();
                FS(g, tie, H("#3A3846"), LINE, 2.5f);
                break;
        }
    }

    static void Effects(Graphics g, Look k)
    {
        if (k.Anger)
        {
            var c = H("#E0485E"); float x = 560, y = 196;
            using (var pen = NewPen(c, 6))
            {
                g.DrawPath(pen, new PB().M(x - 5, y - 26).Q(x - 5, y - 5, x - 26, y - 5).O());
                g.DrawPath(pen, new PB().M(x + 5, y - 26).Q(x + 5, y - 5, x + 26, y - 5).O());
                g.DrawPath(pen, new PB().M(x - 5, y + 26).Q(x - 5, y + 5, x - 26, y + 5).O());
                g.DrawPath(pen, new PB().M(x + 5, y + 26).Q(x + 5, y + 5, x + 26, y + 5).O());
            }
        }
        if (k.Sweat)
        {
            float x = 566, y = 262;
            var d = new PB().M(x, y - 26).Q(x + 18, y, x + 13, y + 11).Q(x, y + 24, x - 13, y + 11).Q(x - 18, y, x, y - 26).Z();
            FS(g, d, H("#C4E6F8"), H("#6FA6CC"), 2.5f);
            F(g, Ell(x - 4, y + 4, 6, 10), A(Color.White, 220));
        }
    }

    // ───────────────────────── 배경 도구 ─────────────────────────
    static Random rnd = new Random(7);

    static void Bands(Graphics g, float x, float y, float w, float h, params string[] hex)
    {
        int steps = 8; int n = hex.Length - 1; float bh = h / (n * steps);
        for (int i = 0; i < n; i++)
            for (int j = 0; j < steps; j++)
            {
                var c = Mix(H(hex[i]), H(hex[i + 1]), j / (float)steps);
                using (var b = new SolidBrush(c)) g.FillRectangle(b, x, y + (i * steps + j) * bh, w, bh + 1);
            }
    }
    static void Glow(Graphics g, float x, float y, float r, Color c)
    {
        for (int i = 4; i >= 1; i--) F(g, Ell(x, y, r * i * 0.9f, r * i * 0.9f), A(c, 26));
        F(g, Ell(x, y, r * 0.8f, r * 0.8f), c);
    }
    static void Fence(Graphics g, float y0, float y1, Color c)
    {
        var area = Rect(0, y0, 1920, y1 - y0);
        Clip(g, area, () =>
        {
            for (float x = -1200; x < 1920; x += 30) { Line(g, A(c, 150), 2f, x, y0, x + (y1 - y0), y1); Line(g, A(c, 150), 2f, x + (y1 - y0), y0, x, y1); }
        });
        for (float x = 60; x < 1920; x += 260) F(g, Rect(x, y0 - 10, 12, y1 - y0 + 10), c);
        F(g, Rect(0, y0 - 16, 1920, 12), c);
        F(g, Rect(0, y1 - 6, 1920, 8), c);
    }
    static void StringLights(Graphics g, float x0, float y0, float x1, float y1, float sag, int seed)
    {
        var r = new Random(seed);
        string[] cols = { "#FFD27A", "#FF9FB4", "#9FD8FF", "#C6F59A", "#E0B6FF" };
        var pts = new List<PointF>();
        for (int i = 0; i <= 40; i++) { float t = i / 40f; pts.Add(new PointF(x0 + (x1 - x0) * t, y0 + (y1 - y0) * t + sag * 4 * t * (1 - t))); }
        using (var pen = NewPen(H("#2A2238"), 3f)) g.DrawCurve(pen, pts.ToArray());
        for (int i = 1; i < 40; i += 2) Glow(g, pts[i].X, pts[i].Y + 10, 14, H(cols[r.Next(cols.Length)]));
    }
    static void Stars(Graphics g, int n, float maxY, int seed)
    {
        var r = new Random(seed);
        for (int i = 0; i < n; i++)
        {
            float x = (float)r.NextDouble() * 1920, y = (float)r.NextDouble() * maxY, s = 2 + (float)r.NextDouble() * 3;
            F(g, Ell(x, y, s, s), A(Color.White, 120 + r.Next(130)));
        }
        for (int i = 0; i < 8; i++) Star(g, (float)r.NextDouble() * 1920, (float)r.NextDouble() * maxY * 0.8f, 9, A(H("#FFF6D8"), 220));
    }
    static void Burst(Graphics g, float cx, float cy, float rad, Color c)
    {
        for (int i = 0; i < 28; i++)
        {
            double a = i * Math.PI * 2 / 28;
            float x0 = cx + (float)Math.Cos(a) * rad * 0.25f, y0 = cy + (float)Math.Sin(a) * rad * 0.25f;
            float x1 = cx + (float)Math.Cos(a) * rad, y1 = cy + (float)Math.Sin(a) * rad;
            Line(g, A(c, 200), 3f, x0, y0, x1, y1);
            F(g, Ell(x1, y1, 7, 7), c);
        }
        Glow(g, cx, cy, 14, A(Color.White, 230));
    }
    static void Particles(Graphics g, int n, Color c, int seed)
    {
        var r = new Random(seed);
        for (int i = 0; i < n; i++) { float s = 4 + (float)r.NextDouble() * 10; F(g, Ell((float)r.NextDouble() * 1920, (float)r.NextDouble() * 1080, s, s), A(c, 60 + r.Next(110))); }
    }
    static void Caption(Graphics g, string text)
    {
        if (maleSet.Count > 0) text += " (남)";
        using (var f = new Font("Malgun Gothic", 22, FontStyle.Bold, GraphicsUnit.Pixel))
        {
            var sz = g.MeasureString(text, f);
            FS(g, RRect(24, 22, sz.Width + 32, 44, 22), A(Color.White, 225), H("#E0628F"), 2f);
            using (var b = new SolidBrush(H("#4A2C2A"))) g.DrawString(text, f, b, 40, 32);
        }
    }

    /// <summary>캐릭터를 CG에 배치. cx = 얼굴 중심 x, top = 캔버스 y=0 위치, mirror = 오른쪽 보기</summary>
    static void Place(Graphics g, Spec s, Look k, float cx, float top, float scale, bool mirror)
    {
        var st = g.Save();
        g.TranslateTransform(cx, top); g.ScaleTransform(mirror ? -scale : scale, scale); g.TranslateTransform(-400, 0);
        DrawChar(g, s, k);
        g.Restore(st);
    }

    // ── 배경: 옥상 ──
    static void Rooftop(Graphics g, bool night)
    {
        if (night)
        {
            Bands(g, 0, 0, 1920, 700, "#141A3A", "#1E2550", "#2C3266", "#4A447E");
            Stars(g, 140, 520, 3);
            F(g, Ell(1640, 150, 90, 90), H("#FFF3C8"));
            F(g, Ell(1666, 136, 80, 80), H("#1A2046"));
        }
        else
        {
            Bands(g, 0, 0, 1920, 700, "#5E5AA0", "#9A78B8", "#E58FA3", "#F6B07F", "#FBD58F");
            Glow(g, 1520, 640, 90, H("#FFE8A8"));
            foreach (var cl in new float[][] { new float[] { 300, 200, 360 }, new float[] { 980, 130, 300 }, new float[] { 1420, 260, 380 } })
            {
                var cloud = Smooth(true, 0.6f, cl[0], cl[1], cl[0] + cl[2] * 0.3f, cl[1] - 40, cl[0] + cl[2] * 0.7f, cl[1] - 30, cl[0] + cl[2], cl[1] + 6, cl[0] + cl[2] * 0.5f, cl[1] + 30);
                F(g, cloud, A(H("#F7B9C4"), 220));
                F(g, Rect(cl[0], cl[1] + 6, cl[2], 6), A(H("#FFE2B8"), 160));
            }
        }
        // 마을 + 축제
        var r = new Random(11);
        Color bld = night ? H("#262A52") : H("#5B4B7C");
        float x = 0;
        while (x < 1920)
        {
            float w = 60 + r.Next(120), h = 60 + r.Next(170);
            F(g, Rect(x, 700 - h, w, h + 40), bld);
            for (float wy = 700 - h + 14; wy < 700; wy += 26)
                for (float wx = x + 10; wx < x + w - 12; wx += 22)
                    if (r.Next(3) == 0) F(g, Rect(wx, wy, 10, 12), night ? H("#FFD98A") : A(H("#FFE6B0"), 170));
            x += w + 4;
        }
        for (int i = 0; i < 18; i++) Glow(g, 120 + i * 100, 640 + (float)Math.Sin(i) * 10, 9, night ? H("#FF8A6A") : H("#FFC48A"));
        // 옥상 난간 벽 + 바닥 + 펜스
        F(g, Rect(0, 730, 1920, 140), night ? H("#45476C") : H("#CBC2D6"));
        F(g, Rect(0, 730, 1920, 10), night ? H("#5A5D86") : H("#DCD5E4"));
        F(g, Rect(0, 860, 1920, 220), night ? H("#3A3B5A") : H("#B4A9BE"));
        for (float ty = 900; ty < 1080; ty += 46) F(g, Rect(0, ty, 1920, 3), night ? H("#2C2D48") : H("#9C90A8"));
        Fence(g, 520, 860, night ? H("#5A5F86") : H("#7E7898"));
        // 물탱크
        var tank = RRect(70, 380, 210, 260, 30);
        FS(g, tank, night ? H("#565C82") : H("#C8C3D6"), LINE, 3f);
        F(g, Rect(70, 450, 210, 10), A(LINE, 80));
        F(g, Rect(100, 640, 14, 220), LINE); F(g, Rect(236, 640, 14, 220), LINE);
    }

    // ───────────────────────── CG 목록 ─────────────────────────
    static Look CG(string eyes, string brows, string mouth, string arms, bool blush)
    {
        var k = L(eyes, brows, mouth, arms); k.Blush = blush; return k;
    }

    static Dictionary<string, Action<Graphics>> CGs()
    {
        var d = new Dictionary<string, Action<Graphics>>();

        d["승주_우산"] = g =>
        {
            // 학교 현관, 겨울 소나기
            F(g, Rect(0, 0, 1920, 1080), H("#C7CCD9"));
            F(g, Rect(0, 0, 1920, 120), H("#AEB4C3"));
            FS(g, RRect(560, 40, 520, 36, 12), H("#F4F7FC"), H("#8E95A6"), 3f);
            // 신발장
            for (int row = 0; row < 5; row++)
                for (int col = 0; col < 6; col++)
                {
                    float x = 20 + col * 92, y = 200 + row * 112;
                    FS(g, Rect(x, y, 86, 104), H("#B5BCCB"), H("#848B9C"), 3f);
                    F(g, Rect(x + 30, y + 46, 26, 8), H("#6D7484"));
                    FS(g, Rect(x + 18, y + 12, 50, 16), H("#F3F4F8"), H("#9AA0AE"), 1.5f);
                }
            // 유리문 + 비
            F(g, Rect(1180, 160, 740, 660), H("#596C8C"));
            for (int i = 0; i < 9; i++) { float tx = 1200 + i * 82; var tree = Smooth(true, 0.6f, tx, 560, tx + 40, 470, tx + 80, 560, tx + 40, 600); F(g, tree, H("#4B5E7D")); }
            F(g, Rect(1180, 600, 740, 220), H("#6A7B98"));
            var rr = new Random(5);
            Clip(g, Rect(1180, 160, 740, 660), () =>
            {
                for (int i = 0; i < 260; i++) { float x = 1160 + (float)rr.NextDouble() * 780, y = 140 + (float)rr.NextDouble() * 700; Line(g, A(H("#E2EAF7"), 140), 2f, x, y, x - 10, y + 40); }
            });
            for (int i = 0; i < 4; i++) F(g, Rect(1180 + i * 246, 160, 14, 660), H("#4E556A"));
            F(g, Rect(1180, 150, 740, 16), H("#4E556A")); F(g, Rect(1180, 470, 740, 10), H("#4E556A"));
            // 바닥 + 반사
            F(g, Rect(0, 820, 1920, 260), H("#8A91A2"));
            for (float y = 860; y < 1080; y += 50) F(g, Rect(0, y, 1920, 3), H("#767D8F"));
            for (int i = 0; i < 6; i++) F(g, Rect(1220 + i * 110, 830, 30, 240), A(H("#E2EAF7"), 50));
            // 우산꽂이
            FS(g, RRect(1060, 640, 100, 200, 10), H("#7C8394"), LINE, 3f);
            Line(g, H("#3A4A78"), 10f, 1080, 640, 1060, 470); Line(g, H("#A04A5C"), 10f, 1110, 640, 1124, 480); Line(g, H("#4E7A5A"), 10f, 1140, 640, 1170, 500);
            var k = CG("half", "flat", "line", "umbrella", true); k.LookX = 0.9f; k.LookY = 0.2f;
            Place(g, Find("승주"), k, 900, -20, 1.0f, false);
            Caption(g, "임시 CG · 승주_우산");
        };

        d["승주_고백"] = g =>
        {
            Rooftop(g, false);
            // 보는 사람(나)의 소매
            Hand(g, 852, 976, -8, 1.25f, "relax", -1);
            var sleeve = Tube(150, 120, -80, 1180, 260, 1080, 600, 1010, 840, 975);
            Cel(g, sleeve, H("#2F3B60"), H("#212A48"), -20, -16); S(g, sleeve, LINE, 3f);
            var cs = g.Save(); g.TranslateTransform(836, 975); g.RotateTransform(-8);
            FS(g, RRect(-14, -66, 24, 132, 8), H("#48568C"), LINE, 3f); FS(g, RRect(8, -56, 12, 112, 5), SHIRT, LINE, 2f);
            g.Restore(cs);
            var k = CG("open", "worried", "pout", "sleeve", true); k.LookX = 0.8f; k.LookY = 0.4f;
            Place(g, Find("승주"), k, 1150, -20, 1.0f, false);
            Particles(g, 40, H("#FFE7B8"), 9);
            Caption(g, "임시 CG · 승주_고백");
        };

        d["승현_엔딩"] = g =>
        {
            Rooftop(g, true);
            StringLights(g, -40, 120, 1960, 160, 140, 1);
            var k = CG("open", "raised", "grin", "reach", true); k.Sparkle = true; k.LookX = -0.5f;
            Place(g, Find("승현"), k, 1050, -20, 1.0f, false);
            Particles(g, 40, H("#FFE7B8"), 2);
            Caption(g, "임시 CG · 승현_엔딩");
        };

        d["서연_엔딩"] = g =>
        {
            Rooftop(g, true);
            StringLights(g, -40, 140, 1960, 110, 150, 3);
            var k = CG("happy", "raised", "smile", "blanketOpen", true);
            Place(g, Find("서연"), k, 960, 0, 0.98f, false);
            Particles(g, 40, H("#FFE7B8"), 3);
            Caption(g, "임시 CG · 서연_엔딩");
        };

        d["기주_엔딩"] = g =>
        {
            Rooftop(g, true);
            StringLights(g, -40, 100, 1960, 140, 120, 4);
            var k = CG("open", "worried", "smile", "headphonesHold", true); k.LookX = -0.6f;
            Place(g, Find("기주"), k, 980, -110, 1.14f, false);
            Particles(g, 50, H("#E0B6FF"), 4);
            Caption(g, "임시 CG · 기주_엔딩");
        };

        d["수민_엔딩"] = g =>
        {
            Rooftop(g, true);
            StringLights(g, -40, 130, 1960, 120, 140, 5);
            var k = CG("open", "flat", "line", "phoneEar", true); k.LookX = -0.6f;
            Place(g, Find("수민"), k, 1000, -20, 1.0f, false);
            Particles(g, 40, H("#B8FFE4"), 5);
            Caption(g, "임시 CG · 수민_엔딩");
        };

        d["하렘_엔딩"] = g =>
        {
            Rooftop(g, true);
            Burst(g, 360, 200, 150, H("#FF9FB4")); Burst(g, 1540, 170, 170, H("#FFD27A")); Burst(g, 960, 120, 120, H("#9FD8FF"));
            StringLights(g, -40, 260, 1960, 280, 100, 6);
            var a = CG("happy", "raised", "grin", "peace", true);
            var b = CG("open", "raised", "o", "phone", true); b.Sparkle = true;
            var c = CG("happy", "raised", "open", "carrot", true);
            var dd = CG("wink", "raised", "grin", "sides", true);
            Place(g, Find("수민"), b, 780, 160, 0.78f, true);
            Place(g, Find("서연"), c, 1160, 160, 0.78f, false);
            Place(g, Find("승현"), a, 440, 220, 0.84f, true);
            Place(g, Find("기주"), dd, 1500, 220, 0.84f, false);
            Particles(g, 60, H("#FFE7B8"), 6);
            Caption(g, "임시 CG · 하렘_엔딩");
        };

        d["승현_농구"] = g =>
        {
            Bands(g, 0, 0, 1920, 640, "#8FC6EE", "#B9DDF3", "#F3E6C2", "#F7D7A8");
            foreach (var cl in new float[][] { new float[] { 200, 150, 340 }, new float[] { 1200, 100, 420 } })
                F(g, Smooth(true, 0.6f, cl[0], cl[1], cl[0] + cl[2] * 0.3f, cl[1] - 50, cl[0] + cl[2] * 0.7f, cl[1] - 36, cl[0] + cl[2], cl[1] + 8, cl[0] + cl[2] * 0.5f, cl[1] + 34), A(Color.White, 230));
            // 학교 건물
            FS(g, Rect(0, 300, 760, 360), H("#E6DCCB"), H("#B3A792"), 3f);
            for (int r2 = 0; r2 < 4; r2++) for (int c2 = 0; c2 < 9; c2++) FS(g, Rect(30 + c2 * 82, 330 + r2 * 78, 56, 50), H("#A9C8DD"), H("#8B98A6"), 2f);
            // 나무
            var tr = new Random(8);
            for (int i = 0; i < 14; i++) { float tx = 700 + i * 90, ty = 520 + tr.Next(40); F(g, Ell(tx, ty, 170, 150), i % 2 == 0 ? H("#6FA65C") : H("#5E9450")); }
            Fence(g, 380, 760, H("#7D8D9B"));
            // 코트
            F(g, Rect(0, 760, 1920, 320), H("#C9684B"));
            F(g, Poly(380, 760, 1540, 760, 1920, 1080, 0, 1080), H("#B95D43"));
            using (var pen = NewPen(Color.White, 6))
            {
                g.DrawLine(pen, 0, 790, 1920, 790);
                g.DrawPolygon(pen, Pts(new float[] { 1280, 790, 1640, 790, 1760, 980, 1180, 980 }));
                g.DrawEllipse(pen, 1240, 930, 440, 110);
                g.DrawArc(pen, 820, 600, 1200, 600, 100, 160);
            }
            // 골대
            F(g, Rect(1700, 230, 26, 580), H("#5D6B78")); F(g, Rect(1600, 300, 120, 20), H("#5D6B78"));
            FS(g, Rect(1420, 180, 300, 190), A(Color.White, 235), H("#5D6B78"), 5f);
            using (var pen = NewPen(H("#E0485E"), 6)) g.DrawRectangle(pen, 1520, 270, 100, 76);
            using (var pen = NewPen(H("#E8782E"), 8)) g.DrawEllipse(pen, 1500, 360, 140, 30);
            for (int i = 0; i <= 6; i++) Line(g, A(Color.White, 220), 2.5f, 1504 + i * 22, 376, 1530 + i * 14, 460);
            for (int j = 0; j < 3; j++) Line(g, A(Color.White, 200), 2f, 1510 + j * 6, 400 + j * 26, 1630 - j * 6, 400 + j * 26);
            Ball(g, 220, 1000, 110);
            var k = CG("happy", "raised", "grin", "ball", true); k.Sweat = true;
            Place(g, Find("승현"), k, 900, -20, 1.0f, false);
            Particles(g, 30, H("#FFF2C8"), 12);
            Caption(g, "임시 CG · 승현_농구");
        };

        d["서연_도서관"] = g =>
        {
            F(g, Rect(0, 0, 1920, 1080), H("#EFE1CC"));
            // 아치형 창문 + 햇빛
            var win = new PB().M(760, 700).L(760, 300).Q(760, 120, 960, 120).Q(1160, 120, 1160, 300).L(1160, 700).Z();
            FS(g, win, H("#FFF4D8"), H("#8A5A3C"), 10f);
            Line(g, H("#8A5A3C"), 8f, 960, 126, 960, 700); Line(g, H("#8A5A3C"), 8f, 764, 420, 1156, 420);
            F(g, Poly(760, 700, 1160, 700, 1400, 1080, 600, 1080), A(H("#FFF1C8"), 70));
            // 책장
            BookShelf(g, 20, 60, 700, 860, 21);
            BookShelf(g, 1220, 60, 680, 860, 22);
            // 바닥
            F(g, Rect(0, 920, 1920, 160), H("#B98A5E"));
            var k = CG("happy", "raised", "smile", "blanketOpen", true);
            Place(g, Find("서연"), k, 980, 40, 0.95f, false);
            // 책상
            FS(g, RRect(-40, 880, 2000, 240, 10), H("#B07C52"), H("#6E4426"), 4f);
            F(g, Rect(-40, 880, 2000, 18), H("#C9966A"));
            var book = new PB().M(420, 1000).Q(520, 960, 620, 990).Q(720, 960, 820, 1000).L(800, 1070).Q(720, 1040, 620, 1068).Q(520, 1040, 440, 1070).Z();
            FS(g, book, H("#FFFDF6"), H("#9C8A70"), 3f);
            Line(g, H("#9C8A70"), 2.5f, 620, 990, 620, 1068);
            for (int i = 0; i < 5; i++) { Line(g, A(H("#9C8A70"), 140), 2f, 460, 1004 + i * 11, 600, 990 + i * 11); Line(g, A(H("#9C8A70"), 140), 2f, 640, 990 + i * 11, 780, 1004 + i * 11); }
            FS(g, RRect(1300, 930, 90, 110, 14), H("#F2EEE6"), LINE, 3f);
            FS(g, Ell(1345, 936, 80, 18), H("#7A4A30"), LINE, 2f);
            Line(g, H("#F2C44A"), 8f, 880, 1040, 1060, 990);
            Carrot(g, 1560, 970, 72, 0.8f); // 책상 위에 눕혀 둔 당근 인형
            Particles(g, 30, H("#FFF1C8"), 13);
            Caption(g, "임시 CG · 서연_도서관");
        };

        d["기주_음악실"] = g =>
        {
            F(g, Rect(0, 0, 1920, 1080), H("#E6E2F2"));
            // 칠판 + 오선
            FS(g, Rect(110, 120, 820, 380), H("#2F4A40"), H("#8A6A48"), 14f);
            for (int st = 0; st < 2; st++)
                for (int i = 0; i < 5; i++) Line(g, A(Color.White, 170), 2.5f, 160, 190 + st * 160 + i * 18, 880, 190 + st * 160 + i * 18);
            var nr = new Random(4);
            for (int i = 0; i < 12; i++)
            {
                float nx = 200 + i * 56, ny = 190 + (i % 2) * 160 + nr.Next(5) * 9;
                F(g, Ell(nx, ny + 60, 20, 15), A(Color.White, 220));
                Line(g, A(Color.White, 220), 3f, nx + 9, ny + 60, nx + 9, ny + 10);
            }
            // 창문 + 커튼
            FS(g, Rect(1320, 110, 480, 520), H("#CFE6F7"), H("#8C8AA8"), 8f);
            Line(g, H("#8C8AA8"), 6f, 1560, 110, 1560, 630);
            foreach (float cx0 in new float[] { 1290, 1730 })
            {
                var cur = new PB().M(cx0, 90).L(cx0 + 110, 90).Q(cx0 + 90, 380, cx0 + 120, 680).L(cx0 - 10, 680).Q(cx0 + 20, 380, cx0, 90).Z();
                FS(g, cur, H("#F6EEDC"), H("#BFAF92"), 3f);
                for (int i = 1; i < 4; i++) Line(g, A(H("#BFAF92"), 180), 2f, cx0 + i * 26, 100, cx0 + i * 26 + 6, 670);
            }
            // 바닥
            F(g, Rect(0, 780, 1920, 300), H("#BE9068"));
            for (float y = 800; y < 1080; y += 40) F(g, Rect(0, y, 1920, 3), H("#A47652"));
            // 피아노
            FS(g, RRect(40, 560, 640, 360, 14), H("#2A2731"), LINE, 4f);
            F(g, Rect(60, 580, 600, 18), H("#45414F"));
            FS(g, Rect(60, 720, 600, 64), H("#FBFBF8"), LINE, 3f);
            for (int i = 1; i < 26; i++) Line(g, A(LINE, 150), 1.5f, 60 + i * 23, 720, 60 + i * 23, 784);
            for (int i = 0; i < 25; i++) if (i % 7 != 2 && i % 7 != 6) F(g, Rect(76 + i * 23, 720, 14, 38), H("#1E1C24"));
            FS(g, Rect(250, 610, 220, 100), H("#FFFDF4"), H("#9C8A70"), 2.5f);
            for (int i = 0; i < 4; i++) Line(g, A(H("#9C8A70"), 160), 1.5f, 266, 630 + i * 18, 454, 630 + i * 18);
            // 기타
            var gb = new PB().M(1620, 700).Q(1540, 720, 1560, 800).Q(1520, 880, 1600, 950).Q(1680, 990, 1740, 940).Q(1800, 880, 1760, 800).Q(1780, 720, 1700, 700).Q(1660, 690, 1620, 700).Z();
            FS(g, gb, H("#D08A4A"), LINE, 3f);
            FS(g, Ell(1662, 820, 50, 50), H("#3A2416"), LINE, 2f);
            FS(g, RRect(1646, 430, 30, 300, 6), H("#6E4426"), LINE, 2.5f);
            FS(g, RRect(1636, 390, 50, 60, 10), H("#4A2E1C"), LINE, 2.5f);
            // 떠다니는 음표
            for (int i = 0; i < 7; i++)
            {
                float nx = 980 + i * 120, ny = 220 + (i % 3) * 90;
                F(g, Ell(nx, ny, 26, 20), A(H("#9A7AE0"), 200));
                Line(g, A(H("#9A7AE0"), 200), 4f, nx + 11, ny, nx + 11, ny - 54);
                Line(g, A(H("#9A7AE0"), 200), 4f, nx + 11, ny - 54, nx + 30, ny - 40);
            }
            var k = CG("open", "raised", "smile", "headphoneShare", true); k.LookX = -0.6f;
            Place(g, Find("기주"), k, 1060, -20, 1.0f, false);
            Particles(g, 30, H("#E0B6FF"), 14);
            Caption(g, "임시 CG · 기주_음악실");
        };

        d["수민_편의점"] = g =>
        {
            F(g, Rect(0, 0, 1920, 1080), H("#F4F6F8"));
            F(g, Rect(0, 0, 1920, 90), H("#E3E8EE"));
            for (int i = 0; i < 4; i++) FS(g, RRect(120 + i * 460, 24, 300, 34, 10), H("#FFFFFF"), H("#C9D1DB"), 2f);
            // 바깥 창 (저녁)
            FS(g, Rect(760, 130, 420, 520), H("#3F4F80"), H("#9AA5B2"), 10f);
            Glow(g, 1080, 260, 20, H("#FFE7A8"));
            F(g, Rect(1074, 270, 10, 380), H("#2A3560"));
            using (var f = new Font("Malgun Gothic", 46, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var b = new SolidBrush(MINT)) g.DrawString("OPEN 24H", f, b, 820, 160);
            // 음료 냉장고
            FS(g, Rect(0, 120, 700, 820), H("#DDE3EA"), H("#8E98A6"), 6f);
            for (int dr = 0; dr < 3; dr++)
            {
                float dx = 20 + dr * 228;
                FS(g, Rect(dx, 140, 210, 780), H("#EAF6FF"), H("#9AA5B2"), 5f);
                for (int sh = 0; sh < 5; sh++)
                {
                    float sy = 160 + sh * 150;
                    F(g, Rect(dx + 6, sy + 128, 198, 8), H("#B9C3CF"));
                    string[] cols = { "#F37A8C", "#7EC8E3", "#F6C85F", "#8FD39B", "#B28DE0", "#FFFFFF" };
                    for (int b = 0; b < 5; b++)
                    {
                        float bx = dx + 14 + b * 38;
                        var col = H(cols[(sh * 3 + b + dr) % cols.Length]);
                        FS(g, RRect(bx, sy + 30, 30, 98, 8), col, LINE, 2f);
                        FS(g, Rect(bx + 9, sy + 16, 12, 16), H("#E7E9EE"), LINE, 1.5f);
                        F(g, Rect(bx + 3, sy + 62, 24, 26), A(Color.White, 170));
                    }
                }
                Line(g, A(Color.White, 200), 6f, dx + 30, 160, dx + 60, 900);
                FS(g, RRect(dx + 186, 480, 10, 90, 4), H("#9AA5B2"), LINE, 1.5f);
            }
            // 과자 진열대
            FS(g, Rect(1240, 200, 680, 740), H("#E9EDF2"), H("#A3ADB9"), 5f);
            string[] snack = { "#F6C85F", "#F37A8C", "#7EC8E3", "#8FD39B", "#F29B5B", "#C59CE8" };
            var sr = new Random(3);
            for (int sh = 0; sh < 4; sh++)
            {
                float sy = 220 + sh * 180;
                F(g, Rect(1240, sy + 150, 680, 14), H("#B9C3CF"));
                FS(g, Rect(1300 + sh * 120, sy + 152, 40, 12), H("#FFE45C"), LINE, 1.5f);
                for (int b = 0; b < 6; b++)
                {
                    float bx = 1256 + b * 110 + sr.Next(8);
                    var col = H(snack[(b + sh * 2) % snack.Length]);
                    var bag = new PB().M(bx, sy + 30).L(bx + 92, sy + 30).Q(bx + 100, sy + 90, bx + 92, sy + 148).L(bx, sy + 148).Q(bx - 8, sy + 90, bx, sy + 30).Z();
                    FS(g, bag, col, LINE, 2.5f);
                    for (int z = 0; z < 6; z++) { Line(g, A(LINE, 120), 1.5f, bx + z * 16, sy + 30, bx + z * 16 + 8, sy + 40); Line(g, A(LINE, 120), 1.5f, bx + z * 16, sy + 148, bx + z * 16 + 8, sy + 138); }
                    FS(g, Ell(bx + 46, sy + 86, 46, 46), A(Color.White, 220), A(LINE, 120), 2f);
                    F(g, Ell(bx + 46, sy + 86, 22, 22), col);
                }
            }
            // 바닥
            F(g, Rect(0, 940, 1920, 140), H("#DDE2E8"));
            for (float x = 0; x < 1920; x += 120) F(g, Rect(x, 940, 3, 140), H("#C5CCD5"));
            F(g, Rect(0, 1000, 1920, 3), H("#C5CCD5"));
            var k = CG("down", "raised", "open", "snack", true); k.Sparkle = true; k.LookX = 0.6f; k.LookY = 1f;
            Place(g, Find("수민"), k, 980, -20, 1.0f, false);
            Particles(g, 20, H("#B8FFE4"), 15);
            Caption(g, "임시 CG · 수민_편의점");
        };

        return d;
    }

    static void BookShelf(Graphics g, float x, float y, float w, float h, int seed)
    {
        var r = new Random(seed);
        FS(g, Rect(x, y, w, h), H("#8A5A3C"), H("#5A3820"), 5f);
        string[] cols = { "#C0504D", "#4F81BD", "#9BBB59", "#8064A2", "#F79646", "#2C4D75", "#D9C27E", "#7A4A3A", "#4BACC6", "#E6A1B0" };
        float shelfH = 140;
        for (float sy = y + 20; sy + shelfH <= y + h; sy += shelfH)
        {
            F(g, Rect(x + 16, sy, w - 32, shelfH - 16), H("#5E3B24"));
            float bx = x + 20;
            while (bx < x + w - 40)
            {
                float bw = 18 + r.Next(22), bh = 80 + r.Next(40);
                var col = H(cols[r.Next(cols.Length)]);
                if (r.Next(9) == 0)
                {
                    var st = g.Save(); g.TranslateTransform(bx, sy + shelfH - 16); g.RotateTransform(-14);
                    FS(g, Rect(0, -bh, bw, bh), col, LINE, 2f); g.Restore(st); bx += bw + 18; continue;
                }
                FS(g, Rect(bx, sy + shelfH - 16 - bh, bw, bh), col, LINE, 2f);
                F(g, Rect(bx + 2, sy + shelfH - 16 - bh + 10, bw - 4, 5), A(Color.White, 140));
                F(g, Rect(bx + 2, sy + shelfH - 34, bw - 4, 5), A(Color.White, 110));
                if (bw > 26) F(g, Rect(bx + 6, sy + shelfH - 16 - bh + 30, bw - 12, 22), A(Color.White, 160));
                bx += bw + 2;
            }
            F(g, Rect(x + 8, sy + shelfH - 16, w - 16, 16), H("#9C6A48"));
        }
    }

    // ───────────────────────── 실행 ─────────────────────────
    static void Save(int w, int h, Action<Graphics> draw, string path)
    {
        const int SS = 2;
        using (var big = new Bitmap(w * SS, h * SS, PixelFormat.Format32bppPArgb))
        {
            using (var g = Graphics.FromImage(big))
            {
                Setup(g); g.Clear(Color.Transparent); g.ScaleTransform(SS, SS); draw(g);
            }
            using (var small = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            {
                using (var g2 = Graphics.FromImage(small))
                {
                    g2.CompositingMode = CompositingMode.SourceCopy;
                    g2.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g2.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g2.DrawImage(big, new Rectangle(0, 0, w, h));
                }
                small.Save(path, ImageFormat.Png);
            }
        }
    }

    public static void Run(string root, string filter)
    {
        string chDir = Path.Combine(root, "Assets\\Resources\\Characters"), cgDir = Path.Combine(root, "Assets\\Resources\\CG");
        string[] exprs = { "기본", "웃음", "화남", "부끄럼" };
        foreach (var s in AllSpecs())
            foreach (var e in exprs)
            {
                string name = s.Name + "_" + e;
                if (!string.IsNullOrEmpty(filter) && !name.Contains(filter)) continue;
                var spec = s; var look = Standing(s, e);
                Save(800, 1400, g => DrawChar(g, spec, look), Path.Combine(chDir, name + ".png"));
                Console.WriteLine("스탠딩 " + name);
            }
        foreach (var s0 in AllSpecs())
            foreach (var e in exprs)
            {
                string name = s0.Name + "_남_" + e;
                if (!string.IsNullOrEmpty(filter) && !name.Contains(filter)) continue;
                Spec fresh = null; foreach (var x in AllSpecs()) if (x.Name == s0.Name) fresh = x; var spec = AsMale(fresh); var look = Standing(fresh, e);
                Save(800, 1400, g => DrawChar(g, spec, look), Path.Combine(chDir, name + ".png"));
                Console.WriteLine("스탠딩 " + name);
            }
        foreach (var kv in CGs())
        {
            // 기본(전원 여자) + 주인공이 남자인 버전 "이름_남" (하렘은 친구 넷 모두 남자)
            string owner = kv.Key.Substring(0, kv.Key.IndexOf('_'));
            var variants = new List<KeyValuePair<string, string[]>>();
            variants.Add(new KeyValuePair<string, string[]>(kv.Key, new string[0]));
            if (owner == "하렘") variants.Add(new KeyValuePair<string, string[]>(kv.Key + "_남", new[] { "승현", "서연", "기주", "수민" }));
            else variants.Add(new KeyValuePair<string, string[]>(kv.Key + "_남", new[] { owner }));
            foreach (var v in variants)
            {
                if (!string.IsNullOrEmpty(filter) && !v.Key.Contains(filter)) continue;
                maleSet = new HashSet<string>(v.Value);
                Save(1920, 1080, kv.Value, Path.Combine(cgDir, v.Key + ".png"));
                Console.WriteLine("CG " + v.Key);
            }
            maleSet = new HashSet<string>();
        }
    }
}
