using System;
using EchoStation.Player;
namespace EchoStation.SalvageNext
{
    /// <summary>Temporarily suspends gameplay input without claiming the menus' UiBlocked flag.</summary>
    public sealed class StreamInputLease:IDisposable
    {
        readonly PlayerInputReader input;readonly bool enabled,captured;bool disposed;
        public StreamInputLease(PlayerInputReader reader)
        {RoomScenePump.RequireMainThread();if(!reader)throw new ArgumentException("Live player input required.");input=reader;enabled=input.enabled;captured=input.PointerCaptured;if(enabled)input.enabled=false;}
        public void Dispose()
        {RoomScenePump.RequireMainThread();if(disposed)return;disposed=true;if(!input||!enabled)return;input.enabled=true;if(captured&&!input.UiBlocked&&input.HasApplicationFocus)input.SetPointerCaptured(true);}
    }
}
