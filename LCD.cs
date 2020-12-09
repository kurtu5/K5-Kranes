using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class LCD  // : MyGridProgram
        {
            #region IMyTextSurface and Sprite properties

            // IMyTextSurface properties
            private float _surfaceFontSize;  // Does nothing in SCRIPT mode
            private Color _surfaceScriptForegroundColor;  // nada
            private float _surfaceTextPadding;  //nada
            private bool _surfacePreserveAspectRatio;
            private Vector2 _surfaceTextureSize;
            private Vector2 _surfaceSurfaceSize;
            private ContentType _surfaceContentType;
            private string _surfaceScript;
            private TextAlignment _surfaceAlignment;
            private string _surfaceFont;
            private byte _surfaceBackgroundAlpha;
            private Color _surfaceBackgroundColor;
            private Color _surfaceFontColor;

            // Sprite poperties
            private SpriteType _spriteType;

            private Vector2 _spritePosistion;
            private Vector2 _spriteSize;
            private Color _spriteColor;
            private string _spriteData;
            private string _spriteFontId;
            private TextAlignment _spriteTextAlignment;
            private float _spriteRotationOrScale;
            private float _spriteRotation;
            private float _spriteFontScale;

            #endregion IMyTextSurface and Sprite properties

            // LCD Specifics
            private Program _program;

            private IMyTextSurface _surface;
            private RectangleF _positionTopLeft;
            private MySpriteDrawFrame _frame;
            private Properties _properties;
            private double _elapsedMs;

            //DateTime _previousTime;
            //Vector2 _posTL;
            private Vector2 _posBLprev;

            private Vector2 _posBL;
            private float _currentLineHeight;

            // Percentage of text padding done by MySprite
            private float _textPctPaddingTop;
            private float _textPctPaddingBot;

            public const byte TEXT = 1 << 0;
            public const byte SPRITE = 1 << 1;
            public const byte BOTH = TEXT | SPRITE;
            public const byte NEITHER = 1 << 2;



            public LCD(Program program, IMyTextSurface surface)
            {
                _program = program;
                _properties = new Properties(_program);
                _animationCurrentFrame = 0;
                _animationPercentage = 0;
                _surface = surface;
                // Do stuff
                _elapsedMs = 0;
                // Calculate the viewport offset by centering the surface size onto the texture size
                _positionTopLeft = new RectangleF(
                    (_surface.TextureSize - _surface.SurfaceSize) / 2f,
                    _surface.SurfaceSize
                );
                //_posTL = _positionTopLeft.Position;
                _posBL = _positionTopLeft.Position;
                _posBLprev = _positionTopLeft.Position;

                // test surface settables
                _surface.ContentType = ContentType.SCRIPT;
                //_surface.FontSize = 21.0f;    // nada
                 _surface.ScriptForegroundColor = Color.BlueViolet;  // nada
                // _surface.TextPadding = 20.0f;  //nada
                // _surface.PreserveAspectRatio = true;  //nada
                //_surface.TextureSize;  // get
                // _surface.SurfaceSize; // get
                //_surface.Script = 
                //_surface.Alignment = TextAlignment.RIGHT;
                //_surface.Font = "White";
                //_surface.BackgroundAlpha = byte....;
                _surface.ScriptBackgroundColor = Color.Red;
                //_surface.BackgroundColor = Color.DarkOrchid;
                //_surface.FontColor = Color.AliceBlue;

                // Padding percentages calculated from screenshot
                _textPctPaddingTop = 175.0f / 725.0f; // top px / total height 0.24
                _textPctPaddingBot = 100.0f / 725.0f; // bottom px / total height 0.13



            }

            public class Properties
            {
                private int _element_count;
                public List<Property> _propertiesList;
                private Program _program;

                public Properties(Program program)
                {
                    _element_count = 0;
                    _program = program;
                    //_program.Echo("Properties()");
                    _propertiesList = new List<Property>();
                }

                public void resetCount()
                {
                    _element_count = 0;
                }

                public Property addifnew()
                {
                    _element_count++;
                    Property property = _propertiesList.ElementAtOrDefault(_element_count - 1);
                    if (property == null)
                    {
                        _propertiesList.Add(new Property(_program, this));
                    }
                    return _propertiesList.ElementAtOrDefault(_element_count - 1);
                }

                public class Property
                {
                    public int animation_frame_index { get; set; }
                    public double animation_ms_until_update { get; set; }
                    private Program _program;

                    //Properties _properties;
                    public Property(Program program, Properties properties)
                    {
                        //_properties = properties;
                        _program = program;
                        animation_frame_index = 0;
                        animation_ms_until_update = 0;
                        // _program.Echo("property constructor called");
                    }
                }
            }

            /// <summary>
            /// Resets the display to allow addition of new sprites
            /// </summary>
            /// <returns></returns>
            public LCD Start()
            {

                _program.Echo($"TL={VString(_positionTopLeft.Position)}");
                _properties.resetCount();
                _posBL = _positionTopLeft.Position; // bottom of current line is off screen
                _frame = _surface.DrawFrame();  // should i use new?
                return this;
            }
            
            /// <summary>
            /// Trim default MySprit text padding.
            /// Top  is 24%
            /// Bottom is 14%
            /// </summary>
            /// <param name="top"></param>
            /// <param name="bottom"></param>
            /// <returns></returns>
            /// <example>TrimPadding(24,14) Will remove all text padding</example>
            public LCD TrimPadding(int top, int bottom)
            {
                _textPctPaddingTop = (float)(top / 100.0f);
                _textPctPaddingBot = (float)(bottom / 100.0f);
                return this;
            }

            /// <summary>
            /// Draws any chained sprites
            /// </summary>
            /// <returns></returns>
            public LCD End()
            {
                _frame.Dispose();
                return this;
            }

            /// <summary>
            /// Sets font
            /// </summary>
            public LCD Font(string fontId)
            {
                _spriteFontId = fontId;
                return this;
            }

            /// <summary>
            /// Sets font size
            /// </summary>
            public LCD FontSize(float scale)
            {
                _spriteFontScale = scale;
                return this;
            }

            public string binary(Byte b)
            {
                return Convert.ToString(b, 2).PadLeft(8, '0');
            }
            /// <summary>
            /// Pick the current highest size between font or sprite and 
            /// move cursor down that much to the next line
            /// </summary>
            /// <returns></returns>
            public LCD NewLine(byte flag = BOTH)
            {
                float height = 0.0f;
                _program.Echo($"check flag {binary(flag)} & {binary(TEXT)}");
                if ((flag & TEXT) == TEXT)
                {
                    height = PaddedTextSize("").Y;
                    // Remove padding to get actual height
                    height *= (1.0f - (_textPctPaddingTop + _textPctPaddingBot));
                    _program.Echo($"Text height={height} ");
                }
                if ((flag & SPRITE) == SPRITE)
                {
                    _program.Echo($"vs Sprite height={_spriteSize.Y}");
                    if (height < _spriteSize.Y)
                        height = _spriteSize.Y;
                }
                _currentLineHeight = height;
                // if NEITHER  height is alreay zero.
                _posBLprev = _posBL;
                _posBL = new Vector2(_positionTopLeft.X, _posBL.Y + height);
                _program.Echo($"New Line:{VString(_posBLprev)}->{VString(_posBL)}");
                return this;
            }
            /// <summary>
            /// Simply go back to line begning and go down by height
            /// </summary>
            public LCD NewLine(float height)
            {
                _posBL.X = _positionTopLeft.X;
                _posBL.Y += height;
                return this;
            }

            /// <summary>
            /// Moves the cursor right based on size
            /// </summary>
            private LCD CursorRight(Vector2 size)
            {
                _posBLprev = _posBL;
                _posBL += new Vector2(size.X, 0);
                _program.Echo($"CursorRight:{VString(_posBLprev)}->{VString(_posBL)}");
                return this;
            }

            /// <summary>
            /// size of text
            /// </summary>
            private Vector2 PaddedTextSize(string text)
            {
                Vector2 size = _surface.MeasureStringInPixels(new StringBuilder(text), _spriteFontId, _spriteFontScale);
                _program.Echo($"Size of \"{text}\" is {VString(size)}");
                return size;
            }
            /// <summary>
            /// vect.ToString
            /// </summary>
            private string VString(Vector2 v)
            {
                return $"({v.X:F0},{v.Y:F0})";
            }

            public LCD SetPos(float x, float y)
            {
                _posBL.X = x;
                _posBL.Y = y;
                return this;
            }

            public LCD SpriteSize(float w, float h)
            {
                _spriteSize.X = w;
                _spriteSize.Y = h;
                return this;
            }

            public LCD AddSprite(string sprite)
            {

                var props = _properties.addifnew();
                //_spriteScale = 1.0f;  // SetFont
                _spriteTextAlignment = TextAlignment.LEFT;
                //_spriteFontId = "White";  // SetFont
                _spriteData = sprite;
                Vector2 size = _spriteSize;
                var pos = _posBL - new Vector2(0, size.Y);
                _program.Echo($"AddSprite:{sprite} POS:{VString(pos)} BL:{VString(_posBL)}");
                _frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = _spriteData,
                    Position = pos,
                    RotationOrScale = 0.0f,
                    //FontId = _spriteFontId,
                    Size = _spriteSize,
                    // Color = new Color(1.0f, 0.0f, 0.0f),
                    Alignment = _spriteTextAlignment
                });
                return this;
            }
            /// <summary>
            /// Add text at current cursor
            /// </summary>
            /// <param name="text"></param>
            /// <returns></returns>
            public LCD AddText(string text)
            {
                var props = _properties.addifnew();
                _spriteTextAlignment = TextAlignment.LEFT;
                _spriteData = text;

                // Deal with wonky padding
                Vector2 size = PaddedTextSize(text);
                var pos = _posBL - new Vector2(0, size.Y * (1.0f - _textPctPaddingBot));
                // actual size without padding
                size.Y *= (1.0f - (_textPctPaddingTop + _textPctPaddingBot));

                _program.Echo($"AddText:{text} POS:{VString(pos)} BL:{VString(_posBL)}");
                _frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = _spriteData,
                    Position = pos,
                    RotationOrScale = _spriteFontScale,
                    FontId = _spriteFontId,
                    //Size = new Vector2(1280, 80),
                    // Color = new Color(1.0f, 0.0f, 0.0f),
                    Alignment = _spriteTextAlignment
                });

                //var pos2 = _posBL - new Vector2(0, size.Y / 2.0f);
                if (true) _frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareHollow",
                    Position = _posBL - new Vector2(0, size.Y / 2.0f),
                    RotationOrScale = 0.0f,
                    //FontId = _spriteFontId,
                    Size = size,
                    Color = new Color(1.0f, 0.0f, 0.0f),
                    Alignment = TextAlignment.LEFT
                });
                CursorRight(size);
                return this;
            }

            public LCD AddSpriteAnimation(string[] spriteList, float seconds)
            {
                var props = _properties.addifnew();
                props.animation_ms_until_update -= _elapsedMs;
                if (props.animation_ms_until_update < 0)
                // if ( (props.animation_ms_until_update -= _elapsed_time) < 0)
                {
                    props.animation_ms_until_update = (double)(seconds * 1000.0f);
                    props.animation_frame_index++;
                    props.animation_frame_index %= spriteList.Length;
                }

                _frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = spriteList[props.animation_frame_index],
                    Position = new Vector2(_posBL.X, _posBL.Y - (_spriteSize.Y / 2.0f)),
                    RotationOrScale = 0.0f,
                    Size = _spriteSize,
                    // Color = new Color(1.0f, 0.0f, 0.0f),
                    Alignment = TextAlignment.LEFT
                });
                return this;
            }

            /// <summary>
            /// Use markup to generate chained sprites
            /// </summary>
            /// <param name="mu"></param>
            public LCD Form(string mu)

            {
                return this;
            }

            private int _animationCurrentFrame;
            private int _animationEndFrame;
            private int _animationPercentage;

            /// <summary>
            /// sets total frames, advances frame counters and stores time elapsed
            /// </summary>
            /// <param name="frameCount"></param>
            /// <returns></returns>
            public LCD Animate(int frameCount)
            {
                _program.Echo($"SPRITEHIEGHTATSTART={_spriteSize.Y}");

                _animationEndFrame = frameCount;
                _elapsedMs = _program.Runtime.TimeSinceLastRun.TotalMilliseconds;
                _animationCurrentFrame++;
                _animationCurrentFrame %= _animationEndFrame;
                _animationPercentage = (_animationCurrentFrame * 100) / _animationEndFrame;
                return this;
            }
        }
    }
}