using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public class AnimationWindowExtension : EditorWindow {
    public static AudioClip lastAudioClip;
    public static AudioClip activeAudioClip;
    bool isPlaying;

    static Texture2D waveform1;
    static Texture2D waveform2;
    static Rect waveform1Rect;
    static Rect waveform2Rect;

    static Vector2 lineStart;
    static Vector2 lineEnd;

    static Type AnimationWindowClass;
    static Type AudioUtilClass;
    static Assembly unityEditorAssembly;
    static EditorWindow animationWindow;

    static System.Single lastTime;

    [MenuItem ("Window/Animation Audio Sync Window")]
    static void Load() {
        /*AnimationWindowExtension window =*/ AnimationWindowExtension.GetWindowWithRect<AnimationWindowExtension>( new Rect(100, 100, 200, 200 ) );

        Initialize();
    }

    static void Initialize() {
        unityEditorAssembly = typeof(EditorWindow).Assembly;
        AnimationWindowClass = unityEditorAssembly.GetType("UnityEditor.AnimationWindow");
        animationWindow = EditorWindow.GetWindow( AnimationWindowClass );

        AudioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
    }

    void Update() {
        if ( AnimationWindowClass == null ) {
            Initialize();
        }

        MethodInfo get_time = AnimationWindowClass.GetMethod( "get_time", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, new System.Type[] {  }, null );
        System.Single time = (System.Single)get_time.Invoke( animationWindow, new System.Object[] { } ); 

        if ( lastTime != time ) {
            MethodInfo GetFrequency = AudioUtilClass.GetMethod( "GetFrequency", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, new System.Type[] { typeof(UnityEngine.AudioClip) }, null );
            System.Int32 frequency = (System.Int32)GetFrequency.Invoke( null, new System.Object[] { activeAudioClip } );
            System.Int32 samples = (System.Int32)(frequency * time);

            // Set sample position if audio is playing
            if ( isPlaying ) {
                MethodInfo SetClipSamplePosition = AudioUtilClass.GetMethod( "SetClipSamplePosition", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, new System.Type[] { typeof(UnityEngine.AudioClip), typeof(System.Int32) }, null );
                SetClipSamplePosition.Invoke( null, new System.Object[] { activeAudioClip, samples } );
            }


            // Set the line points and Repaint
            float percent = (float)samples / (float)activeAudioClip.samples;
            if ( percent <= 1.0f ) {
                float selectionPosition = ( percent * waveform1Rect.width ) + waveform1Rect.x;

                if ( selectionPosition != 0 ) {
                    lineStart = new Vector2( selectionPosition, waveform1Rect.y );
                    if ( waveform2 != null ) {
                        lineEnd = new Vector2( selectionPosition, waveform2Rect.y + waveform2Rect.height );
                    } else {
                        lineEnd = new Vector2( selectionPosition, waveform1Rect.y + waveform1Rect.height );
                    }
                    Repaint();
                }
            }

            lastTime = time;
        }
    }

    void Play() {
        animationWindow = EditorWindow.GetWindow( AnimationWindowClass );
        if ( animationWindow != null ) {
            MethodInfo Play = AnimationWindowClass.GetMethod( "Play", 
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
                null,
                new System.Type[] {},
                null
            );

            if ( Play != null ) {
                Play.Invoke( animationWindow, new System.Object[] {} );
            }

            MethodInfo get_time = AnimationWindowClass.GetMethod( "get_time", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, new System.Type[] {  }, null );
            System.Single time = (System.Single)get_time.Invoke( animationWindow, new System.Object[] { } ); 

            MethodInfo GetFrequency = AudioUtilClass.GetMethod( "GetFrequency", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, new System.Type[] { typeof(UnityEngine.AudioClip) }, null );
            System.Int32 frequency = (System.Int32)GetFrequency.Invoke( null, new System.Object[] { activeAudioClip } );

            System.Int32 samples = (System.Int32)(frequency * time);

            MethodInfo PlayClip = AudioUtilClass.GetMethod( "PlayClip", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, new System.Type[] { typeof(UnityEngine.AudioClip), typeof(System.Int32), typeof(System.Boolean),  }, null );
            PlayClip.Invoke( null, new System.Object[] { activeAudioClip, 0, true } ); 

            if ( samples < activeAudioClip.samples && samples > 0 ) {
                MethodInfo SetClipSamplePosition = AudioUtilClass.GetMethod( "SetClipSamplePosition", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, new System.Type[] { typeof(UnityEngine.AudioClip), typeof(System.Int32) }, null );
                SetClipSamplePosition.Invoke( null, new System.Object[] { activeAudioClip, samples } );
            }

            isPlaying = true;
        }
    }

    void Stop() {
        animationWindow = EditorWindow.GetWindow( AnimationWindowClass );
        if ( animationWindow != null ) {
            MethodInfo Stop = AnimationWindowClass.GetMethod( "Stop", 
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
                null,
                new System.Type[] {},
                null
            );

            if ( Stop != null ) {
                Stop.Invoke( animationWindow, new System.Object[] {} );
            }
        }

        MethodInfo StopClip = AudioUtilClass.GetMethod( "StopClip", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, new System.Type[] { typeof(UnityEngine.AudioClip),  }, null );
        StopClip.Invoke( null, new System.Object[] { activeAudioClip } ); 

        isPlaying = false;
    }

    void OnGUI() {
        activeAudioClip = EditorGUILayout.ObjectField( "Clip", activeAudioClip, typeof(AudioClip), false ) as AudioClip;
        if ( GUILayout.Button( "Play" ) ) {
            Play();
        }

        if ( GUILayout.Button( "Stop" ) ) {
            Stop();
        }


        if ( activeAudioClip != null ) {

            // Did we change audio clips?
            if ( lastAudioClip != activeAudioClip ) {
                DrawWaveforms();
                lastAudioClip = activeAudioClip;

                Debug.Log("loaded new audio clip");
            }

            if ( waveform1 != null ) {
                waveform1Rect = new Rect( 10, 100, position.width - 20, 200 );
                GUI.Box( waveform1Rect, "Channel 1" );
                GUI.DrawTexture( waveform1Rect, waveform1 );
                GUILayout.Space( 200 );
            }

            if ( waveform2 != null ) {
                waveform2Rect = new Rect( 10, 300, position.width - 20, 200 );
                GUI.Box( waveform2Rect, "Channel 2" );
                GUI.DrawTexture( waveform2Rect, waveform2 );
                GUILayout.Space( 210 );
            }

            GUIHelper.DrawLine( lineStart, lineEnd, Color.white );
        }
    }



    void DrawWaveforms() {
        if ( AudioUtilClass == null ) {
            Initialize();
        }

        String path = AssetDatabase.GetAssetPath( activeAudioClip );
        AudioImporter importer = AssetImporter.GetAtPath( path ) as AudioImporter;

        MethodInfo GetWaveform = AudioUtilClass.GetMethod( "GetWaveForm", 
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
            null,
            new System.Type[] {
                typeof( AudioClip ),
                typeof( UnityEditor.AudioImporter ),
                typeof( System.Int32 ),
                typeof( System.Single ),
                typeof( System.Single )
            },
            null
        );

        if ( GetWaveform != null ) {
            waveform1 = GetWaveform.Invoke( null, new System.Object[] { activeAudioClip, importer, 0, position.width, 200 } ) as Texture2D;
            if ( activeAudioClip.channels == 2 ) {
                waveform2 = GetWaveform.Invoke( null, new System.Object[] { activeAudioClip, importer, 1, position.width, 200 } ) as Texture2D;
            } else {
                waveform2 = null;
            }
        }
    }
}


