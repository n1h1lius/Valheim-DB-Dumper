using System;
using System.Linq;

namespace ValheimDBDumper;

public class Color
{
    // === RESET ===
    public static readonly string RESET = "\u001B[0m";

    // === TEXT STYLES ===
    public static readonly string BOLD = "\u001B[1m";
    public static readonly string DIM = "\u001B[2m";
    public static readonly string ITALIC = "\u001B[3m";
    public static readonly string UNDERLINE = "\u001B[4m";
    public static readonly string BLINK = "\u001B[5m";
    public static readonly string REVERSE = "\u001B[7m";
    public static readonly string HIDDEN = "\u001B[8m";
    public static readonly string STRIKETHROUGH = "\u001B[9m";

    // === FOREGROUND COLORS (Normal) ===
    public static readonly string BLACK = "\u001B[30m";
    public static readonly string RED = "\u001B[31m";
    public static readonly string GREEN = "\u001B[32m";
    public static readonly string YELLOW = "\u001B[33m";
    public static readonly string BLUE = "\u001B[34m";
    public static readonly string MAGENTA = "\u001B[35m";
    public static readonly string CYAN = "\u001B[36m";
    public static readonly string WHITE = "\u001B[37m";
    public static readonly string GRAY = "\u001B[90m";
    public static readonly string ORANGE = TextFromRGB(255, 165, 0);
    public static readonly string PINK = TextFromRGB(255, 192, 203);

    // === FOREGROUND COLORS (Bright) ===
    public static readonly string BRIGHT_RED = "\u001B[91m";
    public static readonly string BRIGHT_GREEN = "\u001B[92m";
    public static readonly string BRIGHT_YELLOW = "\u001B[93m";
    public static readonly string BRIGHT_BLUE = "\u001B[94m";
    public static readonly string BRIGHT_MAGENTA = "\u001B[95m";
    public static readonly string BRIGHT_CYAN = "\u001B[96m";
    public static readonly string BRIGHT_WHITE = "\u001B[97m";
    public static readonly string BRIGHT_ORANGE = TextFromRGB(255, 153, 28);
    public static readonly string BRIGHT_PINK = TextFromRGB(244, 160, 250);

    // === BACKGROUND COLORS (Normal) ===
    public static readonly string BG_BLACK = "\u001B[40m";
    public static readonly string BG_RED = "\u001B[41m";
    public static readonly string BG_GREEN = "\u001B[42m";
    public static readonly string BG_YELLOW = "\u001B[43m";
    public static readonly string BG_BLUE = "\u001B[44m";
    public static readonly string BG_MAGENTA = "\u001B[45m";
    public static readonly string BG_CYAN = "\u001B[46m";
    public static readonly string BG_WHITE = "\u001B[47m";
    public static readonly string BG_GRAY = "\u001B[100m";
    public static readonly string BG_ORANGE = BGFromRGB(255, 165, 0);
    public static readonly string BG_PINK = BGFromRGB(255, 192, 203);

    // === BACKGROUND COLORS (Bright) ===
    public static readonly string BG_BRIGHT_RED = "\u001B[101m";
    public static readonly string BG_BRIGHT_GREEN = BGFromRGB(48, 255, 62);
    public static readonly string BG_BRIGHT_YELLOW = "\u001B[103m";
    public static readonly string BG_BRIGHT_BLUE = "\u001B[104m";
    public static readonly string BG_BRIGHT_MAGENTA = "\u001B[105m";
    public static readonly string BG_BRIGHT_CYAN = BGFromRGB(115, 255, 250);
    public static readonly string BG_BRIGHT_WHITE = "\u001B[107m";
    public static readonly string BG_BRIGHT_ORANGE = BGFromRGB(255, 153, 28);
    public static readonly string BG_BRIGHT_PINK = BGFromRGB(244, 160, 250);

    // === HIGH INTENSITY (Legacy names) ===
    public static readonly string BLACK_BRIGHT = "\u001B[90m";
    public static readonly string RED_BRIGHT = "\u001B[91m";
    public static readonly string GREEN_BRIGHT = "\u001B[92m";
    public static readonly string YELLOW_BRIGHT = "\u001B[93m";
    public static readonly string BLUE_BRIGHT = "\u001B[94m";
    public static readonly string MAGENTA_BRIGHT = "\u001B[95m";
    public static readonly string CYAN_BRIGHT = "\u001B[96m";
    public static readonly string WHITE_BRIGHT = "\u001B[97m";


    private static bool IsColourSupported(int r, int g, int b)
    {
        if (r < 0 || r > 255 || g < 0 || g > 255 || b < 0 || b > 255)
            throw new ArgumentException("Invalid Colour: Colour code must be set between 0 and 255.");

        return true;
    }

    public static string TextFromRGB(int r, int g, int b)
    {
        if (IsColourSupported(r, g, b))
            return $"\u001B[38;2;{r};{g};{b}m";
        return string.Empty;
    }

    public static string BGFromRGB(int r, int g, int b)
    {
        if (IsColourSupported(r, g, b))
            return $"\u001B[48;2;{r};{g};{b}m";
        return string.Empty;
    }
}
