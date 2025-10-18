using System.Numerics;
using System.Text;
using System.Xml;
using Raylib_CsLo;
using static Raylib_CsLo.RayGui;
using static Raylib_CsLo.Raylib;
using Rectangle = Raylib_CsLo.Rectangle;

SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
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

if (2 > Environment.GetCommandLineArgs().Length)
{

    Console.WriteLine("Missing Input File");
    return 1;
}

FileSystemWatcher? fsWatch = null;

// we dont wanna reload the global font offsets, we need to reload to see the changes to them 
int fontsizeOffset = 0;

RELOAD:
PollInputEvents();


string filetext = File.ReadAllText(Environment.GetCommandLineArgs()[1]);

var steps = ReadContent(filetext);

steps.Reverse();

Stack<Opp> ops = new(steps);

List<Fader> faders = new();

Opp now = null;
Color fg = WHITE;
Color bg = BLACK;
float fontsize = 20;
float textspeed = .02f;
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
bool typeNext = false;
bool dev = false;


bool hasStartHere = ops.Any(x => x.OpKind == Opp.Kind.StartHere);
if (hasStartHere) while (ops.Pop().OpKind != Opp.Kind.StartHere) ;

bool ReloadFile = false;

var filepath = Path.GetFullPath(Environment.GetCommandLineArgs()[1]);


if (fsWatch == null)
{
    fsWatch = new FileSystemWatcher(Path.GetDirectoryName(filepath));

    fsWatch.Changed += (object s, FileSystemEventArgs e) =>
    {
        if (e.FullPath == filepath) ReloadFile = true;
    };
}

fsWatch.EnableRaisingEvents = true;


while (!WindowShouldClose())
{

    if(IsKeyPressed(KeyboardKey.KEY_EQUAL)){
        fontsizeOffset += 1;
        goto RELOAD;
    }

    if(IsKeyPressed(KeyboardKey.KEY_F11)){
        ToggleFullscreen();
    }

    if(IsKeyPressed(KeyboardKey.KEY_MINUS)){
        fontsizeOffset -= 1;
        goto RELOAD;
    }

    if (IsKeyPressed(KeyboardKey.KEY_F1) || ReloadFile)
    {
        ReloadFile = false;
        goto RELOAD;
    }

    if (IsKeyDown(KeyboardKey.KEY_F2) || dev)
    {
        if (currentText != null)
        {
            currentText.Now = currentText.Goal;
            currentText.Currsor = currentText.Goal.Length;
        }
    }

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

            case Opp.Kind.TypeNext:
                typeNext = true;
                break;

            case Opp.Kind.DevMode:
                dev = !dev;
                break;

            case Opp.Kind.Bg:
                bg = now.color;
                break;

            case Opp.Kind.WaitForClick:
                waitforclick = true;
                break;

            case Opp.Kind.Speed:
                textspeed = now.Speed;
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

            case Opp.Kind.CurrsorBottem:
                currsor.Y = GetScreenHeight() - (fontsize + fontsizeOffset);
                break;

            case Opp.Kind.Halt:
                goto FILE_OVER;


            case Opp.Kind.Text:
                faders.Add(currentText = new()
                {
                    Opp = now,
                    Goal = now.Text,
                    Fg = fg,
                    FontSize = fontsize + fontsizeOffset,
                    Position = currsor,
                    Font = font,
                    MoveLeftRight = nextLeftToRight,
                    TextSpeed = textspeed,
                    UserTypeThis = typeNext
                });

                typeNext = false;

                if (nextLeftToRight && leftrightopp != null)
                {
                    currentText.LeftPercent = leftrightopp.LeftPercent;
                    currentText.RightPercent = leftrightopp.RightPercent;
                }

                nextLeftToRight = false;


                var size = MeasureTextEx(currentText.Font, now.Text, (int)fontsize + fontsizeOffset, 1);
                currsor += size;
                currsor.X = 0;

                if (centerNext)
                {
                    if (!centeredFirst)
                    {
                        centeredFirst = true;
                        currentText.Position.Y = GetScreenHeight() / 2;
                    }

                    currentText.Position.X = (float)(GetScreenWidth() / 2.0 - (.5 * size.X));
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

    if (waitforclick && (IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT) || IsKeyDown(KeyboardKey.KEY_ENTER)))
        waitforclick = false;


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
        else if (line.StartsWith(";"))
        {
            continue;
        }
        else if (!string.IsNullOrWhiteSpace(line))
        {
            var textEffects = ParseTextEffects(line, out line);

            if (line == null) continue;

            ret.Add(new()
            {
                OpKind = Opp.Kind.Text,
                Text = line,
                TextEffects = textEffects
            });
        }
    }


    return ret;
}



