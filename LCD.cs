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
            float _surfaceFontSize;
            Color _surfaceScriptForegroundColor;
            float _surfaceTextPadding;
            bool _surfacePreserveAspectRatio;
            Vector2 _surfaceTextureSize;
            Vector2 _surfaceSurfaceSize;
            ContentType _surfaceContentType;
            string _surfaceScript;
            TextAlignment _surfaceAlignment;
            string _surfaceFont;
            byte _surfaceBackgroundAlpha;
            Color _surfaceBackgroundColor;
            Color _surfaceFontColor;

            // Sprite poperties
            SpriteType _spriteType;
            Vector2 _spritePosistion;
            Vector2 _spriteSize;
            Color _spriteColor;
            string _spriteData;
            string _spriteFontId;
            TextAlignment _spriteTextAlignment;
            float _spriteRotationOrScale;
            float _spriteRotation;
            float _spriteScale;
            #endregion

            // LCD Specifics
            Program _program;
            IMyTextSurface _surface;
            RectangleF _positionTopLeft;
            MySpriteDrawFrame _frame;
            Properties _properties;
            double _elapsedMs;
            //DateTime _previousTime;
            //Vector2 _posTL;
            Vector2 _posBLprev;
            Vector2 _posBL;
            float _currentLineHeight;
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
                _surface.ContentType = ContentType.SCRIPT;
            }

            public class Properties
            {
                int _element_count;
                public List<Property> _propertiesList;
                Program _program;

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
                    Program _program;
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
            /// Sets font
            /// </summary>
            public LCD SetFont(string fontId, float scale)
            {
                _spriteFontId = fontId;
                _spriteScale = scale;
                return this;
            }
            /// <summary>
            /// vect.ToString
            /// </summary>
            private string VString(Vector2 v)
            {
                return $"({v.X},{v.Y})";
            }
            /// <summary>
            /// Get height of current font and move to begining of next line 
            /// </summary>
            /// <returns></returns>
            public LCD NewLine()
            {
                _posBLprev = _posBL;
                //Vector2 size = TextSize("");
                //_currentLineHeight = size.Y; 
                _currentLineHeight = TextSize("").Y;
                _posBL = new Vector2(_positionTopLeft.X, _posBL.Y + _currentLineHeight);
                _program.Echo($"NewLine:{VString(_posBLprev)}->{VString(_posBL)}");
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
            private Vector2 TextSize(string text)
            {
                Vector2 size = _surface.MeasureStringInPixels(new StringBuilder(text), _spriteFontId, _spriteScale);
                _program.Echo($"Size of \"{text}\" is {VString(size)}");
                return size;
            }
            /// <summary>
            /// Add text at current cursor
            /// </summary>
            /// <param name="text"></param>
            /// <returns></returns>
            public LCD AddText(string text)
            {
                var props = _properties.addifnew();
                //_spriteScale = 1.0f;  // SetFont
                _spriteTextAlignment = TextAlignment.LEFT;
                //_spriteFontId = "White";  // SetFont
                _spriteData = text;
                Vector2 size = TextSize(text);
                var pos = _posBL - new Vector2(0, size.Y);
                _program.Echo($"placing {text} at BL {VString(_posBL)}");
                _frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = _spriteData,
                    Position = pos,
                    RotationOrScale = _spriteScale,
                    FontId = _spriteFontId,
                    //Size = new Vector2(1280, 80),
                    // Color = new Color(1.0f, 0.0f, 0.0f),
                    Alignment = _spriteTextAlignment
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
                    Position = _positionTopLeft.Center,
                    RotationOrScale = 0.0f,
                    Size = new Vector2(80, 80),
                    // Color = new Color(1.0f, 0.0f, 0.0f),
                    Alignment = TextAlignment.CENTER
                });
                return this;

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
            /// Draws any chained sprites
            /// </summary>
            /// <returns></returns>
            public LCD End()
            {
                _frame.Dispose();
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
