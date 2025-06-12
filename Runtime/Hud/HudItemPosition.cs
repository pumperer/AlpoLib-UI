namespace alpoLib.UI.Hud
{
    // [Flags]
    public enum HudItemPosition
    {
        None = 0,
        Top = 1 << 0,
        RightTop = 1 << 1,
        Right = 1 << 2,
        RightBottom = 1 << 3,
        Bottom = 1 << 4,
        LeftBottom = 1 << 5,
        Left = 1 << 6,
        LeftTop = 1 << 7,
    }
}