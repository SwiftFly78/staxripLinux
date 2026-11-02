function MultiSharpen(clip C, int multi, float "sharpen", float "blur")
{
    shrp = Min(Max(0.0, Default(sharpen, 0.6)), 1.0)
    blr  = Min(Max(0.0, Default(blur,    0.3)), 1.0)
    C
    for (i=multi, 1, -1) {
        Sharpen(shrp).Blur(blr)
    }
    return Last
}