List<TextEffect> ParseTextEffects(string line, out string? outline)
{
    var ret = new List<TextEffect>();

    Queue<char> str = new(line);

    string buff = "";
    string cmd = "";
    outline = "";
    int charno = 0;
    int startch = 0;

    bool inCommand = false;


    while (0 < str.Count)
    {
        char c = str.Dequeue();

        if (inCommand)
        {
            if (c == ']')
            {
                TextEffect tf = new();
                var cmdParts = buff.Split(" ");

                tf.Kind = cmdParts[0] switch
                {
                    "glitch" => TextEffect.KeKind.Glitch,
                };

                switch (tf.Kind)
                {

                    case TextEffect.KeKind.Glitch:
                        tf.GlitchWords = cmdParts[1..].ToList();
                        tf.CommandStartPos = startch;


                        StringBuilder newnow = new(outline);

                        for (int i = 0; i < tf.GlitchWords[0].Length; i++)
                        {
                            newnow.Append(tf.GlitchWords[0][i]);
                        }

                        outline = newnow.ToString();

                        int longest = tf.GlitchWords.Max(x => x.Length);
                        string s = ""; for (int i = 0; i < longest - tf.GlitchWords[0].Length; i++) s += " ";
                        outline += s;

                        break;
                }


                ret.Add(tf);
                buff = "";
                inCommand = false;
            }
            else
            {
                buff += c;
            }
        }
        else
        {
            charno += 1;
        }


        // [\ lets you type the "[" char in a file
        if (c == '[' && str.Peek() != '\\')
        {
            inCommand = true;
            startch = charno;
        }

        if (!inCommand && c != ']')
        {
            outline += c;
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
        "speed" => new() { OpKind = Opp.Kind.Speed, Speed = float.Parse(split[1]) },
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
        "currsorbottem" => new() { OpKind = Opp.Kind.CurrsorBottem },
        "halt" => new() { OpKind = Opp.Kind.Halt },
        "starthere" => new() { OpKind = Opp.Kind.StartHere },
        "type" => new() { OpKind = Opp.Kind.TypeNext },
        "dev" => new() { OpKind = Opp.Kind.DevMode },

        _ => new() { OpKind = Opp.Kind.Nothing },
    };
}


class TextEffect
{
    public enum KeKind
    {
        Glitch
    }


    public KeKind Kind;

    public List<string> GlitchWords;
    public int CommandStartPos;
    internal double GlitchTimer;
}

class Fader
{
    public string Goal;
    public string Now = "";
    public int Currsor = 0;
    public Vector2 Position;
    public Color Fg;
    public float FontSize;

    public double NextUpdateFadeTime = 0;
    public Font Font;
    public bool MoveLeftRight;
    public int MoveDirection = 1;
    public float MoveSpeed = 2;
    public float LeftPercent = 0;
    public float RightPercent = 1;
    public double TextSpeed = .02;
    public Opp Opp;
    public bool UserTypeThis;

    public bool IsFadded() => Goal.Length <= Currsor;

    public int LongestGlitchWordLength()
    {
        string longest = "";
        foreach (var item in Opp.TextEffects)
        {
            if (item.Kind == TextEffect.KeKind.Glitch)
            {
                foreach (var word in item.GlitchWords)
                {
                    if (longest.Length < word.Length)
                    {
                        longest = word;
                    }
                }
            }
        }

        return longest.Length;
    }

    public bool BlinkCursor = true;
    public double NextBlinkStateChange = 0;

    public void UpdateUserTypeThis()
    {
        if (GetTime() > NextBlinkStateChange)
        {
            NextBlinkStateChange = GetTime() + .25;
            BlinkCursor = !BlinkCursor;
        }


        if (!IsFadded())
        {
            char keyWeWant = char.ToLower(Goal[Currsor]);

            KeyboardKey key = KeyboardKey.KEY_NULL;
            do
            {
                key = GetKeyPressed_();
                char c = char.ToLower((char)key);

                if (c == keyWeWant)
                {
                    Currsor += 1;
                    Now = Goal[..Currsor];
                }

            } while (key != KeyboardKey.KEY_NULL);



        }


    }

    public void Update()
    {

        if (UserTypeThis)
        {
            UpdateUserTypeThis();
        }

        if (TextSpeed == 0 && !UserTypeThis)
        {
            Now = Goal;
            Currsor = Goal.Length;
        }

        if (!UserTypeThis && !IsFadded() && NextUpdateFadeTime < GetTime())
        {
            Currsor += 1;
            Now = Goal[..Currsor];
            NextUpdateFadeTime = GetTime() + TextSpeed;
        }

        foreach (var item in Opp.TextEffects)
        {
            if (Currsor > item.CommandStartPos + LongestGlitchWordLength())
            {
                // swap out that chunk of text 
                if (item.GlitchTimer < GetTime())
                {
                    item.GlitchTimer = GetTime() + .1;

                    string word = item.GlitchWords[GetRandomValue(0, item.GlitchWords.Count - 1)];

                    StringBuilder newnow = new(Now);

                    for (int i = 0; i < word.Length; i++)
                    {
                        newnow[i + (item.CommandStartPos - 1)] = word[i];
                    }

                    Now = newnow.ToString();
                }
            }

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
        if (UserTypeThis && BlinkCursor && !IsFadded())
        {
            var size = MeasureTextEx(Font, Now, FontSize, 1);
            var chsz = MeasureTextEx(Font, " ", FontSize, 1);


            DrawRectangleRec(new Rectangle(Position.X + size.X, Position.Y + chsz.Y - (chsz.Y / 8), chsz.X, chsz.Y / 8), Fg);
        }

        DrawTextEx(Font, Now, Position, FontSize, 1, Fg);


    }

}

class Opp
{
    public enum Kind { Nothing, Fg, Bg, Text, Delay, Clear, SetCursor, FontSize, Center, Left, StartTogeather, EndTogeather, Bold, BoldItalic, Normal, Italic, LeftToRight, WaitForClick, Speed, CurrsorBottem, Halt, StartHere, TypeNext, DevMode };
    public Kind OpKind;
    public Color color;
    public string Text;
    public Vector2 Currsor;
    public double Time;
    public float Size;
    public float LeftPercent, RightPercent;
    internal float Speed;
    internal List<TextEffect> TextEffects;
}

class TextOnScreen
{
    public string Text;
    public Color Fg;
    public Vector2 Position;
    public float FontSize;
}

