/* The MIT License (MIT)
* 
* Copyright (c) 2015 Marc Clifton
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/

// From: http://stackoverflow.com/questions/34478513/c-sharp-full-duplex-asynchronous-named-pipes-net
// See Eric Frazer's Q and self answer

using System;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Clifton.Core.Pipes
{
    public abstract class BasicPipe
    {
        private const int BUFFER_SIZE = 1024 * 1024;

        public event EventHandler<PipeEventArgs> DataReceived;
        public event EventHandler<EventArgs> PipeClosed;

        protected byte[] pipeBuffer = new byte[BUFFER_SIZE];
        protected PipeStream pipeStream;

        public BasicPipe()
        {
        }

        public void Close()
        {
            pipeStream.WaitForPipeDrain();
            pipeStream.Close();
            pipeStream.Dispose();
            pipeStream = null;
        }

        /// <summary>
        /// Called when Server pipe gets a connection, or when Client pipe is created.
        /// </summary>
        public void StartReadingAsync()
        {
            byte[] buffer = new byte[BUFFER_SIZE];

            pipeStream.ReadAsync(buffer, 0, BUFFER_SIZE).ContinueWith(t =>
            {
                int len = t.Result;

                if (len == 0)
                {
                    PipeClosed?.Invoke(this, EventArgs.Empty);
                    return;
                }

                DataReceived?.Invoke(this, new PipeEventArgs(buffer, len));
                StartReadingAsync();
            });
        }

        public void Flush()
        {
            pipeStream.Flush();
        }

        public Task WriteByteArray(byte[] bytes)
        {
            // this will start writing, but does it copy the memory before returning?
            return pipeStream.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}