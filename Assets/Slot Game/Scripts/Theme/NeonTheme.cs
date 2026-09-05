using UnityEngine;

// Procedurally draws every piece of art for the neon-casino re-skin
// (symbols, panels, glow dots) directly into textures at runtime, so the
// re-theme needs no external art pipeline and never touches the .unity
// scene file - it only hands out Sprites for existing components to use.
public static class NeonTheme
{
    public static readonly Color BackgroundTop = new Color32(18, 8, 41, 255);
    public static readonly Color BackgroundBottom = new Color32(8, 4, 20, 255);
    public static readonly Color PanelFill = new Color32(24, 12, 48, 235);
    public static readonly Color NeonPink = new Color32(255, 45, 149, 255);
    public static readonly Color NeonPurple = new Color32(155, 61, 255, 255);
    public static readonly Color NeonGold = new Color32(255, 196, 60, 255);
    public static readonly Color NeonCyan = new Color32(60, 226, 255, 255);
    public static readonly Color NeonGreen = new Color32(80, 255, 158, 255);
    public static readonly Color NeonOrange = new Color32(255, 130, 40, 255);

    // Multiplied onto whatever a sprite/image already renders, to pull the
    // rest of the (untouched) art toward the neon-purple palette without
    // needing to replace it.
    public static readonly Color GlobalTint = new Color(0.55f, 0.4f, 0.95f, 1f);

    public static float EaseOutCubic(float t)
    {
        float inv = 1f - Mathf.Clamp01(t);
        return 1f - inv * inv * inv;
    }

    public static Color GetSymbolAccent(SlotGameManager.SlorRell symbol)
    {
        switch (symbol)
        {
            case SlotGameManager.SlorRell.Seven: return NeonCyan;
            case SlotGameManager.SlorRell.Bar: return NeonPink;
            case SlotGameManager.SlorRell.Bell: return NeonGold;
            case SlotGameManager.SlorRell.Chery: return NeonGreen;
            default: return Color.white;
        }
    }

    // =========================================================
    // LOW LEVEL PIXEL HELPERS
    // =========================================================

    private static float SoftEdge(float distance, float edge, float aa = 1.5f)
    {
        return Mathf.Clamp01((edge - distance) / aa + 0.5f);
    }

    private static float DistanceToSegment(float px, float py, float ax, float ay, float bx, float by)
    {
        float abx = bx - ax;
        float aby = by - ay;
        float apx = px - ax;
        float apy = py - ay;

        float abLenSq = abx * abx + aby * aby;
        float t = abLenSq > 0f ? Mathf.Clamp01((apx * abx + apy * aby) / abLenSq) : 0f;

        float cx = ax + abx * t;
        float cy = ay + aby * t;

        float dx = px - cx;
        float dy = py - cy;

        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    private static float CircleCoverage(float px, float py, float cx, float cy, float r)
    {
        float d = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
        return SoftEdge(d, r);
    }

    private static float LineCoverage(float px, float py, float ax, float ay, float bx, float by, float thickness)
    {
        float d = DistanceToSegment(px, py, ax, ay, bx, by);
        return SoftEdge(d, thickness * 0.5f);
    }

    private static Color AlphaOver(Color src, Color dst)
    {
        float outA = src.a + dst.a * (1f - src.a);
        if (outA <= 0f)
            return new Color(0f, 0f, 0f, 0f);

        return new Color(
            (src.r * src.a + dst.r * dst.a * (1f - src.a)) / outA,
            (src.g * src.a + dst.g * dst.a * (1f - src.a)) / outA,
            (src.b * src.a + dst.b * dst.a * (1f - src.a)) / outA,
            outA
        );
    }

    private static void Blend(Color[] pixels, int size, int x, int y, Color srcWithAlpha)
    {
        if (x < 0 || x >= size || y < 0 || y >= size || srcWithAlpha.a <= 0f)
            return;

        int idx = y * size + x;
        pixels[idx] = AlphaOver(srcWithAlpha, pixels[idx]);
    }

    private static Sprite ToSprite(Texture2D tex, Vector4 border = default)
    {
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();

        return Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            border
        );
    }

    // =========================================================
    // ROUNDED PANEL (buttons, banners, background cards)
    // =========================================================

