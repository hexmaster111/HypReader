using System.Numerics;
using System.Text;
using System.Xml;
using Raylib_CsLo;
using static Raylib_CsLo.RayGui;
using static Raylib_CsLo.Raylib;
using Rectangle = Raylib_CsLo.Rectangle;

InitWindow(1000, 800, "HypRender");
SetTargetFPS(GetMonitorRefreshRate(GetCurrentMonitor()));

Font Font_SpaceMono_Bold;
Font Font_SpaceMono_Normal;
Font Font_SpaceMono_Italic;
Font Font_SpaceMono_BoldItalic;

Font_SpaceMono_Bold = LoadFontEx("SpaceMono-Bold.ttf", 48 * 2, 0);
Font_SpaceMono_Normal = LoadFontEx("SpaceMono-Regular.ttf", 48 * 2, 0);
Font_SpaceMono_Italic = LoadFontEx("SpaceMono-Italic.ttf", 48 * 2, 0);
Font_SpaceMono_BoldItalic = LoadFontEx("SpaceMono-BoldItalic.ttf", 48 * 2, 0);


SetTextureFilter(Font_SpaceMono_Bold.texture, TextureFilter.TEXTURE_FILTER_ANISOTROPIC_16X);
SetTextureFilter(Font_SpaceMono_Normal.texture, TextureFilter.TEXTURE_FILTER_ANISOTROPIC_16X);
SetTextureFilter(Font_SpaceMono_Italic.texture, TextureFilter.TEXTURE_FILTER_ANISOTROPIC_16X);
SetTextureFilter(Font_SpaceMono_BoldItalic.texture, TextureFilter.TEXTURE_FILTER_ANISOTROPIC_16X);



string filetext = File.ReadAllText("test.ini");

var steps = ReadContent(filetext);

steps.Reverse();

Stack<Opp> ops = new(steps);

List<Fader> faders = new();

Opp now = null;
Color fg = WHITE;
Color bg = BLACK;
float fontsize = 20;
Font font = Font_SpaceMono_Normal;


Fader? currentText = null;
double resumeAt = 0;

Vector2 currsor = new();

bool centerNext = false;
bool centeredFirst = false;

bool togeather = false;
bool nextLeftToRight = false;
Opp leftrightopp = null;

bool waitforclick = false;

while (!WindowShouldClose())
{

    while ((togeather || currentText == null) && resumeAt == 0 && !waitforclick)
    {
        if (!ops.TryPop(out now))
        {
            goto FILE_OVER;
        }

        switch (now.OpKind)
        {
            case Opp.Kind.Fg:
                fg = now.color;
                break;

            case Opp.Kind.Bg:
                bg = now.color;
                break;

            case Opp.Kind.WaitForClick:
                waitforclick = true;
                break;

            case Opp.Kind.Bold:
                font = Font_SpaceMono_Bold;
                break;

            case Opp.Kind.Italic:
                font = Font_SpaceMono_Italic;
                break;

            case Opp.Kind.Normal:
                font = Font_SpaceMono_Normal;
                break;

            case Opp.Kind.BoldItalic:
                font = Font_SpaceMono_BoldItalic;
                break;

            case Opp.Kind.StartTogeather:
                togeather = true;
                break;

            case Opp.Kind.EndTogeather:
                togeather = false;
                break;

            case Opp.Kind.LeftToRight:
                nextLeftToRight = true;
                leftrightopp = now;
                break;

            case Opp.Kind.Center:
                centerNext = true;
                centeredFirst = false;
                break;

            case Opp.Kind.Left:
                centerNext = false;
                centeredFirst = false;
                break;

            case Opp.Kind.Clear:
                faders.Clear();
                currsor = Vector2.Zero;
                break;

            case Opp.Kind.Delay:
                resumeAt = GetTime() + now.Time;
                break;

            case Opp.Kind.FontSize:
                fontsize = now.Size;
                break;

            case Opp.Kind.Text:
                faders.Add(currentText = new()
                {
                    Goal = now.Text,
                    Fg = fg,
                    FontSize = fontsize,
                    Position = currsor,
                    Font = Font_SpaceMono_Normal,
                    MoveLeftRight = nextLeftToRight
                });

                if (nextLeftToRight && leftrightopp != null)
                {
                    currentText.LeftPercent = leftrightopp.LeftPercent;
                    currentText.RightPercent = leftrightopp.RightPercent;
                }

                nextLeftToRight = false;


                var size = MeasureTextEx(currentText.Font, now.Text, (int)fontsize, 1);
                currsor += size;
                currsor.X = 0;

                if (centerNext)
                {
                    if (!centeredFirst)
                    {
                        centeredFirst = true;
                        currentText.Position.Y = GetScreenHeight() / 2;
                    }

                    currentText.Position.X = (float)(size.X - (.5 * size.X));
                }

                break;

        }
    }


    var dt = GetFrameTime();
    foreach (var item in faders)
    {
        item.Update();
    }

    if (currentText != null && currentText.IsFadded())
        currentText = null;

    if (resumeAt != 0 && GetTime() > resumeAt)
    {
        resumeAt = 0;
    }

    if (waitforclick && IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT)) waitforclick = false;


    BeginDrawing();
    ClearBackground(bg);

    foreach (var item in faders)
    {
        item.Draw();
    }


    EndDrawing();
}

