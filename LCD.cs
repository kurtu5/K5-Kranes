﻿using Sandbox.Game.EntityComponents;
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
            /// <summary>
            /// Set and access previous sprite properties after subsequent iterations.
            /// </summary>
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

            // This class is overkill.  Sprites can only really use scriptbackground
            // So just reference it directly
            public class LDCSurface
            {
                // IMyTextSurface properties
                public float _surfaceFontSize;  // Does nothing in SCRIPT mode
                public Color _surfaceScriptForegroundColor;  // nada
                public float _surfaceTextPadding;  //nada
                public bool _surfacePreserveAspectRatio;
                public Vector2 _surfaceTextureSize;
                public Vector2 _surfaceSurfaceSize;
                public ContentType _surfaceContentType;
                public string _surfaceScript;
                public TextAlignment _surfaceAlignment;
                public string _surfaceFont;
                public byte _surfaceBackgroundAlpha;
                public Color _surfaceBackgroundColor;
                public Color _surfaceFontColor;
            }
            public class LCDSprite
            {
                // Sprite poperties
                private SpriteType type;
                public Vector2 posistion;
                public Vector2 size;
                public Color color;
                public string data;
                public string fontId;
                public TextAlignment textAlignment;
                public float rotationOrScale;
                public float rotation;
                public float fontScale;

                public LCDSprite()
                {
                    type = SpriteType.TEXTURE;
                    posistion = new Vector2();
                    size = new Vector2(0, 0);
                    color = new Color();
                    data = "";
                    fontId = "";
                    textAlignment = TextAlignment.LEFT;
                    rotationOrScale = 0.0f;
                    rotation = 0.0f;
                    fontScale = 1.0f;
                }
            }

            // LCD Specifics
            private Program _program;
            private IMyTextSurface _surface;
            public LCDSprite _sprite;
            private MySpriteDrawFrame _frame;
            private double _elapsedMs;
            private RectangleF _viewport;
            public bool _debug;

            // Line positioning
            private Vector2 _posTL;
            private Vector2 _posBLprev;
            private Vector2 _posBL;
            private float _currentLineHeight;
            // Newline flags
            //public const byte TEXT = 1 << 0;
            //public const byte SPRITE = 1 << 1;
            //public const byte BOTH = TEXT | SPRITE;
            //public const byte NEITHER = 1 << 2;

            public enum NewLineCaculation
            {
                TEXT = 1 << 0,
                SPRITE = 1 << 1,
                BOTH = TEXT | SPRITE,
                NEITHER = 1 << 2
            };

            // Animation support
            private Properties _properties;
            private int _animationCurrentFrame;
            private int _animationEndFrame;
            private int _animationPercentage;

            // Percentage of text padding done by MySprite
            private float _textPctPaddingTop;
            private float _textPctPaddingBot;

            public LCD(Program program, IMyTextSurface surface)
            {

                _program = program;
                _properties = new Properties(_program);
                _sprite = new LCDSprite();
                _surface = surface;
                _debug = false;  // Highlight things with redlines or boxes.

                // Animation
                _animationCurrentFrame = 0;
                _animationPercentage = 0;
                _elapsedMs = 0;

                // Set up the surface
                _surface.ContentType = ContentType.SCRIPT;
                _surface.ScriptBackgroundColor = Color.Black;


                // Calculate the viewport offset by centering the surface size onto the texture size
                _viewport = new RectangleF(
                    (_surface.TextureSize - _surface.SurfaceSize) / 2f,
                    _surface.SurfaceSize
                );
                _posTL = _viewport.Position;
                _posBL = _viewport.Position;
                _posBLprev = _viewport.Position;

                // Padding percentages calculated from screenshot
                //_textPctPaddingTop = 175.0f / 725.0f; // top px / total height 0.24
                //_textPctPaddingBot = 100.0f / 725.f;0 // bottom px / total height 0.13
                // Actually looks more like 18% top and 11% bottom
                _textPctPaddingTop = 0.18f;
                _textPctPaddingBot = 0.11f;
            }
            /// <summary>
            /// Resets the display to allow addition of new sprites
            /// </summary>
            /// <returns></returns>
            public LCD Start()
            {
                //_program.Echo($"TL={VString(_posTL)}");
                // Since we are reusing objects, make sure to reset them here
                _posBL = _posTL; // bottom of current line is off screen
                _frame = _surface.DrawFrame();  // should i use new?

                _properties.resetCount();
                _sprite.data = "";
                _sprite.fontId = "White";
                _sprite.fontScale = 1.0f;
                _sprite.posistion = new Vector2(0, 0);
                _sprite.rotation = 0.0f;
                _sprite.size = new Vector2(0, 0);
                _sprite.textAlignment = TextAlignment.LEFT;

                return this;
            }

            /// <summary>
            /// Trim default MySprit text padding.
            /// Top  is 18%
            /// Bottom is 11%
            /// </summary>
            /// <param name="top"></param>
            /// <param name="bottom"></param>
            /// <returns></returns>
            /// <example>TrimPadding(18,11) Will remove all text padding</example>
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
                _sprite.fontId = fontId;
                return this;
            }

            /// <summary>
            /// Sets font size
            /// </summary>
            public LCD FontSize(float scale)
            {
                _sprite.fontScale = scale;
                return this;
            }

            // For debugging.... remove
            public string binary(Byte b)
            {
                return Convert.ToString(b, 2).PadLeft(8, '0');
            }
            /// <summary>
            /// Pick the current highest size between font or sprite and 
            /// move cursor down that much to the next line
            /// </summary>
            /// <returns></returns>
            public LCD NewLine(NewLineCaculation flag = NewLineCaculation.BOTH)
            {
                float height = 0.0f;
                if ((flag & NewLineCaculation.TEXT) == NewLineCaculation.TEXT)
                {
                    height = PaddedTextSize("").Y;
                    // Remove padding to get actual height
                    height *= (1.0f - (_textPctPaddingTop + _textPctPaddingBot));
                    //_program.Echo($"Text height={height} ");
                }
                if ((flag & NewLineCaculation.SPRITE) == NewLineCaculation.SPRITE)
                {
                    //_program.Echo($"vs Sprite height={_sprite.size.Y}");
                    if (height < _sprite.size.Y)
                        height = _sprite.size.Y;
                }
                _currentLineHeight = height;
                // if NEITHER  height is alreay zero.
                _posBLprev = _posBL;
                _posBL = new Vector2(_posTL.X, _posBL.Y + height);
                //_program.Echo($"New Line:{VString(_posBLprev)}->{VString(_posBL)}");

                var stroke = 2;
                if (_debug)
                    _frame.Add(new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "SquareSimple",
                        Position = _posBL,
                        RotationOrScale = 0.0f,
                        Size = new Vector2(_viewport.Width, stroke),
                        Color = Color.Aqua,
                        Alignment = TextAlignment.LEFT
                    }
                        );
                return this;
            }


            /// <summary>
            /// Moves the cursor right based on size
            /// </summary>
            private LCD CursorRight(Vector2 size)
            {
                _posBLprev = _posBL;
                _posBL += new Vector2(size.X, 0);
                //_program.Echo($"CursorRight:{VString(_posBLprev)}->{VString(_posBL)}");
                return this;
            }

            /// <summary>
            /// Size of text at the cuurent font and fontsize.
            /// </summary>
            private Vector2 PaddedTextSize(string text)
            {
                Vector2 size = _surface.MeasureStringInPixels(new StringBuilder(text), _sprite.fontId, _sprite.fontScale);
                //_program.Echo($"Size of \"{text}\" is {VString(size)}");
                return size;
            }
            public LCD SetPos(float x, float y)
            {
                _posBL.X = x;
                _posBL.Y = y;
                return this;
            }

            public LCD SpriteSize(float w, float h)
            {

                _sprite.size.X = w;
                _sprite.size.Y = h;
                return this;
            }

            public LCD AddSprite(string sprite)
            {

                var props = _properties.addifnew();
                //_spriteScale = 1.0f;  // SetFont
                _sprite.textAlignment = TextAlignment.LEFT;
                //_spriteFontId = "White";  // SetFont
                _sprite.data = sprite;
                Vector2 size = _sprite.size;
                var pos = _posBL - new Vector2(0, size.Y);
                //_program.Echo($"AddSprite:{sprite} POS:{VString(pos)} BL:{VString(_posBL)}");
                _frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = _sprite.data,
                    Position = pos,
                    RotationOrScale = 0.0f,
                    //FontId = _spriteFontId,
                    Size = _sprite.size,
                    // Color = new Color(1.0f, 0.0f, 0.0f),
                    Alignment = _sprite.textAlignment
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
                _sprite.textAlignment = TextAlignment.LEFT;
                _sprite.data = text;

                // Deal with wonky padding
                Vector2 size = PaddedTextSize(text);
                var pos = _posBL - new Vector2(0, size.Y * (1.0f - _textPctPaddingBot));
                // actual size without padding
                size.Y *= (1.0f - (_textPctPaddingTop + _textPctPaddingBot));

                //_program.Echo($"AddText:{text} POS:{VString(pos)} BL:{VString(_posBL)}");
                _frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = _sprite.data,
                    Position = pos,
                    RotationOrScale = _sprite.fontScale,
                    FontId = _sprite.fontId,
                    //Size = new Vector2(1280, 80),
                    // Color = new Color(1.0f, 0.0f, 0.0f),
                    Alignment = _sprite.textAlignment
                });

                //var pos2 = _posBL - new Vector2(0, size.Y / 2.0f);
                if (_debug)
                    DrawBox(_posBL - new Vector2(0, size.Y), size, 3);

                if (false) _frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareHollow",
                    Position = _posBL - new Vector2(0, size.Y / 2.0f),
                    RotationOrScale = 0.0f,
                    Size = size,
                    Color = Color.Red,
                    Alignment = TextAlignment.LEFT
                });
                CursorRight(size);
                return this;
            }

            /// <summary>
            /// Add marquee text at current cursor
            /// </summary>
            /// <param name="text"></param>
            /// <returns></returns>
            public LCD AddMarquee(string text, float seconds)
            {
                var props = _properties.addifnew();
                // Put this in properties for efficiency...
                StringBuilder textSubstring = new StringBuilder();
                if ((props.animation_ms_until_update -= _elapsedMs) < 0)
                {
                    props.animation_ms_until_update = (double)(seconds * 1000.0f);
                    // Lets substr using this index
                    props.animation_frame_index += 1;
                    props.animation_frame_index %= text.Length;
                }
                int i = props.animation_frame_index;
                _program.Echo($"substr({i},{text.Length - i})");
                int minWidth = 100;
                // How many chars can approximatly fit
                // var textsize = PaddedTextSize("asdflkasdopd09asd09asdfjkasdjklalkdfjladjakljdlkadjk");
                // var textwidth = _viewport.Width;
                // int fittableChars = 123;
                textSubstring.Append(text.Substring(i, text.Length - i));
                var newwidth = textSubstring.Length;
                var pad = minWidth - newwidth;
                if (pad > 0)
                    textSubstring.Append(text.Substring(0, (pad < text.Length ) ? pad : text.Length ));

                _sprite.textAlignment = TextAlignment.LEFT;
                _sprite.data = textSubstring.ToString();

                // Deal with wonky padding
                Vector2 size = PaddedTextSize(text);
                var pos = _posBL - new Vector2(0, size.Y * (1.0f - _textPctPaddingBot));
                // actual size without padding
                size.Y *= (1.0f - (_textPctPaddingTop + _textPctPaddingBot));

                //_program.Echo($"AddText:{text} POS:{VString(pos)} BL:{VString(_posBL)}");
                _frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = _sprite.data,
                    Position = pos,
                    RotationOrScale = _sprite.fontScale,
                    FontId = _sprite.fontId,
                    //Size = new Vector2(1280, 80),
                    // Color = new Color(1.0f, 0.0f, 0.0f),
                    Alignment = _sprite.textAlignment
                });

                //var pos2 = _posBL - new Vector2(0, size.Y / 2.0f);
                if (_debug)
                    DrawBox(_posBL - new Vector2(0, size.Y), size, 3);

                if (false) _frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareHollow",
                    Position = _posBL - new Vector2(0, size.Y / 2.0f),
                    RotationOrScale = 0.0f,
                    Size = size,
                    Color = Color.Red,
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
                    Position = new Vector2(_posBL.X, _posBL.Y - (_sprite.size.Y / 2.0f)),
                    RotationOrScale = 0.0f,
                    Size = _sprite.size,
                    // Color = new Color(1.0f, 0.0f, 0.0f),
                    Alignment = TextAlignment.LEFT
                });
                return this;
            }

            // Primatives
            public LCD SetColor(Color color)
            {
                _sprite.color = color;
                return this;
            }

            // Probably need to rethink this one...
            public LCD DrawLine(Vector2 pos, Vector2 size, float stroke = 1.0f)
            {
                // Top
                _frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = new Vector2(pos.X, pos.Y),
                    RotationOrScale = 0.0f,
                    Size = new Vector2(size.X, size.Y),
                    Color = _sprite.color,
                    Alignment = TextAlignment.LEFT
                });
                return this;
            }

            // Really only using this for debugging now...
            public LCD DrawBox(Vector2 pos, Vector2 size, float stroke = 1.0f)
            {
                Color[] colors = { Color.Red, Color.Green, Color.Yellow, Color.Orange };
                //Color[] colors = { Color.Red, Color.Red, Color.Red, Color.Red };
                float half = stroke / 2.0f;
                string data;
                data = "SquareSimple";
                //data = "SquareHollow";
                // Top
                _frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = data,
                    Position = new Vector2(pos.X, pos.Y + half),
                    RotationOrScale = 0.0f,
                    Size = new Vector2(size.X, stroke),
                    Color = colors[0],
                    Alignment = TextAlignment.LEFT
                });
                // Bottom
                _frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = data,
                    Position = new Vector2(pos.X, pos.Y + size.Y - half),
                    RotationOrScale = 0.0f,
                    Size = new Vector2(size.X, stroke),
                    Color = colors[1],
                    Alignment = TextAlignment.LEFT
                });

                // Left
                _frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = data,
                    Position = new Vector2(pos.X, pos.Y + (size.Y / 2)),
                    RotationOrScale = 0.0f,
                    Size = new Vector2(stroke, size.Y),
                    Color = colors[2],
                    Alignment = TextAlignment.LEFT
                });
                // Right
                _frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = data,
                    Position = new Vector2(pos.X + size.X - stroke, pos.Y + (size.Y / 2)),
                    RotationOrScale = 0.0f,
                    Size = new Vector2(stroke, size.Y),
                    Color = colors[3],
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

            /// <summary>
            /// vect.ToString
            /// </summary>
            private string VString(Vector2 v)
            {
                return $"({v.X:F0},{v.Y:F0})";
            }


            /// <summary>
            /// sets total frames, advances frame counters and stores time elapsed
            /// </summary>
            /// <param name="frameCount"></param>
            /// <returns></returns>
            public LCD Animate(int frameCount)
            {

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