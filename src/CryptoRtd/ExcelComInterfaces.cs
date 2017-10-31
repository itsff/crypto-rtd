using System;
using System.Runtime.InteropServices;

namespace CryptoRtd
{
    //
    // We provide definition of the RTD interfaces used by Excel
    // directly in this C# file. This way out assembly will not
    // have a dependency on Excel's type library.
    //
    
    
    
    [Guid("A43788C1-D91B-11D3-8F39-00C04F3651B8")]
    public interface IRtdUpdateEvent
    {
        void UpdateNotify ();
        int HeartbeatInterval { get; set; }
        void Disconnect ();
    }

    [Guid("EC0E6191-DB51-11D3-8F3E-00C04F3651B8")]
    public interface IRtdServer
    {
        int ServerStart (IRtdUpdateEvent callback);

        object ConnectData (int topicId,
                           [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] ref Array strings,
                           ref bool newValues);

        [return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)]
        Array RefreshData (ref int topicCount);

        void DisconnectData (int topicId);

        int Heartbeat ();

        void ServerTerminate ();
    }
}
