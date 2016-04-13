
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Stormancer.Plugins
{
    public class OutputLogStream : Stream
    {
        private Stormancer.EditorPlugin.StormancerClientViewModel clientVM;
        private Stream _s;
        private MemoryStream internalStream;

        public override long Position { get { return _s.Position; } set { _s.Position = value; internalStream.Position = value; } }

        public OutputLogStream(Stream s, EditorPlugin.StormancerClientViewModel cvm)
        {
            _s = s;
            internalStream = new MemoryStream();
            clientVM = cvm;
        }

        public override bool CanRead
        {
            get
            {
                return false;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return _s.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return _s.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                return _s.Length;
            }
        }

        public override void SetLength(long len)
        {
            _s.SetLength(len);
            if (clientVM != null && clientVM.exportLogs == true)
                internalStream.SetLength(len);
        }

        public override void Flush()
        {
            _s.Flush();
            internalStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("This stream is write only");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            internalStream.Seek(offset, origin);
            return _s.Seek(offset, origin);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _s.Write(buffer, offset, count);
            internalStream.Write(buffer, offset, count);
        }

        public void Log(byte scene_handle, ushort route_handle)
        {
                string scene_name = "";
                string route_name = "";

                var sceneVM = clientVM.scenes.Values.FirstOrDefault(s => s.scene.Handle == scene_handle);
                if (sceneVM != null)
                {
                    scene_name = sceneVM.scene.Id;
                    var route = sceneVM.remotes.Values.FirstOrDefault(r => r.Handle == route_handle);
                    if (route != null)
                        route_name = route.Name;
                    EditorPlugin.StormancerEditorDataCollector.Instance.GetDataStatistics(_s, clientVM, route);
            }
                if (scene_name == "")
                    scene_name = "system";
                if (route_name == "")
                    route_name = "system";
                Log(scene_name, route_name);
           
        }

        public void Log(string scene, string route)
        {
            if (clientVM != null && clientVM.exportLogs == true)
            {
                var message = new List<byte>();
                var buffer = new byte[1024];

                int readByte = 0;
                do
                {
                    readByte = internalStream.Read(buffer, 0, 1024);
                    message.AddRange(buffer.Take(readByte));
                }
                while (readByte > 0);
                clientVM.WritePacketLog(false, scene, route, message);
            }
        }
    }
}