FILE_OVER:

CloseWindow();


return 0;



List<Opp> ReadContent(string conent)
{
    var sr = new StringReader(conent);
    List<Opp> ret = new();

    while (true)
    {
        var line = sr.ReadLine();
        if (line == null) break;

        if (line.StartsWith('[') && line.EndsWith(']'))
        {
            ret.Add(BuildOpCommand(line[1..^1]));
        }
        else if (line.StartsWith("//"))
        {
            continue;
        }
        else if (!string.IsNullOrWhiteSpace(line))
        {
            ret.Add(new()
            {
                OpKind = Opp.Kind.Text,
                Text = line
            });
        }
    }


    return ret;
}



Opp BuildOpCommand(string v)
{
    var split = v.Split([' ', ']']);

    return split[0].ToLower() switch
    {
        "fg" => new()
        {
            OpKind = Opp.Kind.Fg,
            color = new()
            {
                r = byte.Parse(split[1], System.Globalization.NumberStyles.HexNumber),
                g = byte.Parse(split[2], System.Globalization.NumberStyles.HexNumber),
                b = byte.Parse(split[3], System.Globalization.NumberStyles.HexNumber),
                a = 255
            }
        },
        "bg" => new()
        {
            OpKind = Opp.Kind.Bg,
            color = new()
            {
                r = byte.Parse(split[1], System.Globalization.NumberStyles.HexNumber),
                g = byte.Parse(split[2], System.Globalization.NumberStyles.HexNumber),
                b = byte.Parse(split[3], System.Globalization.NumberStyles.HexNumber),
                a = 255
            }
        },
        "delay" => new()
        {
            OpKind = Opp.Kind.Delay,
            Time = double.Parse(split[1])
        },
        "clear" => new() { OpKind = Opp.Kind.Clear },
        "left" => new() { OpKind = Opp.Kind.Left },
        "start_togeather" => new() { OpKind = Opp.Kind.StartTogeather },
        "end_togeather" => new() { OpKind = Opp.Kind.EndTogeather },
        "center" => new() { OpKind = Opp.Kind.Center },
        "bold" => new() { OpKind = Opp.Kind.Bold },
        "bolditalic" => new() { OpKind = Opp.Kind.BoldItalic },
        "normal" => new() { OpKind = Opp.Kind.Normal },
        "italic" => new() { OpKind = Opp.Kind.Italic },
        "lefttoright" => new() { OpKind = Opp.Kind.LeftToRight, LeftPercent = float.Parse(split[1]), RightPercent = float.Parse(split[2]) },
        "fontsize" => new() { OpKind = Opp.Kind.FontSize, Size = float.Parse(split[1]) },
        "waitforclick" => new() { OpKind = Opp.Kind.WaitForClick },
        _ => new() { OpKind = Opp.Kind.Nothing },
    };
}

class Fader
{
    public string Goal;
    public string Now = "";
    public int Currsor = 0;
    public Vector2 Position;
    public Color Fg;
    public float FontSize;

    public double NextUpdateTime = 0;
    public Font Font;
    public bool MoveLeftRight;
    public int MoveDirection = 1;
    public float MoveSpeed = 2;
    public float LeftPercent = 0;
    public float RightPercent = 1;

    public bool IsFadded() => Goal.Length < Currsor;

    public void Update()
    {
        if (!IsFadded() && NextUpdateTime < GetTime())
        {
            Now = Goal[..Currsor];
            Currsor += 1;
            NextUpdateTime = GetTime() + .02;
        }

        if (MoveLeftRight)
        {
            var size = MeasureTextEx(Font, Now, FontSize, 1);

            Position.X += MoveDirection * MoveSpeed;

            if (Position.X + size.X > (GetScreenWidth() * RightPercent))
            {
                MoveDirection = -1;
            }

            if (GetScreenWidth() * LeftPercent > Position.X)
            {
                MoveDirection = 1;

                Position.X = GetScreenWidth() * LeftPercent;
            }

        }
    }


    public void Draw()
    {
        DrawTextEx(Font, Now, Position, FontSize, 1, Fg);
    }

}

class Opp
{
    public enum Kind { Nothing, Fg, Bg, Text, Delay, Clear, SetCursor, FontSize, Center, Left, StartTogeather, EndTogeather, Bold, BoldItalic, Normal, Italic, LeftToRight, WaitForClick };
    public Kind OpKind;
    public Color color;
    public string Text;
    public Vector2 Currsor;
    public double Time;
    public float Size;
    public float LeftPercent, RightPercent;
}


class TextOnScreen
{
    public string Text;
    public Color Fg;
    public Vector2 Position;
    public float FontSize;
}