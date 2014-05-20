//
// System.Net.WebAsyncResult
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//      Martin Baulig (martin.baulig@xamarin.com)
//
// (C) 2003 Ximian, Inc (http://www.ximian.com)
// Copyright (c) 2014 Xamarin Inc. (http://www.xamarin.com)
//
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.IO;
using System.Threading;

namespace System.Net
{
	delegate void SimpleAsyncCallback (SimpleAsyncResult result);

	class SimpleAsyncResult : IAsyncResult
	{
		ManualResetEvent handle;
		bool synch;
		bool isCompleted;
		SimpleAsyncCallback cb;
		object state;
		bool callbackDone;
		Exception exc;
		object locker = new object ();

		public SimpleAsyncResult (SimpleAsyncCallback cb)
		{
			this.cb = cb;
		}

		public SimpleAsyncResult (AsyncCallback cb, object state)
		{
			this.state = state;
			this.cb = r => cb (this);
		}

		protected void Reset_internal ()
		{
			callbackDone = false;
			exc = null;
			exc = null;
			lock (locker) {
				isCompleted = false;
				if (handle != null)
					handle.Reset ();
			}
		}

		internal void SetCompleted (bool synch, Exception e)
		{
			this.synch = synch;
			exc = e;
			lock (locker) {
				isCompleted = true;
				if (handle != null)
					handle.Set ();
			}
		}

		internal void SetCompleted (bool synch)
		{
			exc = null;
			lock (locker) {
				isCompleted = true;
				if (handle != null)
					handle.Set ();
			}
		}

		internal void DoCallback ()
		{
			if (!callbackDone && cb != null) {
				callbackDone = true;
				if (synch)
					cb (this);
				else
					ThreadPool.QueueUserWorkItem (CB, null);
			}
		}

		void CB (object unused)
		{
			cb (this);
		}

		internal void WaitUntilComplete ()
		{
			if (IsCompleted)
				return;

			AsyncWaitHandle.WaitOne ();
		}

		internal bool WaitUntilComplete (int timeout, bool exitContext)
		{
			if (IsCompleted)
				return true;

			return AsyncWaitHandle.WaitOne (timeout, exitContext);
		}

		public object AsyncState {
			get { return state; }
		}

		public WaitHandle AsyncWaitHandle {
			get {
				lock (locker) {
					if (handle == null)
						handle = new ManualResetEvent (isCompleted);
				}

				return handle;
			}
		}

		public bool CompletedSynchronously {
			get { return synch; }
		}

		public bool IsCompleted {
			get {
				lock (locker) {
					return isCompleted;
				}
			}
		}

		internal bool GotException {
			get { return (exc != null); }
		}

		internal Exception Exception {
			get { return exc; }
		}
	}
}