public class GUIHelper
{
    protected static bool clippingEnabled;
    protected static Rect clippingBounds;
    protected static Material lineMaterial;
    /* @ Credit: "http://cs-people.bu.edu/jalon/cs480/Oct11Lab/clip.c" */

    protected static bool clip_test(float p, float q, ref float u1, ref float u2) {
        float r;
        bool retval = true;
        if ( p < 0.0 ) {

            r = q / p;

            if ( r > u2 ) {
                retval = false;
            } else if ( r > u1 ) {
                u1 = r;
            }

        } else if ( p > 0.0 ) {

            r = q / p;

            if ( r < u1 ) {
                retval = false;
            } else if ( r < u2 ) {
                u2 = r;
            }

        } else {

            if ( q < 0.0 ) {
                retval = false;
            }

        }
        return retval;
    }

    protected static bool segment_rect_intersection(Rect bounds, ref Vector2 p1, ref Vector2 p2) {
        float u1 = 0.0f, u2 = 1.0f, dx = p2.x - p1.x, dy;

        if (clip_test(-dx, p1.x - bounds.xMin, ref u1, ref u2)) {

            if (clip_test(dx, bounds.xMax - p1.x, ref u1, ref u2)) {

                dy = p2.y - p1.y;

                if (clip_test(-dy, p1.y - bounds.yMin, ref u1, ref u2)) {

                    if (clip_test(dy, bounds.yMax - p1.y, ref u1, ref u2)) {

                        if (u2 < 1.0) {
                            p2.x = p1.x + u2 * dx;
                            p2.y = p1.y + u2 * dy;
                        }

                        if (u1 > 0.0) {
                            p1.x += u1 * dx;
                            p1.y += u1 * dy;
                        }

                        return true;
                    }
                }
            }
        }
        return false;
    }

    public static void BeginGroup( Rect position ) {
        clippingEnabled = true;
        clippingBounds = new Rect( 0, 0, position.width, position.height );
        GUI.BeginGroup( position );
    }

    public static void EndGroup() {
        GUI.EndGroup();
        clippingBounds = new Rect( 0, 0, Screen.width, Screen.height );
        clippingEnabled = false;
    }

    public static void DrawLine( Vector2 pointA, Vector2 pointB, Color color ) {
        if ( clippingEnabled ) {
            if ( !segment_rect_intersection( clippingBounds, ref pointA, ref pointB ) ) {
                return;
            }
        }

        if ( !lineMaterial ) {
            /* Credit:  */
            lineMaterial = new Material("Shader \"Lines/Colored Blended\" {" +
           "SubShader { Pass {" +
           "   BindChannels { Bind \"Color\",color }" +
           "   Blend SrcAlpha OneMinusSrcAlpha" +
           "   ZWrite Off Cull Off Fog { Mode Off }" +
           "} } }");
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            lineMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
        }

        lineMaterial.SetPass( 0 );
        GL.Begin( GL.LINES );
        GL.Color( color );
        GL.Vertex3( pointA.x, pointA.y, 0 );
        GL.Vertex3( pointB.x, pointB.y, 0 );
        GL.End();
    }
};