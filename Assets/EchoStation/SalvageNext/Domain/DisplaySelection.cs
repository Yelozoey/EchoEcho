using System;
namespace EchoStation.SalvageNext
{
    public enum DisplayWindowMode{Windowed,Borderless,Exclusive}
    public sealed class DisplaySelection:IEquatable<DisplaySelection>
    {
        public RecoveryQuality Quality{get;}
        public int Width{get;}
        public int Height{get;}
        public DisplayWindowMode Mode{get;}
        public DisplaySelection(RecoveryQuality quality,int width,int height,DisplayWindowMode mode)
        {
            if(!Enum.IsDefined(typeof(RecoveryQuality),quality)||!Enum.IsDefined(typeof(DisplayWindowMode),mode)||width<640||width>3840||height<480||height>2160)throw new ArgumentException("Unsupported display selection.");
            Quality=quality;Width=width;Height=height;Mode=mode;
        }
        public bool Equals(DisplaySelection other)=>other!=null&&other.Quality==Quality&&other.Width==Width&&other.Height==Height&&other.Mode==Mode;
        public override bool Equals(object obj)=>Equals(obj as DisplaySelection);
        public override int GetHashCode()=>(((int)Quality*397+Width)*397+Height)*397+(int)Mode;
        public static DisplaySelection Default()=>new DisplaySelection(RecoveryQuality.Medium,1920,1080,DisplayWindowMode.Windowed);
    }
}
