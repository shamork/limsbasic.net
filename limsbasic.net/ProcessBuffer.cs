using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LabObjects.LimsBasicNet
{
 
    internal class ProcessBuffer
    {

        #region private fields
        StringBuilder _buffer = new StringBuilder();
        Queue<string> _dataQueue = new Queue<string>();
        #endregion

        #region public properties
        public bool HasUnreadData
        {
            get { return (_dataQueue.Count > 0); } 
        }

        public string Data
        {
            get { return _buffer.ToString(); }
        }
        #endregion

        #region public Methods
        public void Clear()
        {
#if NET_VER_35 && !NET_VER_40
            _buffer.Length = 0;
#else
            _buffer.Clear();
#endif
            _dataQueue.Clear();
        }

        public string Read()
        {
            if (_dataQueue.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                while (_dataQueue.Count > 0)
                    sb.Append(_dataQueue.Dequeue());
                return sb.ToString();
            }
            else
                return string.Empty;            
        }

        public void Append(string s)
        {
            _dataQueue.Enqueue(s);
            _buffer.Append(s);
        }

        public void AppendLine(string s)
        {
            _dataQueue.Enqueue(string.Format("{0}{1}", s, Environment.NewLine));
            _buffer.AppendLine(s);
        }

        #endregion
    }
}