    public static Sprite CreateRoundedPanel(
        int width, int height, int cornerRadius,
        Color fillTop, Color fillBottom,
        Color borderColor, int borderWidth,
        int glowSize, Color glowColor)
    {
        int pad = Mathf.Max(glowSize, 1);
        int texW = width + pad * 2;
        int texH = height + pad * 2;

        Texture2D tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[texW * texH];

        for (int y = 0; y < texH; y++)
        {
            for (int x = 0; x < texW; x++)
            {
                int lx = x - pad;
                int ly = y - pad;

                float distOutside = DistanceOutsideRoundedRect(lx, ly, width, height, cornerRadius);

                Color pixel;

                if (distOutside <= 0f)
                {
                    float t = height > 0 ? Mathf.Clamp01((float)ly / height) : 0f;
                    Color fill = Color.Lerp(fillBottom, fillTop, t);

                    float borderDist = -distOutside;

                    if (borderWidth > 0 && borderDist < borderWidth)
                    {
                        float borderT = borderDist / borderWidth;
                        pixel = Color.Lerp(borderColor, fill, borderT);
                    }
                    else
                    {
                        pixel = fill;
                    }
                }
                else if (glowSize > 0 && distOutside <= glowSize)
                {
                    float glowT = 1f - (distOutside / glowSize);
                    pixel = glowColor;
                    pixel.a *= glowT * glowT;
                }
                else
                {
                    pixel = new Color(0f, 0f, 0f, 0f);
                }

                pixels[y * texW + x] = pixel;
            }
        }

        tex.SetPixels(pixels);

        float b = pad + cornerRadius;
        return ToSprite(tex, new Vector4(b, b, b, b));
    }

    private static float DistanceOutsideRoundedRect(float x, float y, int width, int height, int radius)
    {
        float halfW = width / 2f;
        float halfH = height / 2f;

        float px = x - halfW;
        float py = y - halfH;

        float rectHalfW = halfW - radius;
        float rectHalfH = halfH - radius;

        float dx = Mathf.Max(Mathf.Abs(px) - rectHalfW, 0f);
        float dy = Mathf.Max(Mathf.Abs(py) - rectHalfH, 0f);

        return Mathf.Sqrt(dx * dx + dy * dy) - radius;
    }

    // =========================================================
    // GLOW DOT (win-celebration particles)
    // =========================================================

