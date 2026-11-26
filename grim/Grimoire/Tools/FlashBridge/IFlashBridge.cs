using System;
using System.Windows.Forms;

namespace Grimoire.Tools.FlashBridge
{
    public interface IFlashBridge : IDisposable
    {
        string EngineName { get; }
        bool IsReady { get; }

        /// <summary>
        /// Fired when ActionScript calls ExternalInterface.call(name, ...args).
        /// Return a non-null string if the call expects a synchronous return value (e.g. modifyServers).
        /// </summary>
        event Func<string, object[], string> ExternalCallReceived;

        void LoadSwf(string swfPath, Control hostContainer);
        string CallFunctionXml(string xmlRequest);
        string CallFunction(string functionName, params object[] args);
        void CallFunctionFireAndForget(string functionName, params object[] args);
        void SetQuality(string quality);
        void Resize(int width, int height);
    }
}