    public static Sprite CreateGlowDot(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                float t = Mathf.Clamp01(1f - d / radius);

                Color c = color;
                c.a *= t * t;

                pixels[y * size + x] = c;
            }
        }

        tex.SetPixels(pixels);
        return ToSprite(tex);
    }

    // =========================================================
    // VERTICAL GRADIENT (ambient background overlay)
    // =========================================================

    public static Sprite CreateVerticalGradient(int width, int height, Color top, Color bottom)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            Color c = Color.Lerp(bottom, top, height > 1 ? (float)y / (height - 1) : 0f);

            for (int x = 0; x < width; x++)
                pixels[y * width + x] = c;
        }

        tex.SetPixels(pixels);
        return ToSprite(tex);
    }

    // =========================================================
    // SYMBOL ICONS (reel sprites)
    // =========================================================

    public static Sprite CreateSymbolSprite(SlotGameManager.SlorRell symbol, int size = 256)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color(0f, 0f, 0f, 0f);

        Color accent = GetSymbolAccent(symbol);

        DrawBadge(pixels, size, accent);

        switch (symbol)
        {
            case SlotGameManager.SlorRell.Chery:
                DrawCherry(pixels, size);
                break;

            case SlotGameManager.SlorRell.Bell:
                DrawBell(pixels, size);
                break;

            case SlotGameManager.SlorRell.Bar:
                DrawBar(pixels, size);
                break;

            case SlotGameManager.SlorRell.Seven:
                DrawSeven(pixels, size);
                break;
        }

        tex.SetPixels(pixels);
        return ToSprite(tex);
    }

    private static void DrawBadge(Color[] pixels, int size, Color accent)
    {
        float s = size;
        float cx = s * 0.5f;
        float cy = s * 0.5f;
        float radius = s * 0.46f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));

                if (d > radius)
                {
                    float glowT = Mathf.Clamp01(1f - (d - radius) / (s * 0.12f));

                    if (glowT > 0f)
                    {
                        Color glow = accent;
                        glow.a *= glowT * glowT * 0.6f;
                        Blend(pixels, size, x, y, glow);
                    }

                    continue;
                }

                float fillCov = SoftEdge(d, radius);
                Color fill = PanelFill;
                fill.a *= fillCov;
                Blend(pixels, size, x, y, fill);

                float ringCov = Mathf.Clamp01(SoftEdge(d, radius) - SoftEdge(d, radius - s * 0.025f));

                if (ringCov > 0f)
                {
                    Color ring = accent;
                    ring.a *= ringCov;
                    Blend(pixels, size, x, y, ring);
                }
            }
        }
    }

    private static void DrawCherry(Color[] pixels, int size)
    {
        float s = size;
        Color berry = NeonPink;
        Color stem = NeonGreen;

        Vector2 leftBerry = new Vector2(s * 0.38f, s * 0.30f);
        Vector2 rightBerry = new Vector2(s * 0.62f, s * 0.24f);
        float berryR = s * 0.15f;
        Vector2 stemTop = new Vector2(s * 0.5f, s * 0.85f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float stemCov = LineCoverage(x, y, stemTop.x, stemTop.y, leftBerry.x, leftBerry.y, s * 0.028f);
                stemCov = Mathf.Max(stemCov, LineCoverage(x, y, stemTop.x, stemTop.y, rightBerry.x, rightBerry.y, s * 0.028f));

                if (stemCov > 0f)
                {
                    Color c = stem;
                    c.a *= stemCov;
                    Blend(pixels, size, x, y, c);
                }

                float berryCov = CircleCoverage(x, y, leftBerry.x, leftBerry.y, berryR);
                berryCov = Mathf.Max(berryCov, CircleCoverage(x, y, rightBerry.x, rightBerry.y, berryR));

                if (berryCov > 0f)
                {
                    Color c = berry;
                    c.a *= berryCov;
                    Blend(pixels, size, x, y, c);
                }
            }
        }
    }

    private static float BellBodyCoverage(float x, float y, float cx, float s)
    {
        float bodyBottom = s * 0.22f;
        float bodyTop = s * 0.80f;

        if (y < bodyBottom - 2f || y > bodyTop + 2f)
            return 0f;

        float t = Mathf.Clamp01(Mathf.InverseLerp(bodyBottom, bodyTop, y));
        float maxHalfWidth = s * 0.30f;
        float halfWidth = maxHalfWidth * (1f - 0.8f * t * t);

        float dx = Mathf.Abs(x - cx);
        return SoftEdge(dx, halfWidth);
    }

    private static void DrawBell(Color[] pixels, int size)
    {
        float s = size;
        float cx = s * 0.5f;
        Color gold = NeonGold;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float cov = BellBodyCoverage(x, y, cx, s);
                cov = Mathf.Max(cov, LineCoverage(x, y, cx - s * 0.34f, s * 0.22f, cx + s * 0.34f, s * 0.22f, s * 0.035f));
                cov = Mathf.Max(cov, CircleCoverage(x, y, cx, s * 0.85f, s * 0.045f));

                if (cov > 0f)
                {
                    Color c = gold;
                    c.a *= cov;
                    Blend(pixels, size, x, y, c);
                }
            }
        }
    }

    private static void DrawBar(Color[] pixels, int size)
    {
        float s = size;
        float cx = s * 0.5f;
        float cy = s * 0.5f;
        Color orange = NeonOrange;
        Color border = NeonPink;

        float halfW = s * 0.34f;
        float halfH = s * 0.30f;
        float[] stripeYs = { s * 0.36f, s * 0.5f, s * 0.64f };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - cx) - halfW;
                float dy = Mathf.Abs(y - cy) - halfH;
                float outsideDist = Mathf.Max(dx, dy);

                float ringOuter = SoftEdge(outsideDist, 0f);
                float ringInner = SoftEdge(outsideDist, -s * 0.03f);
                float ring = Mathf.Clamp01(ringOuter - ringInner);

                if (ring > 0f)
                {
                    Color c = border;
                    c.a *= ring;
                    Blend(pixels, size, x, y, c);
                }

                float stripeCov = 0f;

                for (int i = 0; i < stripeYs.Length; i++)
                {
                    stripeCov = Mathf.Max(
                        stripeCov,
                        LineCoverage(x, y, cx - halfW * 0.75f, stripeYs[i], cx + halfW * 0.75f, stripeYs[i], s * 0.05f)
                    );
                }

                if (stripeCov > 0f)
                {
                    Color c = orange;
                    c.a *= stripeCov;
                    Blend(pixels, size, x, y, c);
                }
            }
        }
    }

    private static void DrawSeven(Color[] pixels, int size)
    {
        float s = size;
        Color cyan = NeonCyan;
        float thickness = s * 0.09f;

        Vector2 topLeft = new Vector2(s * 0.28f, s * 0.78f);
        Vector2 topRight = new Vector2(s * 0.74f, s * 0.78f);
        Vector2 bottomLeft = new Vector2(s * 0.38f, s * 0.16f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float cov = LineCoverage(x, y, topLeft.x, topLeft.y, topRight.x, topRight.y, thickness);
                cov = Mathf.Max(cov, LineCoverage(x, y, topRight.x, topRight.y, bottomLeft.x, bottomLeft.y, thickness));

                if (cov > 0f)
                {
                    Color c = cyan;
                    c.a *= cov;
                    Blend(pixels, size, x, y, c);
                }
            }
        }
    }
}
